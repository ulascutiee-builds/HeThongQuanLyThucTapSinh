using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Every run uses a new disposable LocalDB database, never the application's database.
var tag = Guid.NewGuid().ToString("N");
var databaseName = "CareerPortalPermissionChecks_" + tag;
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("PERMISSION_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
    InitialCatalog = databaseName, IntegratedSecurity = true, TrustServerCertificate = true
}.ConnectionString;
var options = new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options;
await using var db = new CareerDbContext(options);
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var runtime = Path.Combine(root, ".local", "permission-checks-" + tag);
Directory.CreateDirectory(runtime);
using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var address = new Uri($"http://localhost:{port}");
const string password = "TestOnly123!";
var checks = 0;
Process? server = null;
var logs = new StringBuilder();
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
HttpClient Client() => new(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = address };
async Task<JsonNode?> Request(HttpClient client, HttpMethod method, string path, object? body = null, int expected = 200)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    Check((int)response.StatusCode == expected, $"{method} {path}: expected {expected}, got {(int)response.StatusCode}. {content}");
    return string.IsNullOrEmpty(content) ? null : JsonNode.Parse(content);
}
Task<JsonNode?> Get(HttpClient client, string path, int expected = 200) => Request(client, HttpMethod.Get, path, expected: expected);
Task<JsonNode?> Set(HttpClient client, int id, string[] keys, bool useRolePermissions = false, int expected = 200) =>
    Request(client, HttpMethod.Put, $"/api/accounts/{id}/permissions", new { permissions = keys, useRolePermissions }, expected);
Task<JsonNode?> Login(HttpClient client, string email) => Request(client, HttpMethod.Post, "/api/auth/login", new { identity = email, password });
string[] Permissions(JsonNode? node) => node!["permissions"]!.AsArray().Select(x => x!.GetValue<string>()).OrderBy(x => x).ToArray();
AppUser User(string name, string email, string role)
{
    var user = new AppUser { Name = name, Email = email, PasswordHash = "" };
    user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
    user.UserRoles.Add(new() { RoleName = role }); db.AppUsers.Add(user); return user;
}
try
{
    await db.Database.MigrateAsync();
    foreach (var (key, label) in Accounts.Permissions) db.AppPermissions.Add(new() { Key = key, Label = label });
    var roleGrants = new Dictionary<string, string[]> { ["Admin"] = Accounts.Permissions.Keys.ToArray(), ["HR"] = ["profiles.read", "profiles.write", "documents.read"], ["Mentor"] = ["schedule.read"], ["Intern"] = ["schedule.read", "attendance.write"] };
    foreach (var (name, grants) in roleGrants)
        db.AppRoles.Add(new() { Name = name, RolePermissions = grants.Select(key => new RolePermission { RoleName = name, PermissionKey = key }).ToList() });
    var admin = User("Quản trị kiểm thử", "permissions.admin@example.test", "Admin");
    var alice = User("Nhân sự A", "permissions.alice@example.test", "HR");
    var bob = User("Nhân sự B", "permissions.bob@example.test", "HR");
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var apiProject = Path.Combine(root, "backend", "CareerPortal.Api");
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiProject, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var arg in new[] { Path.Combine(apiProject, "bin", "Release", "net10.0", "CareerPortal.Api.dll"), "--urls", address.ToString(), "--Database:SkipInitialization", "true", "--ConnectionStrings:CareerPortal", connection, "--Runtime:DataDirectory", runtime, "--Email:SmtpHost", "", "--Logging:LogLevel:Default", "Warning" }) start.ArgumentList.Add(arg);
    server = new Process { StartInfo = start };
    server.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.Start(); server.BeginOutputReadLine(); server.BeginErrorReadLine();
    using var anon = Client(); using var adminClient = Client(); using var aliceClient = Client(); using var bobClient = Client();
    var ready = false;
    for (var i = 0; i < 60; i++)
    {
        try { using var health = await anon.GetAsync("/api/health/database"); if (health.IsSuccessStatusCode) { ready = true; break; } } catch (HttpRequestException) { }
        if (server.HasExited) break;
        await Task.Delay(500);
    }
    Check(ready, "Test API did not start: " + logs);
    await Login(adminClient, admin.Email); await Login(aliceClient, alice.Email); await Login(bobClient, bob.Email);
    var original = await Get(adminClient, $"/api/accounts/{alice.Id}");
    Check(!original!["hasCustomPermissions"]!.GetValue<bool>(), "Existing accounts must inherit role permissions");
    Check(Permissions(original).SequenceEqual(roleGrants["HR"].OrderBy(x => x)), "Inherited permissions not returned");
    await Set(anon, alice.Id, ["profiles.read"], expected: 401);
    await Set(aliceClient, alice.Id, ["accounts.manage"], expected: 403);
    await Set(adminClient, alice.Id, ["unknown.permission"], expected: 400);
    await Set(adminClient, alice.Id, ["profiles.read", "profiles.read"], expected: 400);
    await Request(adminClient, HttpMethod.Put, $"/api/accounts/{alice.Id}/permissions", new { }, 400);
    await Set(adminClient, int.MaxValue, [], expected: 404);
    var changed = await Set(adminClient, alice.Id, ["profiles.read"]);
    Check(changed!["hasCustomPermissions"]!.GetValue<bool>(), "Custom permission mode was not saved");
    Check(Permissions(changed).SequenceEqual(new[] { "profiles.read" }), "Custom list was combined with the role unexpectedly");
    Check(Permissions(await Get(aliceClient, "/api/auth/me")).SequenceEqual(new[] { "profiles.read" }), "Existing session retained removed grants");
    Check(Permissions(await Get(bobClient, "/api/auth/me")).SequenceEqual(roleGrants["HR"].OrderBy(x => x)), "Editing Alice changed Bob's permissions");
    await Get(aliceClient, "/api/interns");
    await Request(aliceClient, HttpMethod.Post, "/api/hr/profiles", new { }, 403);
    var stored = await db.AppUsers.AsNoTracking().Include(x => x.UserPermissions).SingleAsync(x => x.Id == alice.Id);
    Check(stored.HasCustomPermissions && stored.UserPermissions.Single().PermissionKey == "profiles.read", "Custom permissions not persisted in SQL");
    await Set(adminClient, alice.Id, []);
    Check(Permissions(await Get(aliceClient, "/api/auth/me")).Length == 0, "Empty custom set fell back to role permissions");
    await Get(aliceClient, "/api/interns", 403);
    using var relogin = Client(); Check(Permissions(await Login(relogin, alice.Email)).Length == 0, "Login ignored account permissions");
    await Request(adminClient, HttpMethod.Put, "/api/roles/HR/permissions", new { permissions = new[] { "profiles.read", "programs.manage" } });
    Check(Permissions(await Get(aliceClient, "/api/auth/me")).Length == 0, "Role update overwrote custom permissions");
    Check(Permissions(await Get(bobClient, "/api/auth/me")).SequenceEqual(new[] { "profiles.read", "programs.manage" }), "Inherited account missed role update");
    var reset = await Set(adminClient, alice.Id, [], useRolePermissions: true);
    Check(!reset!["hasCustomPermissions"]!.GetValue<bool>(), "Reset did not restore role inheritance");
    Check(Permissions(await Get(aliceClient, "/api/auth/me")).SequenceEqual(new[] { "profiles.read", "programs.manage" }), "Reset not applied to existing session");
    Check(!await db.UserPermissions.AnyAsync(x => x.UserId == alice.Id), "Reset left stale permission rows");
    await Set(adminClient, alice.Id, ["accounts.manage"]);
    await Get(aliceClient, "/api/accounts");
    await Set(aliceClient, bob.Id, ["accounts.manage", "roles.manage"], expected: 403);
    await Request(aliceClient, HttpMethod.Put, $"/api/accounts/{alice.Id}", new { name = alice.Name, email = alice.Email, roles = new[] { "Admin" }, isActive = true }, 403);
    await Request(aliceClient, HttpMethod.Post, "/api/accounts", new { name = "Unauthorized Admin", email = "unauthorized@example.test", roles = new[] { "Admin" }, isActive = true }, 403);
    await Set(adminClient, admin.Id, [], expected: 400);
    await Set(adminClient, admin.Id, ["accounts.manage", "roles.manage"]);
    await Get(adminClient, "/api/interns", 403);
    await Set(adminClient, admin.Id, [], useRolePermissions: true);
    await Set(adminClient, alice.Id, ["profiles.read"]);
    await Request(adminClient, HttpMethod.Put, $"/api/accounts/{alice.Id}", new { name = alice.Name, email = alice.Email, roles = new[] { "Admin" }, isActive = true });
    var promoted = await Get(adminClient, $"/api/accounts/{alice.Id}");
    Check(Permissions(promoted).Contains("accounts.manage") && Permissions(promoted).Contains("roles.manage"), "Admin promotion lost mandatory management grants");
    await Request(adminClient, HttpMethod.Put, $"/api/accounts/{alice.Id}", new { name = alice.Name, email = alice.Email, roles = new[] { "HR" }, isActive = true });
    await Request(adminClient, HttpMethod.Put, "/api/roles/HR/permissions", new { permissions = roleGrants["HR"] });
    await Set(adminClient, alice.Id, [], useRolePermissions: true);
    Console.WriteLine($"PASS: {checks} account permission API/persistence checks.");
    if (args.Contains("--ui"))
    {
        await UiFixture.Seed(db, password);
        var finish = Path.Combine(runtime, "finish");
        Console.WriteLine($"UI_READY: {address}admin.html");
        Console.WriteLine($"UI account: {admin.Email}; test-only password: {password}");
        Console.WriteLine($"Create this file to finish: {finish}");
        var deadline = DateTime.UtcNow.AddMinutes(45);
        while (!File.Exists(finish) && DateTime.UtcNow < deadline) await Task.Delay(1000);
    }
}
finally
{
    if (server is not null) { if (!server.HasExited) { server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); } server.Dispose(); }
    await File.WriteAllTextAsync(Path.Combine(runtime, "server.log"), logs.ToString());
    SqlConnection.ClearAllPools();
    try { await db.Database.EnsureDeletedAsync(); Console.WriteLine($"Removed isolated test database {databaseName}."); }
    catch (SqlException error) { Console.Error.WriteLine($"Could not check/remove the isolated database {databaseName}: {error.Message}"); }
}

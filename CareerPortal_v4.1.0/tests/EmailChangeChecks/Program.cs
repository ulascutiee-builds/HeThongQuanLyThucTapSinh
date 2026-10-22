using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// A disposable database and loopback HTTP server; never touches the normal app database.
var tag = Guid.NewGuid().ToString("N");
var databaseName = "CareerPortalEmailChecks_" + tag;
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("EMAIL_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
    InitialCatalog = databaseName, IntegratedSecurity = true, TrustServerCertificate = true
}.ConnectionString;
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var apiProject = Path.Combine(root, "backend", "CareerPortal.Api");
var apiDll = Path.Combine(apiProject, "bin", "Release", "net10.0", "CareerPortal.Api.dll");
var runtime = Path.Combine(root, ".local", "email-checks-" + tag);
Directory.CreateDirectory(runtime);
var options = new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options;
await using var db = new CareerDbContext(options);
using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var address = new Uri($"http://localhost:{port}");
const string password = "TestOnly123!";
var checks = 0;
var databaseCreated = false;
Process? server = null;
var logs = new StringBuilder();
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
HttpClient Client() => new(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = address };
async Task<JsonNode?> Request(HttpClient client, HttpMethod method, string path, object? body = null, int expected = 200)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var raw = await response.Content.ReadAsStringAsync();
    Check((int)response.StatusCode == expected, $"{method} {path}: expected {expected}, got {(int)response.StatusCode}: {raw}");
    return string.IsNullOrWhiteSpace(raw) ? null : JsonNode.Parse(raw);
}
Task<JsonNode?> Post(HttpClient c, string path, object body, int expected = 200) => Request(c, HttpMethod.Post, path, body, expected);
Task<JsonNode?> Put(HttpClient c, string path, object body, int expected = 200) => Request(c, HttpMethod.Put, path, body, expected);
Task<JsonNode?> Login(HttpClient c, string email, int expected = 200) => Post(c, "/api/auth/login", new { identity = email, password }, expected);
async Task<string> LatestToken(string recipient)
{
    var body = await db.MailJobs.AsNoTracking().Where(x => x.Recipient == recipient && x.EventKey.StartsWith("email-change:"))
        .OrderByDescending(x => x.Id).Select(x => x.Body).FirstAsync();
    var match = Regex.Match(body, @"token=([0-9A-F]{64})");
    Check(match.Success, "Email change confirmation URL/token missing in queued message");
    return match.Groups[1].Value;
}
object ProfileBody(string email) => new { name = "Thực tập sinh cập nhật", studentId = "SV-EMAIL", email, phone = "0939232002", school = "Đại học kiểm thử", major = "CNTT" };
object AccountBody(string name, string email) => new { name, email, roles = new[] { "HR" }, isActive = true };
try
{
    Check(File.Exists(apiDll), "Build backend/CareerPortal.Api in Release before running email checks.");
    await db.Database.MigrateAsync(); databaseCreated = true;
    foreach (var (key, label) in Accounts.Permissions) db.AppPermissions.Add(new() { Key = key, Label = label });
    db.AppRoles.Add(new() { Name = "Admin", RolePermissions = Accounts.Permissions.Keys.Select(x => new RolePermission { RoleName = "Admin", PermissionKey = x }).ToList() });
    db.AppRoles.Add(new() { Name = "HR" });
    db.AppRoles.Add(new() { Name = "Intern" });
    AppUser User(string name, string email, string role)
    {
        var user = new AppUser { Name = name, Email = email, PasswordHash = "" };
        user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
        user.UserRoles.Add(new() { RoleName = role });
        db.AppUsers.Add(user); return user;
    }
    var admin = User("Admin test", "admin.email@example.test", "Admin");
    var hr = User("HR test", "hr.email@example.test", "HR");
    var profile = new InternProfile { Name = "Thực tập sinh test", StudentId = "SV-EMAIL", Email = "intern.email@example.test", Phone = "0939232002", School = "Đại học kiểm thử", Major = "CNTT", Status = "Chờ hồ sơ", PasswordHash = "", EmailVerified = true };
    profile.PasswordHash = new PasswordHasher<InternProfile>().HashPassword(profile, password);
    var intern = User(profile.Name, profile.Email, "Intern"); intern.Profile = profile;
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();

    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiProject, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    foreach (var arg in new[] { apiDll, "--urls", address.ToString(), "--Database:SkipInitialization", "true", "--ConnectionStrings:CareerPortal", connection, "--Runtime:DataDirectory", runtime, "--PublicUrl", address.ToString().TrimEnd('/'), "--Email:SmtpHost", "", "--Logging:LogLevel:Default", "Warning" }) start.ArgumentList.Add(arg);
    server = new Process { StartInfo = start };
    server.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.Start(); server.BeginOutputReadLine(); server.BeginErrorReadLine();
    using var anonymous = Client(); using var adminClient = Client(); using var internClient = Client();
    var ready = false;
    for (var i = 0; i < 60; i++)
    {
        try { using var health = await anonymous.GetAsync("/api/health/database"); if (health.IsSuccessStatusCode) { ready = true; break; } }
        catch (HttpRequestException) { }
        if (server.HasExited) break;
        await Task.Delay(500);
    }
    Check(ready, "Test API did not start: " + logs);
    await Login(adminClient, admin.Email); await Login(internClient, intern.Email);

    var proposedIntern = "intern.changed@example.test";
    var savedProfile = await Put(internClient, $"/api/interns/{profile.Id}", ProfileBody(proposedIntern));
    Check(savedProfile!["email"]!.GetValue<string>() == intern.Email, "Profile email changed before confirmation");
    Check((await db.PendingEmailChanges.AsNoTracking().SingleAsync(x => x.AccountId == intern.Id)).NewEmail == proposedIntern, "Pending profile email missing");
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == intern.Id && x.Email == intern.Email), "Old account email should remain active");
    using (var oldLogin = Client()) await Login(oldLogin, intern.Email);
    using (var premature = Client()) await Login(premature, proposedIntern, 401);
    var internToken = await LatestToken(proposedIntern);
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = intern.Id, token = "BAD" }, 400);
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = intern.Id, token = internToken });
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = intern.Id, token = internToken }, 400);
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == intern.Id && x.Email == proposedIntern), "Confirmed account email missing");
    Check(await db.InternProfiles.AsNoTracking().AnyAsync(x => x.Id == profile.Id && x.Email == proposedIntern && x.EmailVerified), "Confirmed profile email missing or unverified");
    using (var oldLogin = Client()) await Login(oldLogin, intern.Email, 401);
    using (var newLogin = Client()) await Login(newLogin, proposedIntern);

    var hrEditedProfileEmail = "intern.hr-edited@example.test";
    var hrSavedProfile = await Put(adminClient, $"/api/hr/profiles/{profile.Id}", new
    {
        name = "Thực tập sinh cập nhật", studentId = "SV-EMAIL", email = hrEditedProfileEmail,
        phone = "0939232002", school = "Đại học kiểm thử", major = "CNTT", status = "Chờ hồ sơ"
    });
    Check(hrSavedProfile!["email"]!.GetValue<string>() == proposedIntern, "HR profile edit changed email before confirmation");
    var hrEditedProfileToken = await LatestToken(hrEditedProfileEmail);
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = intern.Id, token = hrEditedProfileToken });
    Check(await db.InternProfiles.AsNoTracking().AnyAsync(x => x.Id == profile.Id && x.Email == hrEditedProfileEmail), "HR profile email edit was not confirmed");
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == intern.Id && x.Email == hrEditedProfileEmail), "HR profile edit did not synchronize account email");

    var proposedHr = "hr.changed@example.test";
    var savedAccount = await Put(adminClient, $"/api/accounts/{hr.Id}", AccountBody("HR updated", proposedHr));
    Check(savedAccount!["email"]!.GetValue<string>() == hr.Email, "Admin account edit changed email before confirmation");
    var hrToken = await LatestToken(proposedHr);
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == hr.Id && x.Email == hr.Email && x.Name == "HR updated"), "Non-email account changes were not saved");
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = hr.Id, token = hrToken });
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == hr.Id && x.Email == proposedHr), "Admin email edit not confirmed");
    using (var newLogin = Client()) await Login(newLogin, proposedHr);

    var expiredEmail = "hr.expired@example.test";
    await Put(adminClient, $"/api/accounts/{hr.Id}", AccountBody("HR updated", expiredEmail));
    var expiredToken = await LatestToken(expiredEmail);
    db.ChangeTracker.Clear();
    var expired = await db.PendingEmailChanges.SingleAsync(x => x.AccountId == hr.Id);
    expired.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    await Post(anonymous, "/api/auth/email-change/confirm", new { accountId = hr.Id, token = expiredToken }, 400);
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == hr.Id && x.Email == proposedHr), "Expired token changed account email");

    Console.WriteLine($"PASS: {checks} verified-email API/persistence checks.");
}
finally
{
    if (server is not null) { if (!server.HasExited) { server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); } server.Dispose(); }
    SqlConnection.ClearAllPools();
    if (databaseCreated)
    {
        try { await db.Database.EnsureDeletedAsync(); Console.WriteLine($"Removed isolated test database {databaseName}."); }
        catch (SqlException error) { Console.Error.WriteLine($"Could not remove isolated database {databaseName}: {error.Message}"); }
    }
}

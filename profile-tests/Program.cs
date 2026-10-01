using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Use a dedicated database containing "Test" in its name, never production data.
string Required(string key) => Environment.GetEnvironmentVariable(key) ?? throw new Exception($"Set {key}");
void Check(bool success, string message) { if (!success) throw new Exception(message); Console.WriteLine("PASS: " + message); }
var connection = Required("STORY_TEST_CONNECTION");
var sql = new SqlConnectionStringBuilder(connection);
if (!sql.InitialCatalog.Contains("Test", StringComparison.OrdinalIgnoreCase) || Required("STORY_TEST_ALLOW_WRITE") != "YES")
    throw new Exception("Use an explicitly authorized test database only.");
var url = new Uri(Required("STORY_TEST_URL"));
if (!url.IsLoopback) throw new Exception("Tests only target a local test API.");
using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
using var client = new HttpClient(handler) { BaseAddress = url };
using var db = new CareerDbContext(new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options);
var suffix = Guid.NewGuid().ToString("N");
var payload = new { name="Profile test",studentId="TEST-"+suffix,email=suffix+"@example.test",school="ICTU",major="CNTT",status="Chờ hồ sơ" };
var before = await db.InternProfiles.CountAsync();
var anonymous = await client.PostAsJsonAsync("/api/hr/profiles", payload);
Check(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Anonymous creation rejected");
var login = await client.PostAsJsonAsync("/api/auth/login", new { identity=Required("STORY_TEST_HR_EMAIL"),password=Required("STORY_TEST_HR_PASSWORD") });
login.EnsureSuccessStatusCode();
var account = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
Check(account.GetProperty("role").GetString() == "HR", "Authenticated account has HR role");
foreach (var invalid in new object[] {
    new {name="",studentId="INVALID-"+suffix,email="wrong-email",school="ICTU",major="CNTT",status="Chờ hồ sơ"},
    new {name="Test",studentId="INVALID-"+suffix,email=suffix+"@example.test",school="ICTU",major="CNTT",status="invalid"},
    new {name="Test",studentId="INVALID-"+suffix,email=suffix+"@example.test",school="ICTU",major="CNTT",status="Chờ hồ sơ",startDate="2026-10-10",endDate="2026-10-01"}
}) {
    var response=await client.PostAsJsonAsync("/api/hr/profiles",invalid);
    Check(response.StatusCode==HttpStatusCode.BadRequest,"Invalid profile returns 400");
    Check(await db.InternProfiles.CountAsync()==before,"Invalid request does not insert database row");
}
var valid = await client.PostAsJsonAsync("/api/hr/profiles", payload);
Check(valid.StatusCode==HttpStatusCode.Created,"Valid profile returns 201");
var saved = await db.InternProfiles.AsNoTracking().SingleAsync(x=>x.StudentId==payload.studentId);
Check(saved.Name==payload.name && saved.Email==payload.email && saved.School==payload.school && saved.Major==payload.major && saved.Status==payload.status,"Stored database fields match request");
Check(!string.IsNullOrWhiteSpace(saved.PasswordHash),"Password hash stored");
var duplicate=await client.PostAsJsonAsync("/api/hr/profiles",payload);
Check(duplicate.StatusCode==HttpStatusCode.Conflict,"Duplicate student ID/email returns 409");
Check(await db.InternProfiles.CountAsync()==before+1,"Only one valid row persisted");
Console.WriteLine("Tests completed. Test profile retained for inspection: "+saved.Id);

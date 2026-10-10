using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

var connection = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION") ?? Environment.GetEnvironmentVariable("ConnectionStrings__CareerPortal")
    ?? throw new InvalidOperationException("Set TEST_DATABASE_CONNECTION to the isolated database used by the test API. No production fallback is used.");
var options = new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options;
await using var db = new CareerDbContext(options);
var tag = Guid.NewGuid().ToString("N");
var baseUrl = Environment.GetEnvironmentVariable("SPRINT1_URL") ?? "http://localhost:5146";
using var http = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(baseUrl) };
var count = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); count++; }
var password = "TestOnly123!";
var phone = $"0{Random.Shared.NextInt64(100_000_000, 1_000_000_000)}";
var dateOfBirth = new DateOnly(2004, 5, 16);
var profileInput = new { name = "Database test", studentId = tag, email = tag + "@example.test", phone, dateOfBirth, school = "DB Test", major = "DB Test", password };
var response = await http.PostAsJsonAsync("/api/interns/register", profileInput); response.EnsureSuccessStatusCode();
var profile = await db.InternProfiles.SingleAsync(x => x.Email == profileInput.email);
var account = await db.AppUsers.Include(x => x.UserRoles).SingleAsync(x => x.ProfileId == profile.Id);
Check(profile.DateOfBirth == dateOfBirth, "Date of birth was not persisted");
Check(profile.PasswordHash != password && profile.PasswordHash.Length > 40, "Profile password was not hashed");
Check(account.PasswordHash != password && new PasswordHasher<AppUser>().VerifyHashedPassword(account, account.PasswordHash, password) != PasswordVerificationResult.Failed, "Persistent account hash does not authenticate");
Check(account.UserRoles.Select(x => x.RoleName).SequenceEqual(new[] { "Intern" }), "Intern role linkage missing");

var duplicateEmail = await http.PostAsJsonAsync("/api/interns/register", new { name = "Duplicate email", studentId = tag + "-email", email = profileInput.email.ToUpperInvariant(), phone = $"0{Random.Shared.NextInt64(100_000_000, 1_000_000_000)}", school = "DB Test", major = "DB Test", password });
Check(duplicateEmail.StatusCode == HttpStatusCode.Conflict, "Duplicate email was accepted");
using (var json = JsonDocument.Parse(await duplicateEmail.Content.ReadAsStringAsync())) Check(json.RootElement.GetProperty("errors").TryGetProperty("Email", out _), "Email field error missing");
var duplicatePhone = await http.PostAsJsonAsync("/api/interns/register", new { name = "Duplicate phone", studentId = tag + "-phone", email = tag + "-phone@example.test", phone = "+84" + phone[1..], school = "DB Test", major = "DB Test", password });
Check(duplicatePhone.StatusCode == HttpStatusCode.Conflict, "Equivalent Vietnamese phone was accepted");
using (var json = JsonDocument.Parse(await duplicatePhone.Content.ReadAsStringAsync())) Check(json.RootElement.GetProperty("errors").TryGetProperty("Phone", out _), "Phone field error missing");
var futureDate = InternshipStatusSync.Today.AddDays(1);
var futureBirth = await http.PostAsJsonAsync("/api/interns/register", new { name = "Future birth", studentId = tag + "-future", email = tag + "-future@example.test", phone = $"0{Random.Shared.NextInt64(100_000_000, 1_000_000_000)}", dateOfBirth = futureDate, school = "DB Test", major = "DB Test", password });
Check(futureBirth.StatusCode == HttpStatusCode.BadRequest, "Future birth date was accepted");
Check(!await db.InternProfiles.AnyAsync(x => x.Email == tag + "-future@example.test"), "Invalid registration persisted");

response = await http.PostAsJsonAsync("/api/interns/login", new { identity = profile.Email, password }); response.EnsureSuccessStatusCode();
var futureUpdate = await http.PutAsJsonAsync($"/api/interns/{profile.Id}", new { name = profile.Name, studentId = profile.StudentId, email = profile.Email, phone = profile.Phone, dateOfBirth = futureDate, school = profile.School, major = profile.Major });
Check(futureUpdate.StatusCode == HttpStatusCode.BadRequest, "Future birth update was accepted");
await db.Entry(profile).ReloadAsync(); Check(profile.DateOfBirth == dateOfBirth, "Rejected birth date changed the profile");

var contract = new InternDocument { ProfileId = profile.Id, Type = "Hợp đồng thực tập", FileName = "expired.pdf", ContentType = "application/pdf", Content = "%PDF-1.4\n%%EOF"u8.ToArray(), Status = "Chờ xác nhận", ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), UploadedBy = "TEST ONLY" };
db.InternDocuments.Add(contract); await db.SaveChangesAsync();
response = await http.PostAsync($"/api/contracts/{contract.Id}/confirm", null);
Check(response.StatusCode == HttpStatusCode.Conflict, "Expired contract was accepted");
await db.Entry(contract).ReloadAsync(); Check(contract.ConfirmedAt is null && contract.Status == "Chờ xác nhận", "Rejected confirmation mutated the contract");

var mail = new MailJob { EventKey = "retry-test:" + tag, Recipient = "INVALID EMAIL", Subject = "Local retry test", Body = "Test only" };
db.MailJobs.Add(mail); await db.SaveChangesAsync();
for (var i = 0; i < 15; i++) { await Task.Delay(1000); await db.Entry(mail).ReloadAsync(); if (mail.Attempts > 0 && mail.Status != "Processing") break; }
Check(mail.Attempts == 1 && mail.Status == "Queued" && !string.IsNullOrEmpty(mail.LastError), "Failed email was not queued and logged");
mail.Recipient = profile.Email; mail.DueAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync();
for (var i = 0; i < 15; i++) { await Task.Delay(1000); await db.Entry(mail).ReloadAsync(); if (mail.Status == "Sent") break; }
Check(mail.Status == "Sent" && mail.Attempts == 2, "Email retry did not reach Sent");
var model = db.Model.FindEntityType(typeof(InternProfile))!;
foreach (var field in new[] { "School", "Major", "Status" }) Check(model.GetIndexes().Any(x => x.Properties.Any(p => p.Name == field)), "Missing index " + field);
Check(db.Model.FindEntityType(typeof(AppUser))!.GetIndexes().Any(x => x.IsUnique && x.Properties.SingleOrDefault()?.Name == "Email"), "Account email unique index missing");
Check(await db.UserRoles.AnyAsync(x => x.UserId == account.Id && x.RoleName == "Intern"), "User-role database row missing");
Console.WriteLine($"PASS: {count} database/account/mail persistence checks. Test tag {tag}.");

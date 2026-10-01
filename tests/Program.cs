using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var result = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../final-result.json")));
var database = result.RootElement.GetProperty("database").GetString()!;
if (!database.StartsWith("CareerPortalRestore_")) throw new Exception("Expected isolated restore database");
await using var db = new CareerDbContext(new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString).Options);
if (await db.InternProfiles.CountAsync() != 3) throw new Exception("Profile restore mismatch");
if (!await db.PortalAccounts.AnyAsync(x => x.Role == "Admin")) throw new Exception("Missing admin");
if (!await db.WorkItems.AnyAsync(x => x.Kind == "tasks" && x.Progress == 100)) throw new Exception("Missing task progress");
var report = await db.WorkItems.SingleAsync(x => x.Kind == "reports");
if (System.Text.Encoding.UTF8.GetString(report.Attachment ?? []) != "%PDF-1.4 proof") throw new Exception("Attachment mismatch");
if (await db.InternDocuments.CountAsync() != 3) throw new Exception("Legacy document mismatch");
if (!await db.InternReviewHistories.AnyAsync()) throw new Exception("Missing review history");
if (await db.MailJobs.AnyAsync(x => x.Status == "Queued")) throw new Exception("Restore must pause outgoing mail");
Console.WriteLine("PASS: restored profiles, accounts, task progress, report bytes, legacy documents, review history, paused mail.");

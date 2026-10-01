using System.Net;
using System.Net.Mail;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

public sealed class PortalJobs(IServiceScopeFactory scopes, ILogger<PortalJobs> logger, IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<CareerDbContext>();
                var host = config["Smtp:Host"];
                var pickup = config["Smtp:DeliveryMode"] == "PickupDirectory";
                if (pickup || !string.IsNullOrWhiteSpace(host))
                {
                    var jobs = await db.MailJobs.Where(x => x.Status == "Queued" && x.DueAt <= DateTimeOffset.UtcNow && x.Attempts < 5).Take(20).ToListAsync(stoppingToken);
                    foreach (var job in jobs)
                    {
                        try
                        {
                            using var smtp = new SmtpClient(string.IsNullOrWhiteSpace(host) ? "localhost" : host, config.GetValue("Smtp:Port", 587)) { EnableSsl = !pickup && config.GetValue("Smtp:EnableSsl", true) };
                            if (pickup)
                            {
                                var directory = Path.Combine(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "App_Data", "mail");
                                Directory.CreateDirectory(directory);
                                smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                                smtp.PickupDirectoryLocation = directory;
                            }
                            if (!string.IsNullOrEmpty(config["Smtp:Username"])) smtp.Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"]);
                            using var message = new MailMessage(config["Smtp:From"] ?? "", job.Recipient, job.Subject, job.Body);
                            await smtp.SendMailAsync(message, stoppingToken); job.Status = pickup ? "SavedToPickup" : "Sent"; job.Error = "";
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException) { job.Attempts++; job.Error = ex.Message; job.DueAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, job.Attempts)); if (job.Attempts >= 5) job.Status = "Failed"; }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                if (config.GetValue("Backup:Enabled", false))
                {
                    var folder = BackupFolder(scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>());
                    Directory.CreateDirectory(folder);
                    var latest = Directory.GetFiles(folder, "*.backup").Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
                    if ((DateTime.UtcNow - latest).TotalHours >= config.GetValue("Backup:IntervalHours", 24))
                        await Backup(db, scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), folder);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Portal background job failed"); }
        }
    }
    public static string BackupFolder(IWebHostEnvironment env) => Path.Combine(env.ContentRootPath, "App_Data", "backups");
    public static async Task<string> Backup(CareerDbContext db, IDataProtectionProvider protection, string folder)
    {
        Directory.CreateDirectory(folder);
        await using var snapshotTransaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        // Explicit document byte arrays are retained. JSON cycles are avoided by projections.
        var snapshot = new {
            schema = 1, createdAt = DateTimeOffset.UtcNow,
            profiles = await db.InternProfiles.AsNoTracking().Select(x => new { x.Id, x.Name, x.StudentId, x.Email, x.School, x.Major, x.PasswordHash, x.Status, x.StartDate, x.EndDate, x.CreatedAt }).ToListAsync(),
            documents = await db.InternDocuments.AsNoTracking().Select(x => new { x.Id, x.ProfileId, x.Type, x.FileName, x.ContentType, x.Content, x.Status, x.Note, x.UploadedAt, x.ConfirmedAt }).ToListAsync(),
            applications = await db.InternApplications.AsNoTracking().Select(x => new { x.Id, x.ProfileId, x.Status, x.AppliedAt }).ToListAsync(),
            reviews = await db.InternReviewHistories.AsNoTracking().Select(x => new { x.Id, x.ProfileId, x.TargetType, x.TargetId, x.TargetLabel, x.Status, x.Note, x.ReviewedAt }).ToListAsync(),
            accounts = await db.PortalAccounts.AsNoTracking().ToListAsync(), work = await db.WorkItems.AsNoTracking().ToListAsync(),
            attachments = await db.WorkItems.Where(x => x.Attachment != null).Select(x => new { x.Id, x.Attachment }).ToListAsync(),
            mail = await db.MailJobs.AsNoTracking().ToListAsync(), audit = await db.PortalAudits.AsNoTracking().ToListAsync()
        };
        var protector = protection.CreateProtector("CareerPortal.Backup.v1");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot);
        await snapshotTransaction.CommitAsync();
        var encrypted = protector.Protect(bytes);
        if (!protector.Unprotect(encrypted).SequenceEqual(bytes)) throw new InvalidOperationException("Backup verification failed");
        var name = $"portal-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.backup";
        await File.WriteAllBytesAsync(Path.Combine(folder, name), encrypted); return name;
    }
}

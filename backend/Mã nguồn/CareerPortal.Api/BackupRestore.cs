using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

public static class BackupRestore
{
    // Restores to a NEW database. Never deletes or overwrites the active database.
    public static async Task<string> Restore(string name, CareerDbContext source, IDataProtectionProvider protection, IWebHostEnvironment env)
    {
        if (Path.GetFileName(name) != name || !name.EndsWith(".backup")) throw new ArgumentException("Tên bản sao lưu không hợp lệ.");
        var encrypted = await File.ReadAllBytesAsync(Path.Combine(PortalJobs.BackupFolder(env), name));
        var bytes = protection.CreateProtector("CareerPortal.Backup.v1").Unprotect(encrypted);
        using var data = JsonDocument.Parse(bytes); var root = data.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1) throw new ArgumentException("Phiên bản bản sao lưu không hỗ trợ.");
        var connection = new SqlConnectionStringBuilder(source.Database.GetConnectionString()) { InitialCatalog = $"CareerPortalRestore_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}" };
        await using var db = new CareerDbContext(new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection.ConnectionString).Options);
        await db.Database.MigrateAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        async Task Insert<T>(string key, string table) where T : class
        {
            var items = root.GetProperty(key).Deserialize<List<T>>(options) ?? [];
            var commands = table switch {
                "InternProfiles" => ("SET IDENTITY_INSERT [InternProfiles] ON", "SET IDENTITY_INSERT [InternProfiles] OFF"),
                "InternDocuments" => ("SET IDENTITY_INSERT [InternDocuments] ON", "SET IDENTITY_INSERT [InternDocuments] OFF"),
                "InternApplications" => ("SET IDENTITY_INSERT [InternApplications] ON", "SET IDENTITY_INSERT [InternApplications] OFF"),
                "InternReviewHistories" => ("SET IDENTITY_INSERT [InternReviewHistories] ON", "SET IDENTITY_INSERT [InternReviewHistories] OFF"),
                "PortalAccounts" => ("SET IDENTITY_INSERT [PortalAccounts] ON", "SET IDENTITY_INSERT [PortalAccounts] OFF"),
                "WorkItems" => ("SET IDENTITY_INSERT [WorkItems] ON", "SET IDENTITY_INSERT [WorkItems] OFF"),
                "PortalAudits" => ("SET IDENTITY_INSERT [PortalAudits] ON", "SET IDENTITY_INSERT [PortalAudits] OFF"),
                _ => throw new InvalidOperationException("Unknown restore table")
            };
            await db.Database.ExecuteSqlRawAsync(commands.Item1);
            db.Set<T>().AddRange(items); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            await db.Database.ExecuteSqlRawAsync(commands.Item2);
        }
        await Insert<InternProfile>("profiles", "InternProfiles");
        await Insert<InternDocument>("documents", "InternDocuments");
        await Insert<InternApplication>("applications", "InternApplications");
        await Insert<InternReviewHistory>("reviews", "InternReviewHistories");
        await Insert<PortalAccount>("accounts", "PortalAccounts");
        await Insert<WorkItem>("work", "WorkItems");
        foreach (var attachment in root.GetProperty("attachments").EnumerateArray())
        {
            var row = await db.WorkItems.FindAsync(attachment.GetProperty("Id").GetInt32());
            if (row is not null) row.Attachment = attachment.GetProperty("Attachment").GetBytesFromBase64();
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        // Do not automatically resend queued messages from a restored database.
        var jobs = root.GetProperty("mail").Deserialize<List<MailJob>>(options) ?? [];
        foreach (var job in jobs) if (job.Status == "Queued") { job.Status = "Paused"; job.Error = "Phục hồi: cần Admin xác nhận thử lại để gửi."; }
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [MailJobs] ON"); db.MailJobs.AddRange(jobs); await db.SaveChangesAsync(); await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [MailJobs] OFF"); db.ChangeTracker.Clear();
        await Insert<PortalAudit>("audit", "PortalAudits");
        await tx.CommitAsync(); return connection.InitialCatalog;
    }
}

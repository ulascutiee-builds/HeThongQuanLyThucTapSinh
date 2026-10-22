using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

public static class PortalMailWorker
{
    static int started;

    public static void Start(WebApplication app)
    {
        if (Interlocked.Exchange(ref started, 1) != 0) return;
        app.Lifetime.ApplicationStarted.Register(() => _ = RunAsync(app.Services, app.Lifetime.ApplicationStopping));
    }

    static async Task RunAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(services, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { Console.Error.WriteLine($"Portal mail worker failed: {error.Message}"); }

            try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    static async Task ProcessBatchAsync(IServiceProvider services, CancellationToken stoppingToken)
    {
        for (var processed = 0; processed < 10 && !stoppingToken.IsCancellationRequested; processed++)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CareerDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var now = DateTimeOffset.UtcNow;
            await db.MailJobs.Where(x => x.Status == "Processing" && x.DueAt <= now && x.Attempts >= 5)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "Failed").SetProperty(x => x.Error, "Worker stopped while processing the email."), stoppingToken);
            var id = await db.MailJobs.AsNoTracking()
                .Where(x => (x.Status == "Queued" || x.Status == "Processing") && x.DueAt <= now && x.Attempts < 5)
                .OrderBy(x => x.DueAt).ThenBy(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync(stoppingToken);
            if (id == 0) return;

            var leaseUntil = now.AddMinutes(5);
            var claimed = await db.MailJobs.Where(x => x.Id == id && (x.Status == "Queued" || x.Status == "Processing") && x.DueAt <= now && x.Attempts < 5)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Status, "Processing")
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.DueAt, leaseUntil), stoppingToken);
            if (claimed == 0) { processed--; continue; }

            var job = await db.MailJobs.SingleOrDefaultAsync(x => x.Id == id, stoppingToken);
            if (job is null) continue;
            try
            {
                var host = config["Smtp:Host"];
                var from = config["Smtp:From"];
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                    throw new InvalidOperationException("Chưa cấu hình Smtp:Host và Smtp:From.");
                if (!MailAddress.TryCreate(from, out _) || !MailAddress.TryCreate(job.Recipient, out _))
                    throw new InvalidOperationException("Địa chỉ người gửi hoặc người nhận không hợp lệ.");

                var port = int.TryParse(config["Smtp:Port"], out var configuredPort) ? configuredPort : 587;
                var enableSsl = bool.TryParse(config["Smtp:EnableSsl"], out var configuredSsl) ? configuredSsl : true;
                using var message = new MailMessage(from, job.Recipient, job.Subject, job.Body);
                using var client = new SmtpClient(host, port) { EnableSsl = enableSsl, UseDefaultCredentials = false };
                var user = config["Smtp:User"];
                var password = config["Smtp:Password"];
                if (!string.IsNullOrWhiteSpace(user)) client.Credentials = new NetworkCredential(user, password);
                await client.SendMailAsync(message);
                job.Status = "Sent";
                job.Error = "";
                job.Body = "";
            }
            catch (Exception error)
            {
                var retrySeconds = Math.Min(3600, 30 * Math.Pow(2, Math.Min(job.Attempts - 1, 7)));
                job.Status = job.Attempts >= 5 ? "Failed" : "Queued";
                job.DueAt = DateTimeOffset.UtcNow.AddSeconds(retrySeconds);
                job.Error = error.Message.Length > 4000 ? error.Message[..4000] : error.Message;
            }
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}

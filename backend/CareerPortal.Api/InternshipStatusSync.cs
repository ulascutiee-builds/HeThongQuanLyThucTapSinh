using Microsoft.EntityFrameworkCore;

public static class InternshipStatusSync
{
    public static async Task<bool> UpdateAsync(CareerDbContext db, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var contracts = await db.InternDocuments
            .Where(x => x.Type == "Hợp đồng thực tập" && x.IsCurrent && x.StartsAt != null && x.ExpiresAt != null)
            .ToListAsync(cancellationToken);
        var profiles = await db.InternProfiles
            .Where(x => x.Status != "Từ chối")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var changed = false;

        foreach (var contract in contracts)
        {
            if (!profiles.TryGetValue(contract.ProfileId, out var profile))
                continue;

            var start = DateOnly.FromDateTime(contract.StartsAt!.Value.Date);
            var end = DateOnly.FromDateTime(contract.ExpiresAt!.Value.Date);
            var status = end <= today
                ? "Đã hoàn thành"
                : start > today
                    ? "Chờ bắt đầu"
                    : "Đang thực tập";

            if (profile.StartDate != start)
            {
                profile.StartDate = start;
                changed = true;
            }

            if (profile.EndDate != end)
            {
                profile.EndDate = end;
                changed = true;
            }

            if (profile.Status != status)
            {
                profile.Status = status;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);
        return changed;
    }
}

public sealed class InternshipStatusWorker(IServiceScopeFactory scopes, ILogger<InternshipStatusWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CareerDbContext>();
                await InternshipStatusSync.UpdateAsync(db, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Không thể tự động cập nhật trạng thái thực tập.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;

public static class InternshipStatusSync
{
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(7);
    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(LocalOffset).Date);
    public static DateTimeOffset StartOfDay(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), LocalOffset);
    public static DateTimeOffset EndOfDay(DateOnly date) => new(date.ToDateTime(TimeOnly.MaxValue), LocalOffset);
    public static bool TryDate(string value, out DateOnly date)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date)) return true;
        if (DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var timestamp))
        { date = DateOnly.FromDateTime(timestamp.ToOffset(LocalOffset).Date); return true; }
        return false;
    }
    public static async Task<bool> UpdateAsync(CareerDbContext db, CancellationToken cancellationToken = default)
    {
        var today = Today;
        var contracts = await db.InternDocuments
            .Where(x => x.Type == "Hợp đồng thực tập" && x.IsCurrent && x.StartsAt != null && x.ExpiresAt != null)
            .ToListAsync(cancellationToken);
        var profiles = await db.InternProfiles
            .Where(x => x.Status != "Từ chối")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var changed = false;
        var assignments = await db.InternAssignments.Include(x => x.Program).ToDictionaryAsync(x => x.ProfileId, cancellationToken);

        foreach (var contract in contracts)
        {
            if (!profiles.TryGetValue(contract.ProfileId, out var profile))
                continue;

            if (assignments.ContainsKey(contract.ProfileId)) continue;
            var start = DateOnly.FromDateTime(contract.StartsAt!.Value.ToOffset(LocalOffset).Date);
            var end = DateOnly.FromDateTime(contract.ExpiresAt!.Value.ToOffset(LocalOffset).Date);
            var status = end < today
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

            if (contract.Status == "Đã xác nhận" && profile.Status != status)
            {
                profile.Status = status;
                changed = true;
            }
        }

        foreach (var assignment in assignments.Values)
        {
            if (!profiles.TryGetValue(assignment.ProfileId, out var profile) || assignment.Program is null) continue;
            var start = assignment.Program.StartDate;
            var end = assignment.Program.EndDate;
            if (profile.StartDate != start || profile.EndDate != end) { profile.StartDate = start; profile.EndDate = end; changed = true; }
            if (profile.Status is "Chờ hồ sơ" or "Chờ duyệt") continue;
            var status = end < today ? "Đã hoàn thành" : start > today ? "Chờ bắt đầu" : "Đang thực tập";
            if (profile.Status != status) { profile.Status = status; changed = true; }
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

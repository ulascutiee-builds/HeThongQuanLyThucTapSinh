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
        var confirmedContractProfiles = contracts
            .Where(x => x.Status == "Đã xác nhận")
            .Select(x => x.ProfileId)
            .ToHashSet();
        var expiredConfirmedContractProfiles = contracts
            .Where(x => x.Status == "Đã xác nhận" &&
                DateOnly.FromDateTime(x.ExpiresAt!.Value.ToOffset(LocalOffset).Date) < today)
            .Select(x => x.ProfileId)
            .ToHashSet();
        var profiles = await db.InternProfiles
            .Where(x => x.Status != "Từ chối")
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var changed = false;
        var assignments = await db.InternAssignments.Include(x => x.Program).ToDictionaryAsync(x => x.ProfileId, cancellationToken);
        var submittedProfiles = await db.InternApplications
            .Where(x => x.Status == "Chờ duyệt" || x.Status == "Đã duyệt")
            .Select(x => new { x.ProfileId, x.Status })
            .ToListAsync(cancellationToken);
        var pendingApplications = submittedProfiles
            .Where(x => x.Status == "Chờ duyệt")
            .Select(x => x.ProfileId)
            .ToHashSet();
        var submittedProfileIds = submittedProfiles
            .Select(x => x.ProfileId)
            .ToHashSet();
        var submittedDocumentTypes = await db.InternDocuments
            .Where(x => x.IsCurrent && (x.Type == "CV" || x.Type == "Đơn xin thực tập") &&
                (x.Status == "Chờ duyệt" || x.Status == "Đã duyệt"))
            .Select(x => new { x.ProfileId, x.Type })
            .ToListAsync(cancellationToken);
        var submittedDocuments = submittedDocumentTypes
            .Select(x => (x.ProfileId, x.Type))
            .ToHashSet();
        var completeApplications = submittedProfileIds
            .Where(profileId => submittedDocuments.Contains((profileId, "CV")) &&
                submittedDocuments.Contains((profileId, "Đơn xin thực tập")))
            .ToHashSet();
        foreach (var profileId in pendingApplications)
        {
            if (completeApplications.Contains(profileId) && profiles.TryGetValue(profileId, out var profile) &&
                profile.Status is not ("Chờ duyệt" or "Đã hoàn thành"))
            {
                profile.Status = "Chờ duyệt";
                changed = true;
            }
        }

        foreach (var profile in profiles.Values)
        {
            if (confirmedContractProfiles.Contains(profile.Id) || profile.Status == "Đã hoàn thành" ||
                completeApplications.Contains(profile.Id))
                continue;

            if (profile.Status != "Chờ hồ sơ")
            {
                profile.Status = "Chờ hồ sơ";
                changed = true;
            }
        }

        foreach (var contract in contracts)
        {
            if (!profiles.TryGetValue(contract.ProfileId, out var profile))
                continue;

            if (assignments.ContainsKey(contract.ProfileId)) continue;
            var start = DateOnly.FromDateTime(contract.StartsAt!.Value.ToOffset(LocalOffset).Date);
            var end = DateOnly.FromDateTime(contract.ExpiresAt!.Value.ToOffset(LocalOffset).Date);
            var status = end < today ? "Đã hoàn thành" : "Đang thực tập";

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
            else if (contract.Status != "Đã xác nhận" && completeApplications.Contains(profile.Id) &&
                !pendingApplications.Contains(profile.Id) &&
                profile.Status is not ("Đã hoàn thành" or "Từ chối"))
            {
                if (profile.Status != "Chờ bắt đầu")
                {
                    profile.Status = "Chờ bắt đầu";
                    changed = true;
                }
            }
        }

        foreach (var assignment in assignments.Values)
        {
            if (!profiles.TryGetValue(assignment.ProfileId, out var profile) || assignment.Program is null) continue;
            var start = assignment.Program.StartDate;
            var end = assignment.Program.EndDate;
            if (profile.StartDate != start || profile.EndDate != end) { profile.StartDate = start; profile.EndDate = end; changed = true; }
            if (profile.Status is "Chờ hồ sơ" or "Chờ duyệt") continue;
            if (!confirmedContractProfiles.Contains(profile.Id))
            {
                if (profile.Status != "Chờ bắt đầu") { profile.Status = "Chờ bắt đầu"; changed = true; }
                continue;
            }
            var status = end < today ? "Đã hoàn thành" : "Đang thực tập";
            if (profile.Status != status) { profile.Status = status; changed = true; }
        }

        foreach (var profileId in expiredConfirmedContractProfiles)
        {
            if (profiles.TryGetValue(profileId, out var profile) && profile.Status != "Đã hoàn thành")
            {
                profile.Status = "Đã hoàn thành";
                changed = true;
            }
        }

        foreach (var profile in profiles.Values)
        {
            if (profile.Status == "Đang thực tập" && !confirmedContractProfiles.Contains(profile.Id))
            {
                profile.Status = "Chờ bắt đầu";
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

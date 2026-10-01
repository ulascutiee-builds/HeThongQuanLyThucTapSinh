using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapWorkspace(RouteGroupBuilder group)
    {
        group.MapGet("/{profileId:int}/workspace", async (int profileId, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles
                .Include(item => item.Documents)
                .Include(item => item.Application)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == profileId);
            if (profile is null) return Results.NotFound();

            var reviewHistory = await db.InternReviewHistories.AsNoTracking()
                .Where(history => history.ProfileId == profileId)
                .OrderByDescending(history => history.ReviewedAt)
                .ToListAsync();

            return Results.Ok(new InternWorkspaceResponse(
                profile.ToResponse(),
                profile.Application?.ToResponse(),
                profile.Documents.OrderByDescending(document => document.UploadedAt)
                    .Select(document => document.ToResponse()).ToArray(),
                reviewHistory.Select(history => history.ToResponse()).ToArray()));
        })
        .WithName("GetInternWorkspace");
    }
}

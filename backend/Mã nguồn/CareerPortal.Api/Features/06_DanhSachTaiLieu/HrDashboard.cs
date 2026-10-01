using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapHrDashboard(RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", async (CareerDbContext db) =>
        {
            var profiles = await db.InternProfiles.AsNoTracking()
                .OrderByDescending(profile => profile.CreatedAt).ToListAsync();
            var applications = await db.InternApplications.AsNoTracking()
                .OrderByDescending(application => application.AppliedAt).ToListAsync();
            var documents = await db.InternDocuments.AsNoTracking()
                .OrderByDescending(document => document.UploadedAt).ToListAsync();
            var reviewHistory = await db.InternReviewHistories.AsNoTracking()
                .OrderByDescending(history => history.ReviewedAt).ToListAsync();
            return Results.Ok(new HrDashboardResponse(
                profiles.Select(profile => profile.ToResponse()).ToArray(),
                applications.Select(application => application.ToResponse()).ToArray(),
                documents.Select(document => document.ToResponse()).ToArray(),
                reviewHistory.Select(history => history.ToResponse()).ToArray()));
        })
        .WithName("GetHrDashboard");
    }
}

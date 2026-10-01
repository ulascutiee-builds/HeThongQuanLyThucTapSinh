using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapSubmitApplication(RouteGroupBuilder group)
    {
        group.MapPost("/{profileId:int}/submit", async (int profileId, CareerDbContext db) =>
        {
            if (await db.WorkItems.AnyAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId && x.Status == "Unverified"))
                return Results.BadRequest(new { message = "Cần xác thực email qua liên kết đã gửi trước khi nộp hồ sơ." });
            var profile = await db.InternProfiles
                .Include(item => item.Documents)
                .Include(item => item.Application)
                .FirstOrDefaultAsync(item => item.Id == profileId);
            if (profile is null) return Results.NotFound();

            var hasCv = profile.Documents.Any(document => document.Type == "CV" && document.Status != "Từ chối");
            var hasLetter = profile.Documents.Any(document => document.Type == "Đơn xin thực tập" && document.Status != "Từ chối");
            if (!hasCv || !hasLetter)
                return Results.BadRequest(new { message = "Cần tải lên CV và đơn xin thực tập trước khi nộp." });

            if (profile.Application is null)
            {
                profile.Application = new InternApplication { ProfileId = profile.Id, Status = "Chờ duyệt" };
            }
            else
            {
                profile.Application.Status = "Chờ duyệt";
                profile.Application.AppliedAt = DateTimeOffset.UtcNow;
            }
            profile.Status = "Chờ duyệt";
            await db.SaveChangesAsync();
            return Results.Ok(profile.Application.ToResponse());
        })
        .WithName("SubmitInternApplication");
    }
}

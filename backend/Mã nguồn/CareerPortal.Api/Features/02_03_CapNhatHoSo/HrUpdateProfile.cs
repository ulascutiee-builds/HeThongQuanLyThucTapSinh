using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapHrUpdateProfile(RouteGroupBuilder group)
    {
        group.MapPut("/profiles/{profileId:int}", async (
            int profileId, UpdateHrProfileRequest request, CareerDbContext db) =>
        {
            if (request.EndDate < request.StartDate)
                return Results.BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });
            if (!ProfileStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái hồ sơ không hợp lệ." });
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();

            profile.Name = request.Name.Trim();
            profile.Email = request.Email.Trim();
            profile.School = request.School.Trim();
            profile.Major = request.Major.Trim();
            profile.StartDate = request.StartDate;
            profile.EndDate = request.EndDate;
            profile.Status = request.Status;
            await db.SaveChangesAsync();
            return Results.Ok(profile.ToResponse());
        })
        .WithName("UpdateHrProfile");
    }
}

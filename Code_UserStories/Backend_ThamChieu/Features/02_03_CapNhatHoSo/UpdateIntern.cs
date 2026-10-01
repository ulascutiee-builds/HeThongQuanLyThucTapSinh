using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapUpdateIntern(RouteGroupBuilder group)
    {
        group.MapPut("/{profileId:int}", async (
            int profileId, UpdateInternProfileRequest request, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();
            var email = request.Email.Trim();
            if (await db.InternProfiles.AnyAsync(item => item.Id != profileId && item.Email == email))
                return Results.Conflict(new { message = "Email đã được tài khoản khác sử dụng." });

            profile.Name = request.Name.Trim();
            profile.Email = email;
            profile.School = request.School.Trim();
            profile.Major = request.Major.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(profile.ToResponse());
        })
        .WithName("UpdateInternProfile");
    }
}

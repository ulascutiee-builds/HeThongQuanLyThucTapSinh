using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapCreateProfile(RouteGroupBuilder group)
    {
        group.MapPost("/profiles", async (
            CreateHrProfileRequest request,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            if (request.EndDate < request.StartDate)
                return Results.BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });
            if (!ProfileStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái hồ sơ không hợp lệ." });
            if (await db.InternProfiles.AnyAsync(profile =>
                    profile.StudentId == request.StudentId.Trim() || profile.Email == request.Email.Trim()))
                return Results.Conflict(new { message = "Mã sinh viên hoặc email đã tồn tại." });

            var profile = new InternProfile
            {
                Name = request.Name.Trim(),
                StudentId = request.StudentId.Trim(),
                Email = request.Email.Trim(),
                School = request.School.Trim(),
                Major = request.Major.Trim(),
                PasswordHash = string.Empty,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status
            };
            profile.PasswordHash = passwordHasher.HashPassword(profile, Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            db.InternProfiles.Add(profile);
            await db.SaveChangesAsync();
            return Results.Created($"/api/hr/profiles/{profile.Id}", profile.ToResponse());
        })
        .WithName("CreateHrProfile");
    }
}

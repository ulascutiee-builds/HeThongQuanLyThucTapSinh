using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapRegistration(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            RegisterInternRequest request,
            IConfiguration configuration,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            var studentId = request.StudentId.Trim();
            var email = request.Email.Trim();
            if (await db.InternProfiles.AnyAsync(profile =>
                    profile.StudentId == studentId || profile.Email == email))
            {
                return Results.Conflict(new { message = "Mã sinh viên hoặc email đã được đăng ký." });
            }

            var profile = new InternProfile
            {
                Name = request.Name.Trim(),
                StudentId = studentId,
                Email = email,
                School = request.School.Trim(),
                Major = request.Major.Trim(),
                PasswordHash = string.Empty,
                Status = "Chờ hồ sơ"
            };
            profile.PasswordHash = passwordHasher.HashPassword(profile, request.Password);
            db.InternProfiles.Add(profile);
            await db.SaveChangesAsync();

            var verification = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profile.Id, Title = "Email verification", Status = "Unverified", Detail = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(verification))), End = DateTimeOffset.UtcNow.AddDays(1), UniqueKey = $"access:{profile.Id}" });
            var publicUrl = (configuration["PublicUrl"] ?? "http://localhost:5134").TrimEnd('/');
            db.MailJobs.Add(new MailJob { Recipient = profile.Email, Subject = "Xác thực email đăng ký", Body = $"Mở liên kết để xác thực email (hiệu lực 24 giờ): {publicUrl}/index.html?verify={verification}&profileId={profile.Id}" });
            await db.SaveChangesAsync();

            return Results.Created($"/api/interns/{profile.Id}/workspace", profile.ToResponse());
        })
        .WithName("RegisterIntern");
    }
}

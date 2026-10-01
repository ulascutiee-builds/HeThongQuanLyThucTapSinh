using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapLogin(RouteGroupBuilder group)
    {
        group.MapPost("/login", async (
            LoginInternRequest request,
            HttpContext context,
            CareerDbContext db,
            IPasswordHasher<InternProfile> passwordHasher) =>
        {
            var identity = request.Identity.Trim();
            var profile = await db.InternProfiles.FirstOrDefaultAsync(item =>
                item.StudentId == identity || item.Email == identity);
            if (profile is null || passwordHasher.VerifyHashedPassword(
                    profile, profile.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Json(
                    new { message = "Mã sinh viên/email hoặc mật khẩu không chính xác." },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (await db.WorkItems.AnyAsync(x => x.Kind == "intern-access" && x.ProfileId == profile.Id && x.Status == "Disabled")) return Results.Unauthorized();

            await PortalSecurity.SignIn(context, profile.Id, "Intern", profile.Name);
            return Results.Ok(profile.ToResponse());
        })
        .WithName("LoginIntern");
    }
}

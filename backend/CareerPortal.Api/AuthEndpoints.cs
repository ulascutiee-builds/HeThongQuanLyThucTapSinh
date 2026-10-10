using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class AuthEndpoints
{
    public static bool RequireEmailVerification(IConfiguration config) => config.GetValue("Sprint1:RequireEmailVerification", false);
    public static void Verification(CareerDbContext db, InternProfile profile, IConfiguration config)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        profile.VerificationHash = Accounts.Hash(token); profile.VerificationExpires = DateTimeOffset.UtcNow.AddHours(24);
        var verifyUrl = $"{(config["PublicUrl"] ?? "http://localhost:5135").TrimEnd('/')}/dang-nhap.html?verify={Uri.EscapeDataString(token)}&profileId={profile.Id}";
        db.MailJobs.Add(new()
        {
            EventKey = "verify:" + Guid.NewGuid(), Recipient = profile.Email, Subject = "Xác thực tài khoản thực tập", IsHtml = true,
            Body = $"<html><body style=\"font-family:Arial,sans-serif\"><p>Xin chào {WebUtility.HtmlEncode(profile.Name)},</p><p>Nhấn liên kết dưới đây trong vòng 24 giờ để xác thực email:</p><p><a href=\"{WebUtility.HtmlEncode(verifyUrl)}\">Xác thực email</a></p></body></html>"
        });
    }
    static async Task<IResult> Login(LoginInternRequest input, HttpContext context, CareerDbContext db, IPasswordHasher<AppUser> hasher)
    {
        var email = input.Identity.Trim().ToLowerInvariant();
        var user = await db.AppUsers.Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x!.RolePermissions).Include(x => x.UserPermissions).SingleOrDefaultAsync(x => x.Email == email);
        if (user is null || !user.IsActive || user.RequiresActivation || string.IsNullOrEmpty(user.PasswordHash)) return Results.Json(new { message = "Tài khoản hoặc mật khẩu không đúng, chưa kích hoạt hoặc đã bị vô hiệu hóa." }, statusCode: 401);
        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
        if (verification == PasswordVerificationResult.Failed) return Results.Json(new { message = "Tài khoản hoặc mật khẩu không đúng." }, statusCode: 401);
        if (verification == PasswordVerificationResult.SuccessRehashNeeded) { user.PasswordHash = hasher.HashPassword(user, input.Password); await db.SaveChangesAsync(); }
        var roles = user.UserRoles.Select(x => x.RoleName).ToArray();
        var permissions = Accounts.EffectivePermissions(user);
        if (roles.Length == 0) return Results.Json(new { message = "Tài khoản chưa được phân quyền." }, statusCode: 403);
        await SprintSecurity.SignIn(context, user, roles, permissions);
        return Results.Ok(SprintSecurity.IdentityResponse(context));
    }

    public static void MapSprintAuth(this WebApplication app)
    {
        app.MapPost("/api/auth/login", Login);
        app.MapPost("/api/interns/login", Login);
        app.MapGet("/api/auth/me", (HttpContext context) => Results.Ok(SprintSecurity.IdentityResponse(context)));
        app.MapPost("/api/auth/logout", async (HttpContext context) => { await context.SignOutAsync("Sprint1"); return Results.Ok(); });
        app.MapPost("/api/interns/register", async (RegisterInternRequest input, CareerDbContext db, IPasswordHasher<InternProfile> hasher, IConfiguration config) =>
        {
            var email = input.Email.Trim().ToLowerInvariant(); var phone = Profiles.NormalizePhone(input.Phone);
            if (input.DateOfBirth is DateOnly birthDate && birthDate > InternshipStatusSync.Today) return Results.ValidationProblem(new Dictionary<string, string[]> { ["DateOfBirth"] = ["Ngày sinh không hợp lệ."] });
            var errors = new Dictionary<string, string[]>();
            if (await db.InternProfiles.AnyAsync(x => x.Email == email) || await Accounts.EmailExists(db, email)) errors["Email"] = ["Email này đã được sử dụng."];
            if (await Profiles.PhoneExistsAsync(db, phone)) errors["Phone"] = ["Số điện thoại này đã được sử dụng."];
            if (errors.Count > 0) return Results.ValidationProblem(errors, statusCode: StatusCodes.Status409Conflict);
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var profile = new InternProfile
            {
                Name = input.Name.Trim(), Email = email, StudentId = input.StudentId.Trim(), Phone = phone, DateOfBirth = input.DateOfBirth,
                School = input.School.Trim(), Major = input.Major.Trim(), PasswordHash = "", Status = "Chờ hồ sơ", EmailVerified = !RequireEmailVerification(config)
            };
            profile.PasswordHash = hasher.HashPassword(profile, input.Password); db.InternProfiles.Add(profile); await db.SaveChangesAsync();
            Accounts.CreateInternAccount(db, profile, profile.PasswordHash);
            if (RequireEmailVerification(config)) Verification(db, profile, config);
            await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Created($"/api/interns/{profile.Id}/workspace", profile.ToResponse());
        });
        app.MapPost("/api/auth/verify-email", async (VerificationInput input, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(input.ProfileId);
            if (profile is null || profile.EmailVerified || profile.VerificationExpires is not DateTimeOffset expiry || expiry <= DateTimeOffset.UtcNow || profile.VerificationHash != Accounts.Hash(input.Token)) return Results.BadRequest(new { message = "Liên kết xác thực không hợp lệ hoặc hết hạn." });
            profile.EmailVerified = true; profile.VerificationHash = null; profile.VerificationExpires = null; await db.SaveChangesAsync(); return Results.Ok(new { message = "Email đã xác thực. Bạn có thể nộp hồ sơ." });
        });
        app.MapPost("/api/auth/resend-verification", async (HttpContext context, CareerDbContext db, IConfiguration config) =>
        {
            if (!context.User.IsInRole("Intern")) return Results.Forbid(); var profile = await db.InternProfiles.FindAsync(SprintSecurity.Id(context)); if (profile is null) return Results.NotFound();
            if (profile.EmailVerified) return Results.BadRequest(new { message = "Email đã được xác thực." });
            if (profile.VerificationExpires > DateTimeOffset.UtcNow.AddHours(24).AddMinutes(-1)) return Results.BadRequest(new { message = "Vui lòng đợi một phút trước khi gửi lại." });
            Verification(db, profile, config); await db.SaveChangesAsync(); return Results.Ok(new { message = "Đã xếp thư xác thực vào hàng đợi." });
        });
    }
}
public sealed record VerificationInput
{
    public int ProfileId { get; init; }
    [Required, MaxLength(200)] public required string Token { get; init; }
}

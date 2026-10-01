using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

public static partial class PortalEndpoints
{
    private static void MapEmailVerification(WebApplication app)
    {
        app.MapPost("/api/auth/verify-email", async (VerifyInput input, CareerDbContext db) => {
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == input.ProfileId);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Token)));
            if (row is null || row.Status != "Unverified" || row.End < DateTimeOffset.UtcNow || row.Detail != hash) return Error("Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
            row.Status = "Verified"; row.Detail = ""; await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPost("/api/auth/resend-verification", async (HttpContext c, CareerDbContext db, IConfiguration config) => {
            if (!c.User.IsInRole("Intern")) return Results.Forbid();
            var id = PortalSecurity.UserId(c.User); var p = await db.InternProfiles.FindAsync(id);
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == id);
            if (p is null || row is null || row.Status != "Unverified") return Error("Tài khoản không cần xác thực lại.");
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); row.Detail = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))); row.End = DateTimeOffset.UtcNow.AddDays(1);
            db.MailJobs.Add(new MailJob { Recipient = p.Email, Subject = "Xác thực email đăng ký", Body = $"{(config["PublicUrl"] ?? "http://localhost:5134").TrimEnd('/')}/index.html?verify={token}&profileId={id}" }); await db.SaveChangesAsync(); return Results.Ok();
        });
    }
}

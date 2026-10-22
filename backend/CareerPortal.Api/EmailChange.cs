using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

public sealed class PendingEmailChange
{
    public int AccountId { get; set; }
    public AppUser? Account { get; set; }
    public required string NewEmail { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed record ConfirmEmailChangeRequest
{
    public int AccountId { get; init; }
    [Required, MaxLength(200)] public required string Token { get; init; }
}

public static class EmailChange
{
    // The caller owns SaveChanges/transaction so the profile edit and outgoing mail are atomic.
    public static async Task<string?> QueueAsync(CareerDbContext db, AppUser account, string newEmail, IConfiguration config)
    {
        newEmail = newEmail.Trim().ToLowerInvariant();
        if (account.Email == newEmail) return null;
        var ownProfileId = account.ProfileId.GetValueOrDefault();
        if (await db.AppUsers.AnyAsync(x => x.Id != account.Id && x.Email == newEmail)
            || await db.InternProfiles.AnyAsync(x => x.Id != ownProfileId && x.Email == newEmail))
            return "Email này đã được sử dụng bởi tài khoản hoặc hồ sơ khác.";

        var now = DateTimeOffset.UtcNow;
        var existing = await db.PendingEmailChanges.SingleOrDefaultAsync(x => x.AccountId == account.Id);
        if (existing is not null && existing.NewEmail == newEmail && existing.ExpiresAt > now && existing.RequestedAt > now.AddMinutes(-1))
            return null; // Keep the first valid link instead of sending duplicate mail.
        if (await db.PendingEmailChanges.AnyAsync(x => x.AccountId != account.Id && x.NewEmail == newEmail && x.ExpiresAt > now))
            return "Email này đang chờ xác nhận cho một tài khoản khác.";

        // Expired requests cannot reserve an address indefinitely.
        await db.PendingEmailChanges.Where(x => x.AccountId != account.Id && x.NewEmail == newEmail && x.ExpiresAt <= now).ExecuteDeleteAsync();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        if (existing is null)
        {
            existing = new PendingEmailChange { AccountId = account.Id, NewEmail = newEmail, TokenHash = Accounts.Hash(token) };
            db.PendingEmailChanges.Add(existing);
        }
        else { existing.NewEmail = newEmail; existing.TokenHash = Accounts.Hash(token); }
        existing.RequestedAt = now;
        existing.ExpiresAt = now.AddHours(24);
        var url = $"{(config["PublicUrl"] ?? "http://localhost:5135").TrimEnd('/')}/xac-nhan-email.html?accountId={account.Id}&token={Uri.EscapeDataString(token)}";
        db.MailJobs.Add(new MailJob
        {
            EventKey = "email-change:" + account.Id + ":" + Guid.NewGuid().ToString("N"),
            Recipient = newEmail,
            Subject = "Xác nhận thay đổi email tài khoản",
            IsHtml = true,
            Body = $"<html><body style=\"font-family:Arial,sans-serif;line-height:1.6;color:#172b4d\"><p>Xin chào {WebUtility.HtmlEncode(account.Name)},</p><p>Bạn đã yêu cầu dùng địa chỉ này cho tài khoản Career Portal. Email hiện tại chỉ được thay đổi sau khi bạn xác nhận.</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">Mở biểu mẫu xác nhận đổi email</a></p><p>Liên kết có hiệu lực 24 giờ và chỉ sử dụng một lần. Nếu bạn không yêu cầu, hãy bỏ qua thư này.</p></body></html>"
        });
        return null;
    }

    public static void MapEmailChange(this WebApplication app)
    {
        app.MapPost("/api/auth/email-change/confirm", async (ConfirmEmailChangeRequest input, CareerDbContext db, IConfiguration config) =>
        {
            if (input.AccountId <= 0 || string.IsNullOrWhiteSpace(input.Token))
                return Results.BadRequest(new { message = "Liên kết xác nhận không hợp lệ." });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var pending = await db.PendingEmailChanges.SingleOrDefaultAsync(x => x.AccountId == input.AccountId);
            if (pending is null || pending.ExpiresAt <= DateTimeOffset.UtcNow
                || pending.TokenHash != Accounts.Hash(input.Token))
                return Results.BadRequest(new { message = "Liên kết xác nhận không hợp lệ, đã sử dụng hoặc hết hạn. Vui lòng lưu lại email trong hồ sơ để nhận liên kết mới." });
            var account = await db.AppUsers.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == pending.AccountId);
            if (account is null)
                return Results.BadRequest(new { message = "Tài khoản không còn tồn tại." });
            var ownProfileId = account.ProfileId.GetValueOrDefault();
            if (await db.AppUsers.AnyAsync(x => x.Id != account.Id && x.Email == pending.NewEmail)
                || await db.InternProfiles.AnyAsync(x => x.Id != ownProfileId && x.Email == pending.NewEmail))
                return Results.Conflict(new { message = "Email mới đã được sử dụng. Hãy lưu một địa chỉ khác." });

            account.Email = pending.NewEmail;
            if (account.Profile is not null)
            {
                account.Profile.Email = pending.NewEmail;
                account.Profile.EmailVerified = true;
                account.Profile.VerificationHash = null;
                account.Profile.VerificationExpires = null;
            }
            if (account.RequiresActivation && account.IsActive)
                Accounts.QueueActivation(db, account, config);
            db.PendingEmailChanges.Remove(pending);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Ok(new { message = account.RequiresActivation
                ? "Đã xác nhận email mới. Một thư đặt mật khẩu/kích hoạt vừa được gửi đến địa chỉ này."
                : "Đã xác nhận email mới. Từ bây giờ hãy dùng địa chỉ này để đăng nhập." });
        });
    }
}

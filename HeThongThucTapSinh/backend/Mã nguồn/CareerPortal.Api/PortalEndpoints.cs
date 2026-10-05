using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class PortalEndpoints
{
    static IResult Error(string text) => Results.BadRequest(new { message = text });
    static readonly PasswordHasher<PortalAccount> Hasher = new();
    public static void MapPortalEndpoints(this WebApplication app)
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
        app.MapPost("/api/admin/interns/{id:int}/account", async (int id, InternAccountInput input, HttpContext c, CareerDbContext db, IPasswordHasher<InternProfile> hasher) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid(); var p = await db.InternProfiles.FindAsync(id); if (p is null) return Results.NotFound();
            if (input.Password.Length > 0 && input.Password.Length < 8) return Error("Mật khẩu tối thiểu 8 ký tự.");
            if (input.Password.Length > 0) p.PasswordHash = hasher.HashPassword(p, input.Password);
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == id);
            if (row is null) { row = new WorkItem { Kind = "intern-access", ProfileId = id, Title = "Account access", Status = "Verified", UniqueKey = $"access:{id}" }; db.WorkItems.Add(row); }
            if (!input.Active) { row.Feedback = row.Status; row.Status = "Disabled"; }
            else if (row.Status == "Disabled") row.Status = row.Feedback == "Unverified" ? "Unverified" : "Verified";
            await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPost("/api/auth/bootstrap", async (AccountInput input, HttpContext c, CareerDbContext db, IConfiguration config) =>
        {
            var token = config["BootstrapToken"];
            if (string.IsNullOrEmpty(token) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(c.Request.Headers["X-Bootstrap-Token"].ToString()))) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await db.PortalAccounts.AnyAsync(x => x.Role == "Admin")) return Results.Conflict(new { message = "Admin đã được khởi tạo." });
            if (input.Password.Length < 12 || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _)) return Error("Email hợp lệ và mật khẩu tối thiểu 12 ký tự.");
            var account = new PortalAccount { Name = input.Name, Email = input.Email.Trim().ToLowerInvariant(), Role = "Admin" };
            account.PasswordHash = Hasher.HashPassword(account, input.Password);
            db.PortalAccounts.Add(account); await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new { account.Id });
        });
        app.MapPost("/api/auth/login", async (LoginInternRequest input, HttpContext c, CareerDbContext db) =>
        {
            var email = input.Identity.Trim().ToLowerInvariant();
            var a = await db.PortalAccounts.FirstOrDefaultAsync(x => x.Email == email && x.Active);
            if (a is null || Hasher.VerifyHashedPassword(a, a.PasswordHash, input.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
            await PortalSecurity.SignIn(c, a.Id, a.Role, a.Name);
            return Results.Ok(new { a.Id, a.Name, a.Role });
        });
        app.MapGet("/api/auth/me", (HttpContext c) => Results.Ok(new { id = PortalSecurity.UserId(c.User), name = c.User.Identity!.Name, role = c.User.FindFirstValue(ClaimTypes.Role) }));
        app.MapPost("/api/auth/logout", async (HttpContext c) => { await c.SignOutAsync("Portal"); return Results.NoContent(); });
        app.MapGet("/api/accounts", async (HttpContext c, CareerDbContext db) =>
            PortalSecurity.Manager(c.User) ? Results.Ok(await db.PortalAccounts.Select(x => new { x.Id, x.Name, x.Email, x.Role, x.Active, x.Permissions }).ToListAsync()) : Results.Forbid());
        app.MapPost("/api/accounts", async (AccountInput input, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            if (input.Role is not ("HR" or "Mentor" or "Admin") || input.Password.Length < 12 || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _)) return Error("Vai trò hợp lệ; email hợp lệ; mật khẩu từ 12 ký tự.");
            var account = new PortalAccount { Name = input.Name, Email = input.Email.Trim().ToLowerInvariant(), Role = input.Role, Active = input.Active, Permissions = input.Permissions };
            account.PasswordHash = Hasher.HashPassword(account, input.Password);
            db.PortalAccounts.Add(account); await db.SaveChangesAsync(); return Results.Ok(new { account.Id });
        });
        app.MapPut("/api/accounts/{id:int}", async (int id, AccountInput input, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var account = await db.PortalAccounts.FindAsync(id); if (account is null) return Results.NotFound();
            if (id == PortalSecurity.UserId(c.User)) return Error("Không thay đổi quyền tài khoản đang đăng nhập.");
            if (input.Role is not ("HR" or "Mentor" or "Admin")) return Error("Vai trò không hợp lệ.");
            if (!System.Net.Mail.MailAddress.TryCreate(input.Email, out _)) return Error("Email không hợp lệ.");
            if (input.Password.Length > 0 && input.Password.Length < 12) return Error("Mật khẩu từ 12 ký tự.");
            account.Name = input.Name; account.Email = input.Email.Trim().ToLowerInvariant(); account.Role = input.Role; account.Active = input.Active; account.Permissions = input.Permissions;
            if (input.Password.Length > 0) account.PasswordHash = Hasher.HashPassword(account, input.Password);
            await db.SaveChangesAsync(); return Results.NoContent();
        });
        app.MapGet("/api/people", async (HttpContext c, CareerDbContext db) => {
            var q = db.InternProfiles.AsNoTracking(); var uid = PortalSecurity.UserId(c.User);
            if (c.User.IsInRole("Intern")) q = q.Where(x => x.Id == uid);
            else if (!PortalSecurity.Manager(c.User)) { var ids = PortalWorkflow.Visible(db, c.User, "assignments").Select(x => x.ProfileId); q = q.Where(x => ids.Contains(x.Id)); }
            return Results.Ok(await q.Select(x => new { x.Id, x.Name, x.StudentId, x.School, x.Major, x.Status, x.StartDate, x.EndDate }).ToListAsync());
        });
        app.MapGet("/api/work/{kind}", async (string kind, HttpContext c, CareerDbContext db) =>
            !PortalWorkflow.Kinds.Contains(kind) ? Results.NotFound() : Results.Ok(await PortalWorkflow.Visible(db, c.User, kind).AsNoTracking().OrderByDescending(x => x.Id).ToListAsync()));
        app.MapPost("/api/work/{kind}", async (string kind, WorkItem input, HttpContext c, CareerDbContext db) => await Save(kind, null, input, c, db));
        app.MapPut("/api/work/{kind}/{id:int}", async (string kind, int id, WorkItem input, HttpContext c, CareerDbContext db) => await Save(kind, id, input, c, db));
        app.MapDelete("/api/work/{kind}/{id:int}", async (string kind, int id, HttpContext c, CareerDbContext db) =>
        {
            if (!await PortalWorkflow.CanWrite(db, c.User, kind) || kind is "attendance" or "notifications") return Results.Forbid();
            var row = await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id); if (row is null) return Results.NotFound();
            if (await db.WorkItems.AnyAsync(x => x.ProgramId == id || x.MentorId == id)) return Error("Bản ghi đang được sử dụng; hãy cập nhật thay vì xóa.");
            db.WorkItems.Remove(row); await db.SaveChangesAsync(); return Results.NoContent();
        });
        app.MapPost("/api/work/{kind}/{id:int}/action", Action);
        app.MapPost("/api/attendance/{action}", async (string action, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Intern")) return Results.Forbid();
            var id = PortalSecurity.UserId(c.User); var key = $"attendance:{id}:{DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):yyyyMMdd}";
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.UniqueKey == key);
            if (action == "check-in")
            {
                if (row is not null) return Error("Đã check-in hôm nay.");
                db.WorkItems.Add(new WorkItem { Kind = "attendance", Title = "Chấm công", ProfileId = id, Start = DateTimeOffset.UtcNow, UniqueKey = key, Status = "Đang làm" });
            }
            else if (action == "check-out" && row is not null && row.End is null) { row.End = DateTimeOffset.UtcNow; row.Status = "Đã kết thúc"; }
            else return Error("Cần check-in trước hoặc đã check-out.");
            await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPost("/api/work/{kind}/{id:int}/attachment", async (string kind, int id, HttpContext c, CareerDbContext db) =>
        {
            if (kind is not ("reports" or "support") || !await PortalWorkflow.CanWrite(db, c.User, kind)) return Results.Forbid();
            var row = await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id); if (row is null) return Results.NotFound();
            var name = Path.GetFileName(c.Request.Query["fileName"].ToString());
            if (!new[] { ".pdf", ".doc", ".docx", ".png", ".jpg" }.Contains(Path.GetExtension(name).ToLowerInvariant())) return Error("Định dạng không hỗ trợ.");
            using var stream = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await c.Request.Body.ReadAsync(buffer)) > 0) { if (stream.Length + count > 10 * 1024 * 1024) return Results.StatusCode(413); await stream.WriteAsync(buffer.AsMemory(0, count)); }
            if (stream.Length == 0) return Error("Tệp rỗng.");
            row.FileName = name; row.Attachment = stream.ToArray(); await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapGet("/api/work/{kind}/{id:int}/attachment", async (string kind, int id, HttpContext c, CareerDbContext db) => {
            var row = await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id);
            return row?.Attachment is null ? Results.NotFound() : Results.File(row.Attachment, "application/octet-stream", row.FileName);
        });
        app.MapGet("/api/admin/audit", async (HttpContext c, CareerDbContext db, string? search, int page = 1) => c.User.IsInRole("Admin")
            ? Results.Ok(await db.PortalAudits.Where(x => search == null || x.Actor.Contains(search) || x.Resource.Contains(search)).OrderByDescending(x => x.Id).Skip((Math.Max(1, page) - 1) * 100).Take(100).ToListAsync()) : Results.Forbid());
        app.MapGet("/api/admin/mail", async (HttpContext c, CareerDbContext db) => c.User.IsInRole("Admin") ? Results.Ok(await db.MailJobs.OrderByDescending(x => x.Id).Take(100).ToListAsync()) : Results.Forbid());
        app.MapPost("/api/admin/mail/{id:int}/retry", async (int id, HttpContext c, CareerDbContext db) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid(); var row = await db.MailJobs.FindAsync(id); if (row is null) return Results.NotFound();
            if (row.Status == "Sent") return Error("Email đã gửi."); row.Status = "Queued"; row.Attempts = 0; row.DueAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPortalReports();
    }
    static async Task<IResult> Save(string kind, int? id, WorkItem input, HttpContext c, CareerDbContext db)
    {
        if (!PortalWorkflow.Kinds.Contains(kind)) return Results.NotFound();
        if (kind is "attendance" or "notifications" || !await PortalWorkflow.CanWrite(db, c.User, kind)) return Results.Forbid();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var row = id is null ? new WorkItem { Kind = kind } : await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id);
        if (row is null) return Results.NotFound();
        if (id is not null && !row.Version.SequenceEqual(input.Version)) return Results.Conflict(new { message = "Dữ liệu đã đổi; tải lại trước khi lưu." });
        row.Title = input.Title; row.Detail = input.Detail; row.ProfileId = c.User.IsInRole("Intern") ? PortalSecurity.UserId(c.User) : input.ProfileId;
        row.ProgramId = input.ProgramId; row.MentorId = input.MentorId; row.Department = input.Department; row.Start = input.Start; row.End = input.End;
        row.Amount = input.Amount; row.Capacity = input.Capacity;
        if (kind == "evaluations") row.Progress = input.Progress;
        if (id is null && kind is "leave" or "support") row.Status = "Chờ duyệt";
        var problem = await PortalWorkflow.Validate(db, row, c.User); if (problem is not null) return Error(problem);
        row.History += $"{DateTimeOffset.UtcNow:O} {c.User.Identity!.Name}: {(id is null ? "Tạo" : "Cập nhật")}\n";
        if (id is null) db.WorkItems.Add(row);
        if (kind == "programs" && id is not null)
        {
            var assignments = await db.WorkItems.Where(x => x.Kind == "assignments" && x.ProgramId == id).ToListAsync();
            foreach (var a in assignments) { a.Start = row.Start; a.End = row.End; var p = await db.InternProfiles.FindAsync(a.ProfileId); if (p is not null) { p.StartDate = row.Start is null ? null : DateOnly.FromDateTime(row.Start.Value.Date); p.EndDate = row.End is null ? null : DateOnly.FromDateTime(row.End.Value.Date); } }
        }
        if (row.ProfileId is int pid && kind == "assignments") { var p = await db.InternProfiles.FindAsync(pid); if (p is not null) { p.StartDate = DateOnly.FromDateTime(row.Start!.Value.Date); p.EndDate = DateOnly.FromDateTime(row.End!.Value.Date); } }
        if (row.ProfileId is int target && kind is "tasks" or "meetings" or "assignments" or "shifts") PortalWorkflow.Notify(db, target, row.Title, row.Detail + $"\n{row.Start} - {row.End}", kind == "meetings");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(row);
    }
    static async Task<IResult> Action(string kind, int id, ActionInput input, HttpContext c, CareerDbContext db)
    {
        if (!await PortalWorkflow.HasPermission(db, c.User, kind)) return Results.Forbid();
        var row = await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id); if (row is null) return Results.NotFound();
        var manager = PortalSecurity.Manager(c.User);
        if (kind == "tasks" && input.Action == "progress" && (manager || c.User.IsInRole("Intern") || c.User.IsInRole("Mentor")))
        { if (input.Progress is < 0 or > 100) return Error("Tiến độ phải từ 0 đến 100%."); row.Progress = input.Progress; row.Status = input.Progress == 100 ? "Hoàn thành" : "Đang làm"; }
        else if (kind == "reports" && input.Action == "feedback" && (manager || c.User.IsInRole("Mentor"))) { if (string.IsNullOrWhiteSpace(input.Feedback)) return Error("Nhập phản hồi."); row.Feedback = input.Feedback; }
        else if (kind == "notifications" && input.Action == "read" && c.User.IsInRole("Intern")) row.Status = "Đã đọc";
        else if (kind is "leave" or "support" && input.Action is "approve" or "reject" or "close" && manager)
        { if (input.Action == "reject" && string.IsNullOrWhiteSpace(input.Feedback)) return Error("Nhập lý do từ chối."); row.Status = input.Action == "approve" ? "Đã duyệt" : input.Action == "reject" ? "Từ chối" : "Đã đóng"; row.Feedback = input.Feedback; }
        else if (kind == "allowances" && input.Action == "paid" && manager) row.Status = "Đã thanh toán";
        else return Results.Forbid();
        row.History += $"{DateTimeOffset.UtcNow:O} {c.User.Identity!.Name}: {input.Action} {input.Progress} {input.Feedback}\n";
        if (row.ProfileId is int target && kind != "notifications") PortalWorkflow.Notify(db, target, row.Title, row.Status + " " + row.Feedback);
        await db.SaveChangesAsync(); return Results.Ok(row);
    }
}
public record VerifyInput(int ProfileId, string Token);
public record InternAccountInput(string Password, bool Active);

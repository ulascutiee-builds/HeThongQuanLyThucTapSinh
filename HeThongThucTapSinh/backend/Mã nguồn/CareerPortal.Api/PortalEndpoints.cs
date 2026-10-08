using System.Security.Claims;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class PortalEndpoints
{
    static IResult Error(string text) => Results.BadRequest(new { message = text });
    static readonly PasswordHasher<PortalAccount> Hasher = new();
    static readonly string[] AccountRoles = ["HR", "Mentor", "Intern", "Admin"];

    static void QueueActivationEmail(CareerDbContext db, PortalAccount account, IConfiguration config)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        account.ActivationTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        account.ActivationExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        account.EmailVerifiedAt = null;
        account.Active = false;

        var publicUrl = config["PublicUrl"];
        if (string.IsNullOrWhiteSpace(publicUrl)) throw new InvalidOperationException("Cần cấu hình PublicUrl trước khi tạo tài khoản.");
        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && !(baseUri.IsLoopback && baseUri.Scheme == Uri.UriSchemeHttp)))
            throw new InvalidOperationException("PublicUrl phải dùng HTTPS (HTTP chỉ được phép trên localhost).");

        var activationUrl = $"{publicUrl.TrimEnd('/')}/index.html?activate={Uri.EscapeDataString(token)}";
        db.MailJobs.Add(new MailJob
        {
            Recipient = account.Email,
            Subject = "Kích hoạt tài khoản thực tập",
            Body = $"Xin chào {account.Name},\n\nMở liên kết sau trong vòng 24 giờ để xác thực email và tự đặt mật khẩu: {activationUrl}\n\nNếu bạn không yêu cầu tài khoản này, hãy bỏ qua email."
        });
    }

    public static void MapPortalEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/verify-email", async (VerifyInput input, CareerDbContext db) => {
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == input.ProfileId);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Token)));
            if (row is null || row.Status != "Unverified" || row.End < DateTimeOffset.UtcNow || row.Detail != hash) return Error("Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
            row.Status = "Verified"; row.Detail = ""; await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPost("/api/auth/activate-account", async (ActivateAccountInput input, CareerDbContext db, IPasswordHasher<InternProfile> internHasher) => {
            if (string.IsNullOrWhiteSpace(input.Token) || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 12 || input.Password.Length > 200)
                return Error("Mã kích hoạt không hợp lệ hoặc mật khẩu phải từ 12 đến 200 ký tự.");
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Token)));
            var account = await db.PortalAccounts.SingleOrDefaultAsync(x => x.ActivationTokenHash == hash);
            if (account is null || account.ActivationExpiresAt <= DateTimeOffset.UtcNow)
                return Error("Liên kết kích hoạt không hợp lệ hoặc đã hết hạn.");

            account.PasswordHash = Hasher.HashPassword(account, input.Password);
            account.Active = true;
            account.EmailVerifiedAt = DateTimeOffset.UtcNow;
            account.ActivationTokenHash = null;
            account.ActivationExpiresAt = null;
            if (account.InternProfileId is int profileId)
            {
                var profile = await db.InternProfiles.FindAsync(profileId);
                if (profile is null) return Results.Conflict(new { message = "Hồ sơ thực tập sinh liên kết không còn tồn tại." });
                profile.PasswordHash = internHasher.HashPassword(profile, input.Password);
                var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                if (access is null)
                    db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profileId, Title = "Account access", Status = "Verified", UniqueKey = $"access:{profileId}" });
                else { access.Status = "Verified"; access.Detail = ""; access.End = null; }
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Tài khoản đã được kích hoạt. Bạn có thể đăng nhập." });
        });
        app.MapPost("/api/auth/resend-verification", async (HttpContext c, CareerDbContext db, IConfiguration config) => {
            if (!c.User.IsInRole("Intern")) return Results.Forbid();
            var id = PortalSecurity.UserId(c.User); var p = await db.InternProfiles.FindAsync(id);
            var row = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == id);
            if (p is null || row is null || row.Status != "Unverified") return Error("Tài khoản không cần xác thực lại.");
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); row.Detail = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))); row.End = DateTimeOffset.UtcNow.AddDays(1);
            db.MailJobs.Add(new MailJob { Recipient = p.Email, Subject = "Xác thực email đăng ký", Body = $"{(config["PublicUrl"] ?? "http://localhost:5134").TrimEnd('/')}/index.html?verify={token}&profileId={id}" }); await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPost("/api/admin/interns/{id:int}/account", async (int id, InternAccountInput input, HttpContext c, CareerDbContext db, IConfiguration config) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var profile = await db.InternProfiles.FindAsync(id); if (profile is null) return Results.NotFound();
            if (!string.IsNullOrEmpty(input.Password)) return Error("Không gửi mật khẩu cho quản trị viên; thực tập sinh sẽ tự đặt qua email kích hoạt.");

            var account = await db.PortalAccounts.FirstOrDefaultAsync(x => x.InternProfileId == id);
            var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == id);
            if (!input.Active)
            {
                if (account is not null) { account.Active = false; account.ActivationTokenHash = null; account.ActivationExpiresAt = null; }
                if (access is not null) { access.Feedback = access.Status; access.Status = "Disabled"; }
                await db.SaveChangesAsync();
                return Results.Ok(new { active = false });
            }
            if (account is null)
            {
                account = new PortalAccount { Name = profile.Name, Email = profile.Email.Trim().ToLowerInvariant(), Role = "Intern", InternProfileId = id };
                account.PasswordHash = Hasher.HashPassword(account, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
                db.PortalAccounts.Add(account);
                QueueActivationEmail(db, account, config);
                profile.PasswordHash = new PasswordHasher<InternProfile>().HashPassword(profile, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
                if (access is null) db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = id, Title = "Account access", Status = "Unverified", UniqueKey = $"access:{id}" });
            }
            else if (!account.Active && account.EmailVerifiedAt is null)
                QueueActivationEmail(db, account, config);
            else
            {
                account.Active = true;
                if (access is not null) { access.Status = "Verified"; access.Detail = ""; access.End = null; }
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { active = account.Active, activationRequired = account.ActivationTokenHash is not null });
        });
        app.MapPost("/api/auth/bootstrap", async (AccountInput input, HttpContext c, CareerDbContext db, IConfiguration config) =>
        {
            var token = config["BootstrapToken"];
            if (string.IsNullOrEmpty(token) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(c.Request.Headers["X-Bootstrap-Token"].ToString()))) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await db.PortalAccounts.AnyAsync(x => x.Role == "Admin")) return Results.Conflict(new { message = "Admin đã được khởi tạo." });
            if (input.Password.Length < 12 || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _)) return Error("Email hợp lệ và mật khẩu tối thiểu 12 ký tự.");
            var account = new PortalAccount { Name = input.Name.Trim(), Email = input.Email.Trim().ToLowerInvariant(), Role = "Admin", EmailVerifiedAt = DateTimeOffset.UtcNow };
            account.PasswordHash = Hasher.HashPassword(account, input.Password);
            db.PortalAccounts.Add(account); await db.SaveChangesAsync(); await tx.CommitAsync();
            return Results.Ok(new { account.Id });
        });
        app.MapPost("/api/auth/login", async (LoginInternRequest input, HttpContext c, CareerDbContext db, IPasswordHasher<InternProfile> internHasher) =>
        {
            var email = input.Identity.Trim().ToLowerInvariant();
            var account = await db.PortalAccounts.FirstOrDefaultAsync(x => x.Email == email && x.Active);
            if (account is null || Hasher.VerifyHashedPassword(account, account.PasswordHash, input.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
            if (account.Role == "Intern")
            {
                if (account.InternProfileId is not int profileId) return Results.Unauthorized();
                var profile = await db.InternProfiles.FindAsync(profileId);
                if (profile is null || internHasher.VerifyHashedPassword(profile, profile.PasswordHash, input.Password) == PasswordVerificationResult.Failed) return Results.Unauthorized();
                var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                if (access is not null && access.Status != "Verified") return Results.Forbid();
                await PortalSecurity.SignIn(c, profile.Id, account.Role, profile.Name);
                return Results.Ok(new { id = profile.Id, profile.Name, account.Role });
            }
            await PortalSecurity.SignIn(c, account.Id, account.Role, account.Name);
            return Results.Ok(new { account.Id, account.Name, account.Role });
        });
        app.MapGet("/api/auth/me", (HttpContext c) => Results.Ok(new { id = PortalSecurity.UserId(c.User), name = c.User.Identity!.Name, role = c.User.FindFirstValue(ClaimTypes.Role) }));
        app.MapPost("/api/auth/logout", async (HttpContext c) => { await c.SignOutAsync("Portal"); return Results.NoContent(); });
        app.MapGet("/api/accounts", async (HttpContext c, CareerDbContext db) =>
            PortalSecurity.Manager(c.User)
                ? Results.Ok(await db.PortalAccounts.AsNoTracking().Select(x => new { x.Id, x.Name, x.Email, x.Role, x.Active, x.Permissions, internProfileId = x.InternProfileId, profileName = x.InternProfile == null ? null : x.InternProfile.Name, studentId = x.InternProfile == null ? null : x.InternProfile.StudentId, activationPending = x.ActivationTokenHash != null, x.EmailVerifiedAt }).ToListAsync())
                : Results.Forbid());
        app.MapGet("/api/accounts/{id:int}", async (int id, HttpContext c, CareerDbContext db) => {
            if (!PortalSecurity.Manager(c.User)) return Results.Forbid();
            var account = await db.PortalAccounts.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Name, x.Email, x.Role, x.Active, x.Permissions, internProfileId = x.InternProfileId, profileName = x.InternProfile == null ? null : x.InternProfile.Name, studentId = x.InternProfile == null ? null : x.InternProfile.StudentId, activationPending = x.ActivationTokenHash != null, x.EmailVerifiedAt })
                .FirstOrDefaultAsync();
            return account is null ? Results.NotFound() : Results.Ok(account);
        });
        app.MapPost("/api/accounts", async (AccountInput input, HttpContext c, CareerDbContext db, IConfiguration config, IPasswordHasher<InternProfile> internHasher) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            if (!AccountRoles.Contains(input.Role) || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _) || string.IsNullOrWhiteSpace(input.Name))
                return Error("Nhập tên, email hợp lệ và vai trò HR/Mentor/Intern/Admin.");
            if (!string.IsNullOrEmpty(input.Password)) return Error("Không gửi mật khẩu trong yêu cầu tạo tài khoản; người dùng sẽ tự đặt mật khẩu qua email kích hoạt.");
            InternProfile? profile = null;
            if (input.Role == "Intern")
            {
                if (input.InternProfileId is not int profileId) return Error("Chọn hồ sơ thực tập sinh để liên kết tài khoản.");
                profile = await db.InternProfiles.FindAsync(profileId);
                if (profile is null) return Results.NotFound();
                if (!string.Equals(profile.Email.Trim(), input.Email.Trim(), StringComparison.OrdinalIgnoreCase)) return Error("Email tài khoản phải trùng email trong hồ sơ thực tập sinh.");
                if (await db.PortalAccounts.AnyAsync(x => x.InternProfileId == profileId)) return Error("Hồ sơ này đã có tài khoản.");
            }
            else if (input.InternProfileId is not null) return Error("Chỉ tài khoản Intern mới được liên kết hồ sơ thực tập sinh.");

            var normalizedEmail = input.Email.Trim().ToLowerInvariant();
            if (await db.PortalAccounts.AnyAsync(x => x.Email == normalizedEmail)) return Error("Email đã được dùng cho tài khoản khác.");
            var account = new PortalAccount
            {
                Name = profile?.Name ?? input.Name.Trim(), Email = profile?.Email.Trim().ToLowerInvariant() ?? normalizedEmail,
                Role = input.Role, Permissions = input.Permissions?.Trim() ?? "", InternProfileId = profile?.Id
            };
            account.PasswordHash = Hasher.HashPassword(account, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
            db.PortalAccounts.Add(account);
            if (profile is not null)
            {
                profile.PasswordHash = internHasher.HashPassword(profile, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
                var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profile.Id);
                if (access is null) db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profile.Id, Title = "Account access", Status = "Unverified", UniqueKey = $"access:{profile.Id}" });
                else { access.Status = "Unverified"; access.Feedback = ""; access.Detail = ""; }
            }
            QueueActivationEmail(db, account, config);
            await db.SaveChangesAsync();
            return Results.Created($"/api/accounts/{account.Id}", new { account.Id, activationRequired = true });
        });
        app.MapPut("/api/accounts/{id:int}", async (int id, AccountInput input, HttpContext c, CareerDbContext db, IConfiguration config, IPasswordHasher<InternProfile> internHasher) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var account = await db.PortalAccounts.FindAsync(id); if (account is null) return Results.NotFound();
            if (id == PortalSecurity.UserId(c.User)) return Error("Không thay đổi quyền tài khoản đang đăng nhập.");
            if (!AccountRoles.Contains(input.Role) || !System.Net.Mail.MailAddress.TryCreate(input.Email, out _) || string.IsNullOrWhiteSpace(input.Name)) return Error("Tên, email hoặc vai trò không hợp lệ.");
            if (!string.IsNullOrEmpty(input.Password)) return Error("Không đổi/gửi mật khẩu qua API quản trị; hãy gửi lại email kích hoạt để người dùng tự đặt mật khẩu.");
            var normalizedEmail = input.Email.Trim().ToLowerInvariant();
            if (await db.PortalAccounts.AnyAsync(x => x.Id != id && x.Email == normalizedEmail)) return Error("Email đã được dùng cho tài khoản khác.");
            InternProfile? profile = null;
            if (input.Role == "Intern")
            {
                if (input.InternProfileId is not int profileId) return Error("Chọn hồ sơ thực tập sinh để liên kết tài khoản.");
                profile = await db.InternProfiles.FindAsync(profileId);
                if (profile is null) return Results.NotFound();
                if (!string.Equals(profile.Email.Trim(), input.Email.Trim(), StringComparison.OrdinalIgnoreCase)) return Error("Email tài khoản phải trùng email trong hồ sơ thực tập sinh.");
                if (await db.PortalAccounts.AnyAsync(x => x.Id != id && x.InternProfileId == profileId)) return Error("Hồ sơ này đã liên kết với tài khoản khác.");
            }
            else if (input.InternProfileId is not null) return Error("Chỉ tài khoản Intern mới được liên kết hồ sơ thực tập sinh.");

            var previousProfileId = account.InternProfileId;
            var emailChanged = !string.Equals(account.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
            var roleOrProfileChanged = account.Role != input.Role || account.InternProfileId != profile?.Id;
            account.Name = profile?.Name ?? input.Name.Trim(); account.Email = profile?.Email.Trim().ToLowerInvariant() ?? normalizedEmail;
            account.Role = input.Role; account.Permissions = input.Permissions?.Trim() ?? ""; account.InternProfileId = profile?.Id;
            if (emailChanged || roleOrProfileChanged)
            {
                if (previousProfileId is int oldProfileId && oldProfileId != profile?.Id)
                {
                    var oldAccess = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == oldProfileId);
                    if (oldAccess is not null) { oldAccess.Feedback = oldAccess.Status; oldAccess.Status = "Disabled"; }
                }
                if (account.InternProfileId is int profileId && profile is not null)
                {
                    profile.PasswordHash = internHasher.HashPassword(profile, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
                    var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                    if (access is null) db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profileId, Title = "Account access", Status = "Unverified", UniqueKey = $"access:{profileId}" });
                    else { access.Status = "Unverified"; access.Feedback = ""; access.Detail = ""; }
                }
                QueueActivationEmail(db, account, config);
            }
            else
            {
                account.Active = input.Active && account.EmailVerifiedAt is not null;
                if (account.InternProfileId is int profileId)
                {
                    var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                    if (!input.Active && access is not null) { access.Feedback = access.Status; access.Status = "Disabled"; }
                    else if (account.Active && access?.Status == "Disabled") access.Status = access.Feedback == "Unverified" ? "Unverified" : "Verified";
                }
            }
            await db.SaveChangesAsync(); return Results.NoContent();
        });
        app.MapDelete("/api/accounts/{id:int}", async (int id, HttpContext c, CareerDbContext db) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var account = await db.PortalAccounts.FindAsync(id); if (account is null) return Results.NotFound();
            if (id == PortalSecurity.UserId(c.User)) return Error("Không thể khóa tài khoản đang đăng nhập.");
            account.Active = false; account.ActivationTokenHash = null; account.ActivationExpiresAt = null;
            if (account.InternProfileId is int profileId)
            {
                var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                if (access is not null) { access.Feedback = access.Status; access.Status = "Disabled"; }
            }
            await db.SaveChangesAsync(); return Results.NoContent();
        });
        app.MapPost("/api/accounts/{id:int}/resend-activation", async (int id, HttpContext c, CareerDbContext db, IConfiguration config) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var account = await db.PortalAccounts.FindAsync(id); if (account is null) return Results.NotFound();
            if (account.Active && account.EmailVerifiedAt is not null) return Error("Tài khoản này đã được kích hoạt.");
            QueueActivationEmail(db, account, config);
            if (account.InternProfileId is int profileId)
            {
                var access = await db.WorkItems.FirstOrDefaultAsync(x => x.Kind == "intern-access" && x.ProfileId == profileId);
                if (access is null) db.WorkItems.Add(new WorkItem { Kind = "intern-access", ProfileId = profileId, Title = "Account access", Status = "Unverified", UniqueKey = $"access:{profileId}" });
                else { access.Status = "Unverified"; access.Feedback = ""; access.Detail = ""; }
            }
            await db.SaveChangesAsync(); return Results.Ok(new { activationQueued = true });
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
        app.MapPost("/api/tasks", async (WorkItem input, HttpContext c, CareerDbContext db) => await Save("tasks", null, input, c, db));
        app.MapPost("/api/reports/weekly", async (WorkItem input, HttpContext c, CareerDbContext db) => await Save("reports", null, input, c, db));
        app.MapPut("/api/work/{kind}/{id:int}", async (string kind, int id, WorkItem input, HttpContext c, CareerDbContext db) => await Save(kind, id, input, c, db));
        app.MapDelete("/api/work/{kind}/{id:int}", async (string kind, int id, HttpContext c, CareerDbContext db) =>
        {
            if (!await PortalWorkflow.CanWrite(db, c.User, kind) || kind is "attendance" or "notifications") return Results.Forbid();
            var row = await PortalWorkflow.Visible(db, c.User, kind).FirstOrDefaultAsync(x => x.Id == id); if (row is null) return Results.NotFound();
            if (await db.WorkItems.AnyAsync(x => x.ProgramId == id || x.MentorId == id)) return Error("Bản ghi đang được sử dụng; hãy cập nhật thay vì xóa.");
            db.WorkItems.Remove(row); await db.SaveChangesAsync(); return Results.NoContent();
        });
        app.MapPost("/api/work/{kind}/{id:int}/action", Action);
        app.MapGet("/api/evaluations/criteria", async (CareerDbContext db) =>
            Results.Ok(await db.EvaluationCriteria.AsNoTracking().Where(x => x.Active).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.Name, x.Description, x.MaxScore, x.SortOrder }).ToListAsync()));
        app.MapGet("/api/evaluations/{id:int}/scores", async (int id, HttpContext c, CareerDbContext db) =>
        {
            var evaluation = await PortalWorkflow.Visible(db, c.User, "evaluations").AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (evaluation is null) return Results.NotFound();
            var criteria = await db.EvaluationCriteria.AsNoTracking().Where(x => x.Active).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
            var saved = await db.EvaluationScores.AsNoTracking().Where(x => x.EvaluationId == id)
                .ToDictionaryAsync(x => x.CriterionId);
            return Results.Ok(criteria.Select(x =>
            {
                saved.TryGetValue(x.Id, out var value);
                return new { criterionId = x.Id, x.Name, x.Description, x.MaxScore,
                    score = value is null ? (decimal?)null : value.Score, comment = value?.Comment ?? "" };
            }).ToList());
        });
        app.MapPut("/api/evaluations/{id:int}/scores", async (int id, EvaluationScoreInput[] input, HttpContext c, CareerDbContext db) =>
        {
            if (!await PortalWorkflow.CanWrite(db, c.User, "evaluations")) return Results.Forbid();
            var evaluation = await PortalWorkflow.Visible(db, c.User, "evaluations").FirstOrDefaultAsync(x => x.Id == id);
            if (evaluation is null) return Results.NotFound();
            var criteria = await db.EvaluationCriteria.Where(x => x.Active).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
            if (criteria.Count == 0 || input.Length != criteria.Count || input.Select(x => x.CriterionId).Distinct().Count() != input.Length)
                return Error("Gửi đúng một điểm cho mỗi tiêu chí đang hoạt động.");
            var byCriterion = input.ToDictionary(x => x.CriterionId);
            if (criteria.Any(x => !byCriterion.TryGetValue(x.Id, out var score) || score.Score < 0 || score.Score > x.MaxScore || (score.Comment?.Length ?? 0) > 2000) ||
                input.Any(x => !criteria.Any(criterion => criterion.Id == x.CriterionId)))
                return Error("Điểm phải nằm trong giới hạn của tiêu chí; nhận xét tối đa 2.000 ký tự.");

            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var existing = await db.EvaluationScores.Where(x => x.EvaluationId == id).ToListAsync();
            foreach (var criterion in criteria)
            {
                var submitted = byCriterion[criterion.Id];
                var score = existing.FirstOrDefault(x => x.CriterionId == criterion.Id);
                if (score is null)
                {
                    score = new EvaluationScore { EvaluationId = id, CriterionId = criterion.Id };
                    db.EvaluationScores.Add(score);
                }
                score.Score = submitted.Score;
                score.Comment = submitted.Comment?.Trim() ?? "";
                score.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (byCriterion.TryGetValue(1, out var skills)) evaluation.Amount = skills.Score;
            if (byCriterion.TryGetValue(2, out var attitude)) evaluation.Progress = (int)decimal.Round(attitude.Score, 0, MidpointRounding.AwayFromZero);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Ok(criteria.Select(x => { var submitted = byCriterion[x.Id];
                return new { criterionId = x.Id, x.Name, x.MaxScore, score = submitted.Score,
                    comment = submitted.Comment?.Trim() ?? "" }; }).ToList());
        });
        app.MapGet("/api/attendance/summary", async (HttpContext c, CareerDbContext db, string? from, string? to, int? profileId) =>
        {
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date);
            if (string.IsNullOrWhiteSpace(from)) from = new DateOnly(today.Year, today.Month, 1).ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(to)) to = today.ToString("yyyy-MM-dd");
            if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate) ||
                !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate) ||
                toDate < fromDate || toDate.DayNumber - fromDate.DayNumber > 366)
                return Error("Khoảng thời gian không hợp lệ; chọn tối đa 367 ngày theo định dạng YYYY-MM-DD.");

            var vnOffset = TimeSpan.FromHours(7);
            var rangeStart = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), vnOffset);
            var rangeEnd = new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), vnOffset);
            var attendanceQuery = PortalWorkflow.Visible(db, c.User, "attendance").AsNoTracking()
                .Where(x => x.Start >= rangeStart && x.Start < rangeEnd);
            var shiftQuery = PortalWorkflow.Visible(db, c.User, "shifts").AsNoTracking()
                .Where(x => x.Start < rangeEnd && x.End > rangeStart);
            var leaveQuery = PortalWorkflow.Visible(db, c.User, "leave").AsNoTracking()
                .Where(x => x.Status == "Đã duyệt" && x.Start < rangeEnd && x.End >= rangeStart);
            if (profileId is int requestedProfile)
            {
                attendanceQuery = attendanceQuery.Where(x => x.ProfileId == requestedProfile);
                shiftQuery = shiftQuery.Where(x => x.ProfileId == requestedProfile);
                leaveQuery = leaveQuery.Where(x => x.ProfileId == requestedProfile);
            }

            var attendance = await attendanceQuery.ToListAsync();
            var shifts = await shiftQuery.ToListAsync();
            var leaves = await leaveQuery.ToListAsync();
            var profileIds = attendance.Concat(leaves).Select(x => x.ProfileId).OfType<int>().Distinct().ToList();
            if (profileId is int requestedId && !profileIds.Contains(requestedId) &&
                await PortalWorkflow.Visible(db, c.User, "attendance").AnyAsync(x => x.ProfileId == requestedId))
                profileIds.Add(requestedId);
            var names = await db.InternProfiles.AsNoTracking().Where(x => profileIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var daily = new List<(int ProfileId, DateOnly Day, DateTimeOffset CheckIn, DateTimeOffset? CheckOut)>();
            foreach (var group in attendance.Where(x => x.ProfileId is not null && x.Start is not null)
                         .GroupBy(x => x.ProfileId!.Value))
            foreach (var day in group.GroupBy(x => VietnamDate(x.Start)))
                daily.Add((group.Key, day.Key!.Value, day.Min(x => x.Start!.Value),
                    day.Where(x => x.End is not null).Select(x => x.End).Max()));

            var summaries = profileIds.Order().Select(id =>
            {
                var personDays = daily.Where(x => x.ProfileId == id).ToList();
                var personShifts = shifts.Where(x => x.ProfileId == id && x.Start is not null && x.End is not null).ToList();
                var lateDays = personDays.Count(day => personShifts.Any(shift => VietnamDate(shift.Start) == day.Day &&
                    VietnamDate(shift.End) == day.Day && day.CheckIn > shift.Start));
                var earlyDays = personDays.Count(day => day.CheckOut is not null && personShifts.Any(shift =>
                    VietnamDate(shift.Start) == day.Day && VietnamDate(shift.End) == day.Day && day.CheckOut < shift.End));
                var leaveDates = new HashSet<DateOnly>();
                foreach (var leave in leaves.Where(x => x.ProfileId == id && x.Start is not null && x.End is not null))
                {
                    var first = VietnamDate(leave.Start)!.Value < fromDate ? fromDate : VietnamDate(leave.Start)!.Value;
                    var last = VietnamDate(leave.End)!.Value > toDate ? toDate : VietnamDate(leave.End)!.Value;
                    for (var date = first; date <= last; date = date.AddDays(1)) leaveDates.Add(date);
                }
                return new { profileId = id, name = names.GetValueOrDefault(id, ""), workDays = personDays.Count,
                    lateDays, earlyLeaveDays = earlyDays, leaveDays = leaveDates.Count };
            }).ToList();

            return Results.Ok(new { from = fromDate.ToString("yyyy-MM-dd"), to = toDate.ToString("yyyy-MM-dd"),
                workDays = summaries.Sum(x => x.workDays), lateDays = summaries.Sum(x => x.lateDays),
                earlyLeaveDays = summaries.Sum(x => x.earlyLeaveDays), leaveDays = summaries.Sum(x => x.leaveDays),
                items = summaries });
        });
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
        app.MapGet("/api/admin/mail", async (HttpContext c, CareerDbContext db) => c.User.IsInRole("Admin")
            ? Results.Ok(await db.MailJobs.OrderByDescending(x => x.Id).Take(100).Select(x => new { x.Id, x.Recipient, x.Subject, x.Status, x.Attempts, x.DueAt, x.Error }).ToListAsync())
            : Results.Forbid());
        app.MapPost("/api/admin/mail/{id:int}/retry", async (int id, HttpContext c, CareerDbContext db) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid(); var row = await db.MailJobs.FindAsync(id); if (row is null) return Results.NotFound();
            if (row.Status == "Sent") return Error("Email đã gửi."); row.Status = "Queued"; row.Attempts = 0; row.DueAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); return Results.Ok();
        });
        app.MapPortalReports();
        PortalMailWorker.Start(app);
    }
    static DateOnly? VietnamDate(DateTimeOffset? value) => value is null ? null : DateOnly.FromDateTime(value.Value.ToOffset(TimeSpan.FromHours(7)).Date);

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
            foreach (var a in assignments) { a.Start = row.Start; a.End = row.End; var p = await db.InternProfiles.FindAsync(a.ProfileId); if (p is not null) { p.StartDate = VietnamDate(row.Start); p.EndDate = VietnamDate(row.End); } }
        }
        if (row.ProfileId is int pid && kind == "assignments") { var p = await db.InternProfiles.FindAsync(pid); if (p is not null) { p.StartDate = VietnamDate(row.Start); p.EndDate = VietnamDate(row.End); } }
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

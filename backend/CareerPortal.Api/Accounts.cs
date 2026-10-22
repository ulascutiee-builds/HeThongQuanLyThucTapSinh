using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class AppUser
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresActivation { get; set; }
    public string? ActivationHash { get; set; }
    public DateTimeOffset? ActivationExpiresAt { get; set; }
    public int? ProfileId { get; set; }
    public InternProfile? Profile { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<UserRole> UserRoles { get; set; } = [];
    public bool HasCustomPermissions { get; set; }
    public List<UserPermission> UserPermissions { get; set; } = [];
}
public sealed class AppRole
{
    public required string Name { get; set; }
    public List<RolePermission> RolePermissions { get; set; } = [];
}
public sealed class AppPermission
{
    public required string Key { get; set; }
    public required string Label { get; set; }
}
public sealed class UserRole
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public required string RoleName { get; set; }
    public AppRole? Role { get; set; }
}
public sealed class RolePermission
{
    public required string RoleName { get; set; }
    public AppRole? Role { get; set; }
    public required string PermissionKey { get; set; }
    public AppPermission? Permission { get; set; }
}
public sealed class UserPermission
{
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public required string PermissionKey { get; set; }
    public AppPermission? Permission { get; set; }
}
public sealed record AccountRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, MinLength(1)] public required string[] Roles { get; init; }
    public int? ProfileId { get; init; }
    public bool IsActive { get; init; } = true;
}
public sealed record RolePermissionsRequest
{
    [Required] public required string[] Permissions { get; init; }
}
public sealed record AccountPermissionsRequest
{
    [Required] public required string[] Permissions { get; init; }
    public bool UseRolePermissions { get; init; }
}
public sealed record ActivateAccountRequest
{
    public int AccountId { get; init; }
    [Required, MaxLength(200)] public required string Token { get; init; }
    [Required, MinLength(8), MaxLength(200)] public required string Password { get; init; }
}

public static class Accounts
{
    public static readonly string[] Roles = ["Admin", "HR", "Mentor", "Intern"];
    public static readonly Dictionary<string, string> Permissions = new()
    {
        ["profiles.read"] = "Xem và tìm kiếm hồ sơ",
        ["profiles.write"] = "Thêm và chỉnh sửa hồ sơ",
        ["documents.read"] = "Xem và tải tài liệu",
        ["documents.review"] = "Duyệt và từ chối tài liệu",
        ["applications.review"] = "Xét duyệt hồ sơ, xem email kết quả",
        ["contracts.write"] = "Tải lên và thay thế hợp đồng",
        ["programs.manage"] = "Quản lý chương trình thực tập",
        ["assignments.manage"] = "Phân công thực tập sinh và mentor",
        ["tasks.manage"] = "Giao và quản lý nhiệm vụ thực tập",
        ["reports.review"] = "Phản hồi báo cáo tuần",
        ["evaluations.manage"] = "Đánh giá kỹ năng và thái độ",
        ["attendance.report"] = "Xem báo cáo chấm công và nghỉ phép",
        ["schedule.read"] = "Xem lịch thực tập cá nhân",
        ["attendance.write"] = "Chấm công cá nhân",
        ["accounts.manage"] = "Quản lý tài khoản và kích hoạt",
        ["roles.manage"] = "Phân quyền vai trò và từng tài khoản",
    };

    public static void ConfigureAccounts(this ModelBuilder builder)
    {
        builder.Entity<AppUser>(e =>
        {
            e.ToTable("APP_USER");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.ProfileId).IsUnique().HasFilter("[ProfileId] IS NOT NULL");
            e.Property(x => x.Name).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(160);
            e.Property(x => x.PasswordHash).HasMaxLength(500);
            e.Property(x => x.ActivationHash).HasMaxLength(64);
            e.HasOne(x => x.Profile).WithOne().HasForeignKey<AppUser>(x => x.ProfileId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<AppRole>(e => { e.ToTable("APP_ROLE"); e.HasKey(x => x.Name); e.Property(x => x.Name).HasMaxLength(40); });
        builder.Entity<AppPermission>(e => { e.ToTable("APP_PERMISSION"); e.HasKey(x => x.Key); e.Property(x => x.Key).HasMaxLength(80); e.Property(x => x.Label).HasMaxLength(180); });
        builder.Entity<UserRole>(e =>
        {
            e.ToTable("USER_ROLE"); e.HasKey(x => new { x.UserId, x.RoleName });
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleName).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<RolePermission>(e =>
        {
            e.ToTable("ROLE_PERMISSION"); e.HasKey(x => new { x.RoleName, x.PermissionKey });
            e.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleName).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionKey).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<UserPermission>(e =>
        {
            e.ToTable("USER_PERMISSION"); e.HasKey(x => new { x.UserId, x.PermissionKey });
            e.HasOne(x => x.User).WithMany(x => x.UserPermissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionKey).OnDelete(DeleteBehavior.Restrict);
        });
    }

    static IEnumerable<string> DefaultPermissions(string role) => role switch
    {
        "Admin" => Permissions.Keys,
        "HR" => Permissions.Keys.Where(x => x is not ("accounts.manage" or "roles.manage")),
        "Mentor" => ["schedule.read", "tasks.manage", "reports.review", "evaluations.manage"],
        "Intern" => ["schedule.read", "attendance.write"],
        _ => [],
    };

    public static async Task InitializeAccountsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var profileHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<InternProfile>>();
        var knownPermissions = await db.AppPermissions.Select(x => x.Key).ToListAsync();
        var addedPermissions = Permissions.Keys.Except(knownPermissions).ToArray();
        foreach (var key in addedPermissions) db.AppPermissions.Add(new() { Key = key, Label = Permissions[key] });
        foreach (var role in Roles)
        {
            var entity = await db.AppRoles.Include(x => x.RolePermissions).SingleOrDefaultAsync(x => x.Name == role);
            if (entity is null)
            {
                entity = new() { Name = role };
                foreach (var permission in DefaultPermissions(role)) entity.RolePermissions.Add(new() { RoleName = role, PermissionKey = permission });
                db.AppRoles.Add(entity);
            }
            else
                foreach (var permission in DefaultPermissions(role).Intersect(addedPermissions)) entity.RolePermissions.Add(new() { RoleName = role, PermissionKey = permission });
        }
        await db.SaveChangesAsync();
        // Demo credentials are bootstrap data by default; repair is an explicit opt-in.
        var repairDemoAccounts = app.Configuration.GetValue("Accounts:RepairDemoAccounts", false);
        var bootstrapDemoAccounts = !await db.AppUsers.AnyAsync();
        foreach (var profile in await db.InternProfiles.Where(x => !db.AppUsers.Any(u => u.ProfileId == x.Id || u.Email == x.Email)).ToListAsync())
        {
            // Preserve legacy logins while moving authentication to persistent accounts.
            var account = new AppUser { Name = profile.Name, Email = profile.Email.ToLowerInvariant(), ProfileId = profile.Id, PasswordHash = profile.PasswordHash };
            account.UserRoles.Add(new() { RoleName = "Intern" });
            db.AppUsers.Add(account);
        }
        await db.SaveChangesAsync();
        var mentorEmail = app.Configuration["Accounts:MentorEmail"]?.Trim().ToLowerInvariant();
        var mentorPassword = app.Configuration["Accounts:MentorPassword"];
        if (bootstrapDemoAccounts || repairDemoAccounts)
        {
        foreach (var (role, emailKey, passwordKey, name, legacyEmail) in new[]
        {
            ("HR", "Sprint1:HrEmail", "Sprint1:HrPassword", "Nhân sự", "hr@sprint1.local"),
            ("Admin", "Accounts:AdminEmail", "Accounts:AdminPassword", "Quản trị hệ thống", "admin@sprint12.local")
        })
        {
            var email = app.Configuration[emailKey]?.Trim().ToLowerInvariant();
            var password = app.Configuration[passwordKey];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password)) continue;
            if (password.Length < 8) throw new InvalidOperationException($"{passwordKey} phải có ít nhất 8 ký tự.");
            var target = await db.AppUsers.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Email == email);
            if (target is not null && !target.UserRoles.Any(x => x.RoleName == role))
                throw new InvalidOperationException($"Email khởi tạo {email} đã được gán cho tài khoản không thuộc vai trò {role}.");
            var account = target ?? (bootstrapDemoAccounts
                ? await db.AppUsers.Include(x => x.UserRoles)
                    .Where(x => x.UserRoles.Any(r => r.RoleName == role))
                    .OrderBy(x => x.Email == legacyEmail ? 0 : 1)
                    .ThenBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync()
                : null);
            if (account is null)
            {
                account = new AppUser { Name = name, Email = email, PasswordHash = "" };
                db.AppUsers.Add(account);
            }
            account.Name = name;
            account.Email = email;
            account.PasswordHash = hasher.HashPassword(account, password);
            account.IsActive = true;
            account.RequiresActivation = false;
            account.ActivationHash = null;
            account.ActivationExpiresAt = null;
            if (!account.UserRoles.Any(x => x.RoleName == role)) account.UserRoles.Add(new() { RoleName = role });
            if (role == "Admin" && account.HasCustomPermissions)
                foreach (var key in new[] { "accounts.manage", "roles.manage" }.Except(account.UserPermissions.Select(x => x.PermissionKey)).ToArray())
                    account.UserPermissions.Add(new() { PermissionKey = key });
        }
        await db.SaveChangesAsync();

        // Create the configured demo mentor only when missing. Existing mentor credentials and
        // account-level permissions belong to the user and must not be reset on every startup.
        if (!string.IsNullOrWhiteSpace(mentorEmail) && !string.IsNullOrEmpty(mentorPassword))
        {
            if (mentorPassword.Length < 8) throw new InvalidOperationException("Accounts:MentorPassword phải có ít nhất 8 ký tự.");
            var mentor = await db.AppUsers.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Email == mentorEmail);
            if (mentor is not null && !mentor.UserRoles.Any(x => x.RoleName == "Mentor"))
                throw new InvalidOperationException($"Email khởi tạo {mentorEmail} đang được dùng bởi tài khoản không thuộc vai trò Mentor.");
            if (mentor is null)
            {
                mentor = new AppUser { Name = "Mentor hướng dẫn", Email = mentorEmail, PasswordHash = "" };
                mentor.PasswordHash = hasher.HashPassword(mentor, mentorPassword);
                mentor.UserRoles.Add(new() { RoleName = "Mentor" });
                db.AppUsers.Add(mentor);
                await db.SaveChangesAsync();
            }
            else if (repairDemoAccounts)
            {
                mentor.PasswordHash = hasher.HashPassword(mentor, mentorPassword);
                mentor.IsActive = true;
                mentor.RequiresActivation = false;
                mentor.ActivationHash = null;
                mentor.ActivationExpiresAt = null;
                await db.SaveChangesAsync();
            }
        }

        var internEmail = app.Configuration["Accounts:UserEmail"]?.Trim().ToLowerInvariant();
        var internPassword = app.Configuration["Accounts:UserPassword"];
        if ((bootstrapDemoAccounts || repairDemoAccounts) && !string.IsNullOrWhiteSpace(internEmail) && !string.IsNullOrEmpty(internPassword))
        {
            if (internPassword.Length < 8) throw new InvalidOperationException("Accounts:UserPassword phải có ít nhất 8 ký tự.");
            var account = await db.AppUsers.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Email == internEmail);
            if (account is not null && account.UserRoles.Any(x => x.RoleName != "Intern"))
                throw new InvalidOperationException($"Email khởi tạo {internEmail} đang được dùng bởi vai trò khác.");
            var profile = account?.ProfileId is int profileId
                ? await db.InternProfiles.SingleOrDefaultAsync(x => x.Id == profileId)
                : await db.InternProfiles.SingleOrDefaultAsync(x => x.Email == internEmail);
            if (profile is null)
            {
                profile = new InternProfile
                {
                    Name = "Thực tập sinh",
                    StudentId = "DEMO-USER",
                    Email = internEmail,
                    School = "Chưa cập nhật",
                    Major = "Chưa cập nhật",
                    PasswordHash = "",
                    EmailVerified = true,
                    Status = "Chờ hồ sơ"
                };
                profile.PasswordHash = profileHasher.HashPassword(profile, internPassword);
                db.InternProfiles.Add(profile);
                await db.SaveChangesAsync();
            }
            else
            {
                profile.Email = internEmail;
                profile.EmailVerified = true;
                profile.PasswordHash = profileHasher.HashPassword(profile, internPassword);
            }

            account ??= await db.AppUsers.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.ProfileId == profile.Id);
            if (account is null)
            {
                account = CreateInternAccount(db, profile, "");
            }
            account.Name = profile.Name;
            account.Email = internEmail;
            account.ProfileId = profile.Id;
            account.PasswordHash = hasher.HashPassword(account, internPassword);
            account.IsActive = true;
            account.RequiresActivation = false;
            account.ActivationHash = null;
            account.ActivationExpiresAt = null;
            if (!account.UserRoles.Any(x => x.RoleName == "Intern")) account.UserRoles.Add(new() { RoleName = "Intern" });
            await db.SaveChangesAsync();
        }
        }

        // The demo Mentor account also needs a Mentor row for Sprint 2/3 endpoints.
        // The neutral department is only a placeholder; HR chooses the actual department
        // and assigns interns through the normal workflow.
        if (!string.IsNullOrWhiteSpace(mentorEmail) && !string.IsNullOrEmpty(mentorPassword))
        {
            var mentorAccount = await db.AppUsers.SingleOrDefaultAsync(x => x.Email == mentorEmail);
            if (mentorAccount is not null && !await db.Mentors.AnyAsync(x => x.UserId == mentorAccount.Id))
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var department = await db.Departments.SingleOrDefaultAsync(x => x.Name == "Chưa phân loại");
                department ??= new Department { Name = "Chưa phân loại" };
                db.Mentors.Add(new Mentor { UserId = mentorAccount.Id, Department = department, Capacity = 5 });
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
    }

    public static AppUser CreateInternAccount(CareerDbContext db, InternProfile profile, string passwordHash)
    {
        var account = new AppUser { Name = profile.Name, Email = profile.Email, ProfileId = profile.Id, Profile = profile, PasswordHash = passwordHash };
        account.UserRoles.Add(new() { RoleName = "Intern" });
        db.AppUsers.Add(account);
        return account;
    }

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static void QueueActivation(CareerDbContext db, AppUser account, IConfiguration configuration)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        account.ActivationHash = Hash(token);
        account.ActivationExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        account.RequiresActivation = true;
        var url = $"{(configuration["PublicUrl"] ?? "http://localhost:5135").TrimEnd('/')}/activation.html?accountId={account.Id}&token={Uri.EscapeDataString(token)}";
        db.MailJobs.Add(new()
        {
            EventKey = "activation:" + account.Id + ":" + Guid.NewGuid(), Recipient = account.Email, Subject = "Kích hoạt tài khoản hệ thống thực tập", IsHtml = true,
            Body = $"<html><body style=\"font-family:Arial,sans-serif\"><p>Xin chào {WebUtility.HtmlEncode(account.Name)},</p><p>Hãy đặt mật khẩu của bạn bằng liên kết dưới đây trong vòng 24 giờ. Không chia sẻ liên kết này.</p><p><a href=\"{WebUtility.HtmlEncode(url)}\">Kích hoạt và đặt mật khẩu</a></p></body></html>"
        });
    }

    public static async Task<bool> EmailExists(CareerDbContext db, string email, int? profileId = null, int? accountId = null)
        => await db.AppUsers.AnyAsync(x => x.Email == email && (accountId == null || x.Id != accountId) && (profileId == null || x.ProfileId != profileId));

    public static async Task SyncInternAccount(CareerDbContext db, InternProfile profile)
    {
        var account = await db.AppUsers.SingleOrDefaultAsync(x => x.ProfileId == profile.Id);
        if (account is not null) { account.Name = profile.Name; account.Email = profile.Email; }
    }

    public static string[] RolePermissionsFor(AppUser user) => user.UserRoles
        .SelectMany(x => x.Role?.RolePermissions ?? [])
        .Select(x => x.PermissionKey).Distinct().OrderBy(x => x).ToArray();

    public static string[] EffectivePermissions(AppUser user) => user.HasCustomPermissions
        ? user.UserPermissions.Select(x => x.PermissionKey).Distinct().OrderBy(x => x).ToArray()
        : RolePermissionsFor(user);

    static IQueryable<AppUser> WithPermissions(CareerDbContext db) => db.AppUsers
        .Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x!.RolePermissions)
        .Include(x => x.UserPermissions);

    public static object ToResponse(AppUser user) => new
    {
        user.Id, user.Name, user.Email, user.IsActive, user.RequiresActivation, user.ProfileId, user.CreatedAt,
        Roles = user.UserRoles.Select(x => x.RoleName).OrderBy(x => x).ToArray(),
        user.HasCustomPermissions,
        Permissions = EffectivePermissions(user),
        RolePermissions = RolePermissionsFor(user)
    };

    static async Task<string?> ValidateAccount(CareerDbContext db, AccountRequest input, int? id = null)
    {
        if (input.Roles.Any(x => !Roles.Contains(x)) || input.Roles.Distinct().Count() != input.Roles.Length) return "Chọn vai trò hợp lệ và không trùng.";
        var email = input.Email.Trim().ToLowerInvariant();
        if (await EmailExists(db, email, accountId: id)) return "Email đã được sử dụng bởi tài khoản khác.";
        if (input.Roles.Contains("Intern") && input.ProfileId is null) return "Tài khoản thực tập sinh phải liên kết với hồ sơ.";
        if (input.ProfileId is int profileId)
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return "Hồ sơ thực tập sinh không tồn tại.";
            if (profile.Email != email) return "Email tài khoản phải trùng email của hồ sơ thực tập sinh.";
            if (await db.AppUsers.AnyAsync(x => x.ProfileId == profileId && (id == null || x.Id != id))) return "Hồ sơ đã liên kết với tài khoản khác.";
        }
        else if (await db.InternProfiles.AnyAsync(x => x.Email == email)) return "Email đã thuộc một hồ sơ thực tập sinh. Hãy liên kết hồ sơ đó.";
        return null;
    }

    static async Task<bool> IsLastAdmin(CareerDbContext db, AppUser user, bool active, string[] roles)
        => user.IsActive && user.UserRoles.Any(x => x.RoleName == "Admin") && (!active || !roles.Contains("Admin"))
           && !await db.AppUsers.AnyAsync(x => x.Id != user.Id && x.IsActive && x.UserRoles.Any(r => r.RoleName == "Admin"));

    public static void MapAccounts(this WebApplication app)
    {
        app.MapGet("/api/accounts", async (CareerDbContext db) =>
        {
            var users = await WithPermissions(db).AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
            var roles = await db.AppRoles.Include(x => x.RolePermissions).AsNoTracking().OrderBy(x => x.Name).ToListAsync();
            return Results.Ok(new { items = users.Select(ToResponse), roles = roles.Select(x => new { x.Name, Permissions = x.RolePermissions.Select(p => p.PermissionKey).OrderBy(p => p).ToArray() }), permissions = Permissions.Select(x => new { key = x.Key, label = x.Value }) });
        });
        app.MapGet("/api/accounts/profiles", async (CareerDbContext db) => Results.Ok(await db.InternProfiles.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Email, x.StudentId }).ToListAsync()));
        app.MapGet("/api/accounts/{id:int}", async (int id, CareerDbContext db) =>
        {
            var user = await WithPermissions(db).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            return user is null ? Results.NotFound() : Results.Ok(ToResponse(user));
        });
        app.MapPost("/api/accounts", async (AccountRequest input, HttpContext context, CareerDbContext db, IConfiguration config) =>
        {
            if (!SprintSecurity.HasPermission(context, "roles.manage")) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            if (await ValidateAccount(db, input) is string error) return Results.BadRequest(new { message = error });
            var user = new AppUser { Name = input.Name.Trim(), Email = input.Email.Trim().ToLowerInvariant(), PasswordHash = "", ProfileId = input.ProfileId, IsActive = input.IsActive };
            foreach (var role in input.Roles) user.UserRoles.Add(new() { RoleName = role });
            db.AppUsers.Add(user); await db.SaveChangesAsync();
            QueueActivation(db, user, config); await db.SaveChangesAsync(); await tx.CommitAsync();
            var created = await WithPermissions(db).AsNoTracking().SingleAsync(x => x.Id == user.Id);
            return Results.Created($"/api/accounts/{user.Id}", new { account = ToResponse(created), message = "Tài khoản đã tạo. Liên kết đặt mật khẩu được gửi qua email (hiệu lực 24 giờ)." });
        });
        app.MapPut("/api/accounts/{id:int}", async (int id, AccountRequest input, HttpContext context, CareerDbContext db, IConfiguration config) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var user = await WithPermissions(db).SingleOrDefaultAsync(x => x.Id == id); if (user is null) return Results.NotFound();
            if (!SprintSecurity.HasPermission(context, "roles.manage") && !input.Roles.ToHashSet().SetEquals(user.UserRoles.Select(x => x.RoleName))) return Results.Forbid();
            var requestedEmail = input.Email.Trim().ToLowerInvariant();
            var changingEmail = requestedEmail != user.Email;
            if (changingEmail && input.ProfileId != user.ProfileId) return Results.BadRequest(new { message = "Hãy đổi liên kết hồ sơ và email trong hai lần lưu riêng." });
            // Validate the current linked profile while the new email is pending confirmation.
            var validationInput = changingEmail ? input with { Email = user.Email } : input;
            if (await ValidateAccount(db, validationInput, id) is string error) return Results.BadRequest(new { message = error });
            if (await IsLastAdmin(db, user, input.IsActive, input.Roles)) return Results.Conflict(new { message = "Phải giữ ít nhất một tài khoản Admin đang hoạt động." });
            if (changingEmail && await EmailChange.QueueAsync(db, user, requestedEmail, config) is string emailError)
                return Results.Conflict(new { message = emailError });
            user.Name = input.Name.Trim(); user.ProfileId = input.ProfileId; user.IsActive = input.IsActive;
            db.UserRoles.RemoveRange(user.UserRoles.Where(x => !input.Roles.Contains(x.RoleName)));
            foreach (var role in input.Roles.Except(user.UserRoles.Select(x => x.RoleName)).ToArray()) user.UserRoles.Add(new() { RoleName = role });
            if (user.HasCustomPermissions && input.Roles.Contains("Admin"))
                foreach (var key in new[] { "accounts.manage", "roles.manage" }.Except(user.UserPermissions.Select(x => x.PermissionKey)).ToArray())
                    user.UserPermissions.Add(new() { PermissionKey = key });
            await db.SaveChangesAsync(); await tx.CommitAsync();
            var updated = await WithPermissions(db).AsNoTracking().SingleAsync(x => x.Id == id);
            return Results.Ok(ToResponse(updated));
        });
        app.MapPut("/api/accounts/{id:int}/permissions", async (int id, AccountPermissionsRequest input, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HasPermission(context, "roles.manage")) return Results.Forbid();
            if (input.Permissions.Any(x => !Permissions.ContainsKey(x)) || input.Permissions.Distinct().Count() != input.Permissions.Length)
                return Results.BadRequest(new { message = "Danh sách quyền không hợp lệ." });
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var user = await WithPermissions(db).SingleOrDefaultAsync(x => x.Id == id);
            if (user is null) return Results.NotFound();
            if (!input.UseRolePermissions && user.UserRoles.Any(x => x.RoleName == "Admin")
                && (!input.Permissions.Contains("accounts.manage") || !input.Permissions.Contains("roles.manage")))
                return Results.BadRequest(new { message = "Admin cần giữ quyền quản lý tài khoản và phân quyền để tránh khóa hệ thống." });
            var selected = input.UseRolePermissions ? Array.Empty<string>() : input.Permissions;
            db.UserPermissions.RemoveRange(user.UserPermissions.Where(x => !selected.Contains(x.PermissionKey)));
            foreach (var key in selected.Except(user.UserPermissions.Select(x => x.PermissionKey)).ToArray())
                user.UserPermissions.Add(new() { PermissionKey = key });
            user.HasCustomPermissions = !input.UseRolePermissions;
            await db.SaveChangesAsync(); await tx.CommitAsync();
            var updated = await WithPermissions(db).AsNoTracking().SingleAsync(x => x.Id == id);
            return Results.Ok(ToResponse(updated));
        });
        app.MapDelete("/api/accounts/{id:int}", async (int id, HttpContext context, CareerDbContext db) =>
        {
            if (!context.User.IsInRole("Admin") || !SprintSecurity.HasPermission(context, "accounts.manage")) return Results.Forbid();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var user = await db.AppUsers.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.Id == id);
            if (user is null) return Results.NotFound();
            if (await IsLastAdmin(db, user, false, []))
                return Results.Conflict(new { message = "Không thể xóa Admin đang hoạt động cuối cùng." });

            var mentorIds = await db.Mentors.Where(x => x.UserId == id).Select(x => x.Id).ToArrayAsync();
            var profileId = user.ProfileId;
            var profileEmail = profileId is int pid
                ? await db.InternProfiles.Where(x => x.Id == pid).Select(x => x.Email).SingleOrDefaultAsync()
                : null;
            var recipientEmails = new[] { user.Email, profileEmail }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();

            // Notification and mail records are not FK-linked to a user. Remove direct
            // account/profile messages and queued or sent mail metadata for this identity.
            await db.PortalNotifications.Where(x => x.AccountId == id || (profileId != null && x.ProfileId == profileId)).ExecuteDeleteAsync();
            await db.MailJobs.Where(x => recipientEmails.Contains(x.Recipient)
                || x.EventKey.StartsWith("activation:" + id + ":")
                || x.EventKey.StartsWith("email-change:" + id + ":")
                || (profileId != null && (x.EventKey.StartsWith("report-submitted:" + profileId + ":")
                    || x.EventKey.StartsWith("evaluation:" + profileId + ":")))).ExecuteDeleteAsync();
            await db.TaskProgressHistories.Where(x => x.ChangedByUserId == id).ExecuteDeleteAsync();

            if (mentorIds.Length > 0)
            {
                await db.InternshipTasks.Where(x => mentorIds.Contains(x.MentorId)).ExecuteDeleteAsync();
                await db.InternshipEvaluations.Where(x => mentorIds.Contains(x.MentorId)).ExecuteDeleteAsync();
                await db.InternAssignments.Where(x => mentorIds.Contains(x.MentorId)).ExecuteDeleteAsync();
                await db.Mentors.Where(x => x.UserId == id).ExecuteDeleteAsync();
            }

            if (profileId is int linkedProfileId)
            {
                await db.InternshipTasks.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
                await db.InternshipEvaluations.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
                await db.WeeklyReports.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
                await db.AttendanceRecords.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
                await db.LeaveRequests.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
                await db.InternAssignments.Where(x => x.ProfileId == linkedProfileId).ExecuteDeleteAsync();
            }

            db.AppUsers.Remove(user);
            await db.SaveChangesAsync();
            if (profileId is int ownedProfileId)
                await db.InternProfiles.Where(x => x.Id == ownedProfileId).ExecuteDeleteAsync();
            await tx.CommitAsync();
            return Results.NoContent();
        });
        app.MapPost("/api/accounts/{id:int}/activation", async (int id, CareerDbContext db, IConfiguration config) =>
        {
            var user = await db.AppUsers.FindAsync(id); if (user is null) return Results.NotFound();
            if (!user.IsActive) return Results.BadRequest(new { message = "Kích hoạt trạng thái tài khoản trước khi gửi liên kết." });
            if (user.ActivationExpiresAt > DateTimeOffset.UtcNow.AddHours(24).AddMinutes(-1)) return Results.BadRequest(new { message = "Vui lòng đợi một phút trước khi gửi lại." });
            QueueActivation(db, user, config); await db.SaveChangesAsync(); return Results.Ok(new { message = "Đã gửi liên kết đặt mật khẩu mới, hiệu lực 24 giờ." });
        });
        app.MapPut("/api/roles/{role}/permissions", async (string role, RolePermissionsRequest input, CareerDbContext db) =>
        {
            var entity = await db.AppRoles.Include(x => x.RolePermissions).SingleOrDefaultAsync(x => x.Name == role); if (entity is null) return Results.NotFound();
            if (input.Permissions.Any(x => !Permissions.ContainsKey(x)) || input.Permissions.Distinct().Count() != input.Permissions.Length) return Results.BadRequest(new { message = "Danh sách quyền không hợp lệ." });
            if (role == "Admin" && (!input.Permissions.Contains("accounts.manage") || !input.Permissions.Contains("roles.manage"))) return Results.BadRequest(new { message = "Admin cần giữ quyền quản lý tài khoản và phân quyền để tránh khóa hệ thống." });
            db.RolePermissions.RemoveRange(entity.RolePermissions.Where(x => !input.Permissions.Contains(x.PermissionKey)));
            foreach (var permission in input.Permissions.Except(entity.RolePermissions.Select(x => x.PermissionKey)).ToArray()) entity.RolePermissions.Add(new() { RoleName = role, PermissionKey = permission });
            await db.SaveChangesAsync(); return Results.Ok(new { role, permissions = input.Permissions });
        });
        app.MapPost("/api/auth/activate", async (ActivateAccountRequest input, CareerDbContext db, IPasswordHasher<AppUser> hasher, IPasswordHasher<InternProfile> profileHasher) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var user = await db.AppUsers.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Id == input.AccountId);
            if (user is null || !user.IsActive || !user.RequiresActivation || user.ActivationHash != Hash(input.Token) || user.ActivationExpiresAt is not DateTimeOffset expiry || expiry <= DateTimeOffset.UtcNow) return Results.BadRequest(new { message = "Liên kết kích hoạt không hợp lệ, đã sử dụng hoặc hết hạn." });
            user.PasswordHash = hasher.HashPassword(user, input.Password); user.RequiresActivation = false; user.ActivationHash = null; user.ActivationExpiresAt = null;
            if (user.Profile is not null) { user.Profile.PasswordHash = profileHasher.HashPassword(user.Profile, input.Password); user.Profile.EmailVerified = true; }
            await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { message = "Đã đặt mật khẩu. Bạn có thể đăng nhập." });
        });
    }
}

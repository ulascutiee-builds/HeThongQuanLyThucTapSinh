using Microsoft.EntityFrameworkCore;

public static class Profiles
{
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var digits = phone.Trim();
        if (digits.StartsWith('+')) digits = digits[1..];
        return digits.StartsWith("84", StringComparison.Ordinal) && digits.Length == 11 ? "0" + digits[2..] : digits;
    }
    public static Task<bool> PhoneExistsAsync(CareerDbContext db, string? phone, int? exceptId = null)
    {
        if (phone is null) return Task.FromResult(false);
        var candidates = new List<string> { phone, "+" + phone };
        if (phone.Length == 10 && phone[0] == '0') { candidates.Add("84" + phone[1..]); candidates.Add("+84" + phone[1..]); }
        IQueryable<InternProfile> profiles = db.InternProfiles;
        if (exceptId is int id) profiles = profiles.Where(x => x.Id != id);
        return profiles.AnyAsync(x => x.Phone != null && candidates.Contains(x.Phone));
    }
    public static readonly string[] Statuses = ["Chờ hồ sơ", "Chờ duyệt", "Đang thực tập", "Chờ bắt đầu", "Đã hoàn thành", "Từ chối"];
    static string? ValidateDates(string status, DateOnly? start, DateOnly? end, DateOnly? birth)
    {
        if (!Statuses.Contains(status)) return "Trạng thái hồ sơ không hợp lệ.";
        if (birth is DateOnly dob && dob > InternshipStatusSync.Today) return "Ngày sinh không hợp lệ.";
        if (start is not null && end is not null && end < start) return "Ngày bắt đầu không được lớn hơn ngày kết thúc.";
        if (status is "Đã hoàn thành" or "Đang thực tập" or "Chờ bắt đầu" && (start is null || end is null)) return "Vui lòng nhập ngày bắt đầu và ngày kết thúc hợp lệ.";
        return null;
    }
    public static void MapProfiles(this WebApplication app)
    {
        app.MapPost("/api/interns", Create); app.MapPost("/api/hr/profiles", Create);
        app.MapPut("/api/hr/profiles/{id:int}", UpdateHR);
        app.MapPut("/api/interns/{id:int}", Update);
        app.MapGet("/api/interns/{id:int}", async (int id, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.Own(context, id)) return Results.Forbid();
            await InternshipStatusSync.UpdateAsync(db);
            var profile = await db.InternProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            return profile is null ? Results.NotFound() : Results.Ok(profile.ToResponse());
        });
        app.MapGet("/api/interns", async (HttpContext context, CareerDbContext db, string? search, string? school, string? major, string? status, int page = 1, int pageSize = 5) =>
        {
            if (!SprintSecurity.HasPermission(context, "profiles.read")) return Results.Forbid();
            if (page < 1 || page > 10000000 || pageSize < 1 || pageSize > 100) return Results.BadRequest(new { message = "Phân trang không hợp lệ." });
            if (!string.IsNullOrWhiteSpace(status) && !Statuses.Contains(status)) return Results.BadRequest(new { message = "Trạng thái lọc không hợp lệ." });
            await InternshipStatusSync.UpdateAsync(db);
            var query = db.InternProfiles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search)) { var word = search.Trim(); query = query.Where(x => x.Name.Contains(word) || x.StudentId.Contains(word) || x.Email.Contains(word) || x.School.Contains(word) || x.Major.Contains(word)); }
            if (!string.IsNullOrWhiteSpace(school)) query = query.Where(x => x.School == school.Trim());
            if (!string.IsNullOrWhiteSpace(major)) query = query.Where(x => x.Major == major.Trim());
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            var total = await query.CountAsync();
            var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new { items = rows.Select(x => x.ToResponse()), total, page, pageSize });
        });
        app.MapGet("/api/interns/{id:int}/workspace", async (int id, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.Own(context, id)) return Results.Forbid();
            await InternshipStatusSync.UpdateAsync(db);
            var profile = await db.InternProfiles.Include(x => x.Application).Include(x => x.Documents).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (profile is null) return Results.NotFound();
            var history = await db.InternReviewHistories.Where(x => x.ProfileId == id).AsNoTracking().OrderByDescending(x => x.ReviewedAt).ToListAsync();
            var canReadDocuments = context.User.IsInRole("Intern") && SprintSecurity.Id(context) == id || SprintSecurity.HasPermission(context, "documents.read");
            return Results.Ok(new InternWorkspaceResponse(profile.ToResponse(), profile.Application?.ToResponse(), canReadDocuments ? profile.Documents.Where(x => x.IsCurrent).OrderByDescending(x => x.UploadedAt).Select(x => x.ToResponse()).ToArray() : [], history.Select(x => x.ToResponse()).ToArray()));
        });
        app.MapGet("/api/hr/dashboard", async (HttpContext context, CareerDbContext db) =>
        {
            await InternshipStatusSync.UpdateAsync(db);
            var profiles = await db.InternProfiles.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
            var applications = SprintSecurity.HasPermission(context, "applications.review") ? await db.InternApplications.AsNoTracking().ToListAsync() : [];
            var documents = SprintSecurity.HasPermission(context, "documents.read") ? await db.InternDocuments.AsNoTracking().OrderByDescending(x => x.UploadedAt).ToListAsync() : [];
            var history = SprintSecurity.HasPermission(context, "documents.review") || SprintSecurity.HasPermission(context, "applications.review") ? await db.InternReviewHistories.AsNoTracking().OrderByDescending(x => x.ReviewedAt).ToListAsync() : [];
            return Results.Ok(new HrDashboardResponse(profiles.Select(x => x.ToResponse()).ToArray(), applications.Select(x => x.ToResponse()).ToArray(), documents.Select(x => x.ToResponse()).ToArray(), history.Select(x => x.ToResponse()).ToArray()));
        });
    }
    static async Task<IResult> Create(CreateHrProfileRequest input, HttpContext context, CareerDbContext db, IConfiguration config)
    {
        if (!SprintSecurity.HasPermission(context, "profiles.write")) return Results.Forbid();
        if (ValidateDates(input.Status, input.StartDate, input.EndDate, input.DateOfBirth) is string error) return Results.BadRequest(new { message = error });
        if (input.DateOfBirth is null) return Results.BadRequest(new { message = "Vui lòng nhập ngày tháng năm sinh." });
        var email = input.Email.Trim().ToLowerInvariant(); var phone = NormalizePhone(input.Phone);
        if (await db.InternProfiles.AnyAsync(x => x.Email == email) || await Accounts.EmailExists(db, email) || await PhoneExistsAsync(db, phone)) return Results.Conflict(new { message = "Email hoặc số điện thoại đã tồn tại." });
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var profile = new InternProfile { Name = input.Name.Trim(), Email = email, StudentId = input.StudentId.Trim(), Phone = phone, DateOfBirth = input.DateOfBirth, School = input.School.Trim(), Major = input.Major.Trim(), StartDate = input.StartDate, EndDate = input.EndDate, Status = input.Status, PasswordHash = "" };
        db.InternProfiles.Add(profile); await db.SaveChangesAsync();
        var account = Accounts.CreateInternAccount(db, profile, ""); await db.SaveChangesAsync();
        Accounts.QueueActivation(db, account, config); await db.SaveChangesAsync(); await tx.CommitAsync();
        return Results.Created($"/api/interns/{profile.Id}/workspace", profile.ToResponse());
    }
    static async Task<IResult> Update(int id, UpdateInternProfileRequest input, HttpContext context, CareerDbContext db, IConfiguration config)
    {
        if (!SprintSecurity.Own(context, id)) return Results.Forbid();
        if (SprintSecurity.HasPermission(context, "profiles.write"))
        {
            var existing = await db.InternProfiles.FindAsync(id); if (existing is null) return Results.NotFound();
            return await UpdateHR(id, new UpdateHrProfileRequest { Name = input.Name, StudentId = input.StudentId, Email = input.Email, Phone = input.Phone, DateOfBirth = input.DateOfBirth, School = input.School, Major = input.Major, StartDate = input.StartDate ?? existing.StartDate, EndDate = input.EndDate ?? existing.EndDate, Status = input.Status ?? existing.Status }, context, db, config);
        }
        if (!context.User.IsInRole("Intern") || SprintSecurity.Id(context) != id) return Results.Forbid();
        if (input.Status is not null || input.StartDate is not null || input.EndDate is not null) return Results.Forbid();
        if (input.DateOfBirth is DateOnly dob && dob > InternshipStatusSync.Today) return Results.ValidationProblem(new Dictionary<string, string[]> { ["DateOfBirth"] = ["Ngày sinh không hợp lệ."] });
        var profile = await db.InternProfiles.FindAsync(id); if (profile is null) return Results.NotFound();
        var email = input.Email.Trim().ToLowerInvariant(); var phone = NormalizePhone(input.Phone);
        if (await db.InternProfiles.AnyAsync(x => x.Id != id && x.Email == email) || await Accounts.EmailExists(db, email, profileId: id) || await PhoneExistsAsync(db, phone, id)) return Results.Conflict(new { message = "Email hoặc số điện thoại đã tồn tại." });
        ChangeEmail(db, profile, email, config);
        profile.Name = input.Name.Trim(); profile.StudentId = input.StudentId.Trim(); profile.Phone = phone; profile.DateOfBirth = input.DateOfBirth; profile.School = input.School.Trim(); profile.Major = input.Major.Trim();
        await SyncAccount(db, profile, config); await db.SaveChangesAsync(); return Results.Ok(profile.ToResponse());
    }
    static void ChangeEmail(CareerDbContext db, InternProfile profile, string email, IConfiguration config)
    {
        if (profile.Email == email) return;
        profile.Email = email; profile.EmailVerified = !AuthEndpoints.RequireEmailVerification(config);
        if (AuthEndpoints.RequireEmailVerification(config)) AuthEndpoints.Verification(db, profile, config);
    }
    static async Task SyncAccount(CareerDbContext db, InternProfile profile, IConfiguration config)
    {
        var account = await db.AppUsers.SingleOrDefaultAsync(x => x.ProfileId == profile.Id);
        if (account is null) return;
        var changedEmail = account.Email != profile.Email;
        account.Name = profile.Name; account.Email = profile.Email;
        if (changedEmail && account.RequiresActivation) Accounts.QueueActivation(db, account, config);
    }
    static async Task<IResult> UpdateHR(int id, UpdateHrProfileRequest input, HttpContext context, CareerDbContext db, IConfiguration config)
    {
        if (!SprintSecurity.HasPermission(context, "profiles.write")) return Results.Forbid();
        if (ValidateDates(input.Status, input.StartDate, input.EndDate, input.DateOfBirth) is string error) return Results.BadRequest(new { message = error });
        var profile = await db.InternProfiles.FindAsync(id); if (profile is null) return Results.NotFound();
        var email = input.Email.Trim().ToLowerInvariant(); var phone = NormalizePhone(input.Phone);
        if (await db.InternProfiles.AnyAsync(x => x.Id != id && x.Email == email) || await Accounts.EmailExists(db, email, profileId: id) || await PhoneExistsAsync(db, phone, id)) return Results.Conflict(new { message = "Email hoặc số điện thoại đã tồn tại." });
        if (input.StudentId is not null && string.IsNullOrWhiteSpace(input.StudentId)) return Results.BadRequest(new { message = "Mã sinh viên không được để trống." });
        if (profile.Status != input.Status && input.Status is "Từ chối" or "Đang thực tập" && await db.InternApplications.AnyAsync(x => x.ProfileId == id)) return Results.BadRequest(new { message = "Duyệt/từ chối qua màn hình Đơn xin để ghi lịch sử và gửi email." });
        var assignment = await db.InternAssignments.Include(x => x.Program).SingleOrDefaultAsync(x => x.ProfileId == id);
        if (assignment?.Program is not null && (input.StartDate != assignment.Program.StartDate || input.EndDate != assignment.Program.EndDate)) return Results.BadRequest(new { message = "Hồ sơ đã phân công vào chương trình. Vui lòng sửa thời gian tại chương trình thực tập." });
        ChangeEmail(db, profile, email, config);
        profile.Name = input.Name.Trim(); if (input.StudentId is not null) profile.StudentId = input.StudentId.Trim();
        profile.DateOfBirth = input.DateOfBirth ?? profile.DateOfBirth; profile.Phone = phone; profile.School = input.School.Trim(); profile.Major = input.Major.Trim(); profile.StartDate = input.StartDate; profile.EndDate = input.EndDate; profile.Status = input.Status;
        if (assignment is null)
        {
            var contract = await db.InternDocuments.SingleOrDefaultAsync(x => x.ProfileId == id && x.Type == "Hợp đồng thực tập" && x.IsCurrent);
            if (contract is not null) { contract.StartsAt = input.StartDate is DateOnly start ? InternshipStatusSync.StartOfDay(start) : null; contract.ExpiresAt = input.EndDate is DateOnly end ? InternshipStatusSync.EndOfDay(end) : null; }
        }
        await SyncAccount(db, profile, config); await db.SaveChangesAsync(); return Results.Ok(profile.ToResponse());
    }
}

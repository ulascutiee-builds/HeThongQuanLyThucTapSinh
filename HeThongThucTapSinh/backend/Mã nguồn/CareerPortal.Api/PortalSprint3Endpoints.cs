using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public static partial class PortalEndpoints
{
    static readonly HashSet<string> SupportedCurrencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(culture =>
        {
            try { return new RegionInfo(culture.Name).ISOCurrencySymbol; }
            catch (ArgumentException) { return ""; }
        })
        .Where(code => !string.IsNullOrWhiteSpace(code))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static void MapSprint3Endpoints(this WebApplication app)
    {
        app.MapPut("/api/leave-requests/{id:int}/status", async (int id, LeaveRequestStatusInput input, HttpContext c, CareerDbContext db) =>
        {
            var feedback = input.Feedback?.Trim() ?? "";
            var action = input.Status?.Trim() switch
            {
                "Đã duyệt" or "approved" or "approve" => "approve",
                "Từ chối" or "rejected" or "reject" => "reject",
                _ => ""
            };
            if (action.Length == 0) return Error("Trạng thái chỉ có thể là Đã duyệt hoặc Từ chối.");
            if (feedback.Length > 2000) return Error("Phản hồi tối đa 2.000 ký tự.");
            return await Action("leave", id, new ActionInput(action, Feedback: feedback), c, db);
        });

        app.MapGet("/api/allowances", async (HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.Identity?.IsAuthenticated ?? true) return Results.Unauthorized();
            if (!CanReadAllowances(c.User)) return Results.Forbid();
            var rows = await VisibleAllowances(db, c.User).AsNoTracking()
                .Include(x => x.Profile).OrderByDescending(x => x.PeriodStart).ThenBy(x => x.Profile.Name).ToListAsync();
            return Results.Ok(rows.Select(ToAllowanceResponse));
        });

        app.MapPost("/api/allowances", async (AllowanceInput input, HttpContext c, CareerDbContext db) =>
            await SaveAllowance(null, input, c, db));
        app.MapPut("/api/allowances/{id:int}", async (int id, AllowanceInput input, HttpContext c, CareerDbContext db) =>
            await SaveAllowance(id, input, c, db));

        app.MapPut("/api/allowances/{id:int}/payment-status", async (int id, AllowancePaymentStatusInput input, HttpContext c, CareerDbContext db) =>
        {
            if (!await PortalWorkflow.CanWrite(db, c.User, "allowances")) return Results.Forbid();
            var row = await db.Allowances.FirstOrDefaultAsync(x => x.Id == id);
            if (row is null) return Results.NotFound();
            if (input.Version is null || !row.Version.SequenceEqual(input.Version))
                return Results.Conflict(new { message = "Dữ liệu đã đổi; tải lại trước khi cập nhật." });
            var status = input.Status?.Trim() ?? "";
            if (status is not ("Chưa thanh toán" or "Đã thanh toán"))
                return Error("Trạng thái thanh toán chỉ có thể là Chưa thanh toán hoặc Đã thanh toán.");
            var feedback = input.Feedback?.Trim() ?? "";
            if (feedback.Length > 2000) return Error("Ghi chú tối đa 2.000 ký tự.");
            if (row.PaymentStatus == status) return Results.Ok(ToAllowanceResponse(row));

            var previous = Snapshot(row);
            var oldStatus = row.PaymentStatus;
            var actor = Actor(c.User);
            row.PaymentStatus = status;
            row.UpdatedBy = actor;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            db.AllowanceHistories.Add(new AllowanceHistory
            {
                Allowance = row,
                ChangedBy = actor,
                Action = "payment-status",
                Comment = feedback,
                PreviousValuesJson = previous,
                NewValuesJson = JsonSerializer.Serialize(new { paymentStatus = status, feedback })
            });
            await db.SaveChangesAsync();
            return Results.Ok(new { id = row.Id, previousStatus = oldStatus, allowance = ToAllowanceResponse(row) });
        });

        app.MapGet("/api/allowances/{id:int}/history", async (int id, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.Identity?.IsAuthenticated ?? true) return Results.Unauthorized();
            if (!CanReadAllowances(c.User)) return Results.Forbid();
            var allowance = await VisibleAllowances(db, c.User).AsNoTracking().AnyAsync(x => x.Id == id);
            if (!allowance) return Results.NotFound();
            var history = await db.AllowanceHistories.AsNoTracking().Where(x => x.AllowanceId == id)
                .OrderByDescending(x => x.ChangedAt).ThenByDescending(x => x.Id)
                .Select(x => new { x.Id, x.Action, x.ChangedBy, x.ChangedAt, x.Comment,
                    previousValues = x.PreviousValuesJson, newValues = x.NewValuesJson }).ToListAsync();
            return Results.Ok(history);
        });

        app.MapGet("/api/allowances/summary", async (HttpContext c, CareerDbContext db, string? from, string? to, int? profileId) =>
        {
            if (!c.User.Identity?.IsAuthenticated ?? true) return Results.Unauthorized();
            if (!CanReadAllowances(c.User)) return Results.Forbid();
            var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date);
            from ??= new DateOnly(today.Year, today.Month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            to ??= today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate) ||
                !DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate) ||
                toDate < fromDate || toDate.DayNumber - fromDate.DayNumber > 366)
                return Error("Khoảng thời gian không hợp lệ; chọn tối đa 367 ngày theo định dạng YYYY-MM-DD.");

            var query = VisibleAllowances(db, c.User).AsNoTracking()
                .Where(x => x.PeriodEnd >= fromDate && x.PeriodStart <= toDate);
            if (profileId is int selectedProfile) query = query.Where(x => x.ProfileId == selectedProfile);
            var records = await query.Select(x => new
            {
                x.PeriodStart, x.PeriodEnd, x.Currency, x.PaymentStatus, x.Amount
            }).ToListAsync();
            var items = records.GroupBy(x => new { x.PeriodStart, x.PeriodEnd, x.Currency, x.PaymentStatus })
                .OrderBy(group => group.Key.PeriodStart).ThenBy(group => group.Key.Currency).ThenBy(group => group.Key.PaymentStatus)
                .Select(group => new
                {
                    periodStart = group.Key.PeriodStart,
                    periodEnd = group.Key.PeriodEnd,
                    currency = group.Key.Currency,
                    paymentStatus = group.Key.PaymentStatus,
                    amount = group.Sum(x => x.Amount),
                    count = group.Count()
                }).ToList();
            var totalsByCurrency = records.GroupBy(x => x.Currency).OrderBy(group => group.Key)
                .Select(group => new { currency = group.Key, amount = group.Sum(x => x.Amount), count = group.Count() }).ToList();
            return Results.Ok(new { from = fromDate, to = toDate, items, totalsByCurrency });
        });

        app.MapGet("/api/mentors/statistics/mentees", async (HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("HR") && !c.User.IsInRole("Admin") && !c.User.IsInRole("Mentor")) return Results.Forbid();
            var mentors = await PortalWorkflow.Visible(db, c.User, "mentors").AsNoTracking().ToListAsync();
            var mentorIds = mentors.Select(x => x.Id).ToList();
            var counts = await db.WorkItems.AsNoTracking().Where(x => x.Kind == "assignments" && x.MentorId != null && mentorIds.Contains(x.MentorId.Value))
                .GroupBy(x => x.MentorId!.Value).Select(group => new { mentorId = group.Key, count = group.Count() }).ToListAsync();
            return Results.Ok(mentors.OrderBy(x => x.Title).Select(mentor =>
            {
                var assigned = counts.FirstOrDefault(x => x.mentorId == mentor.Id)?.count ?? 0;
                return new { mentorId = mentor.Id, mentor = mentor.Title, assignedMentees = assigned,
                    capacity = mentor.Amount, availableSlots = Math.Max(0, (int)mentor.Amount - assigned) };
            }));
        });

        app.MapGet("/api/statistics/schools-majors", async (HttpContext c, CareerDbContext db, string? school, string? major) =>
        {
            if (!PortalSecurity.Manager(c.User)) return Results.Forbid();
            var query = db.InternProfiles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(school)) query = query.Where(x => x.School.Contains(school.Trim()));
            if (!string.IsNullOrWhiteSpace(major)) query = query.Where(x => x.Major.Contains(major.Trim()));
            var items = await query
                .GroupBy(x => new { x.School, x.Major })
                .Select(group => new { school = group.Key.School, major = group.Key.Major, count = group.Count() })
                .OrderBy(x => x.school).ThenBy(x => x.major).ToListAsync();
            return Results.Ok(new { total = items.Sum(x => x.count), items });
        });
    }

    static IQueryable<Allowance> VisibleAllowances(CareerDbContext db, ClaimsPrincipal user)
    {
        var query = db.Allowances.AsQueryable();
        if (PortalSecurity.Manager(user)) return query;
        if (user.IsInRole("Intern")) return query.Where(x => x.ProfileId == PortalSecurity.UserId(user));
        return query.Where(_ => false);
    }

    static bool CanReadAllowances(ClaimsPrincipal user) => PortalSecurity.Manager(user) || user.IsInRole("Intern");

    static string Actor(ClaimsPrincipal user) => user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    static object ToAllowanceResponse(Allowance row) => new
    {
        row.Id,
        row.Title,
        row.ProfileId,
        profileName = row.Profile?.Name,
        start = row.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        end = row.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        periodStart = row.PeriodStart,
        periodEnd = row.PeriodEnd,
        row.Amount,
        row.Currency,
        status = row.PaymentStatus,
        paymentStatus = row.PaymentStatus,
        detail = row.Note,
        row.CreatedBy,
        row.CreatedAt,
        row.UpdatedBy,
        row.UpdatedAt,
        row.Version
    };

    static string Snapshot(Allowance row) => JsonSerializer.Serialize(new
    {
        row.Title, row.ProfileId, row.Amount, row.Currency, row.PeriodStart, row.PeriodEnd,
        paymentStatus = row.PaymentStatus, note = row.Note
    });

    static async Task<IResult> SaveAllowance(int? id, AllowanceInput input, HttpContext c, CareerDbContext db)
    {
        if (!await PortalWorkflow.CanWrite(db, c.User, "allowances")) return Results.Forbid();
        var title = input.Title?.Trim() ?? "";
        var currency = input.Currency?.Trim().ToUpperInvariant() ?? "";
        var note = input.Note?.Trim() ?? "";
        if (title.Length == 0 || title.Length > 120) return Error("Nhập loại phụ cấp tối đa 120 ký tự.");
        if (!SupportedCurrencies.Contains(currency)) return Error("Mã tiền tệ không hợp lệ; hãy dùng mã ISO 4217, ví dụ VND hoặc USD.");
        if (input.Amount < 0 || input.Amount > 9999999999999999.99m || decimal.Round(input.Amount, 2) != input.Amount)
            return Error("Số tiền phải không âm và có tối đa 2 chữ số thập phân.");
        if (input.PeriodStart is not DateOnly periodStart || input.PeriodEnd is not DateOnly periodEnd)
            return Error("Nhập đầy đủ ngày bắt đầu và kết thúc kỳ áp dụng.");
        if (periodEnd < periodStart) return Error("Ngày kết thúc kỳ áp dụng phải từ ngày bắt đầu trở đi.");
        if (note.Length > 2000) return Error("Ghi chú tối đa 2.000 ký tự.");
        if (!await db.InternProfiles.AnyAsync(x => x.Id == input.ProfileId)) return Error("Hồ sơ thực tập sinh không tồn tại.");

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var row = id is null ? new Allowance() : await db.Allowances.FirstOrDefaultAsync(x => x.Id == id);
        if (row is null) return Results.NotFound();
        if (id is not null && (input.Version is null || !row.Version.SequenceEqual(input.Version)))
            return Results.Conflict(new { message = "Dữ liệu đã đổi; tải lại trước khi lưu." });

        var previous = id is null ? "{}" : Snapshot(row);
        var actor = Actor(c.User);
        row.Title = title;
        row.ProfileId = input.ProfileId;
        row.Amount = input.Amount;
        row.Currency = currency;
        row.PeriodStart = periodStart;
        row.PeriodEnd = periodEnd;
        row.Note = note;
        row.UpdatedBy = actor;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        if (id is null)
        {
            row.PaymentStatus = "Chưa thanh toán";
            row.CreatedBy = actor;
            row.CreatedAt = row.UpdatedAt;
            db.Allowances.Add(row);
        }
        await db.SaveChangesAsync();
        db.AllowanceHistories.Add(new AllowanceHistory
        {
            AllowanceId = row.Id,
            ChangedBy = actor,
            Action = id is null ? "created" : "updated",
            PreviousValuesJson = previous,
            NewValuesJson = Snapshot(row)
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return id is null ? Results.Created($"/api/allowances/{row.Id}", ToAllowanceResponse(row)) : Results.Ok(ToAllowanceResponse(row));
    }
}

public sealed record AllowanceInput(string Title, int ProfileId, decimal Amount, string Currency,
    DateOnly? PeriodStart, DateOnly? PeriodEnd, string Note = "", byte[]? Version = null);
public sealed record AllowancePaymentStatusInput(string Status, string Feedback = "", byte[]? Version = null);
public sealed record LeaveRequestStatusInput(string Status, string Feedback = "");

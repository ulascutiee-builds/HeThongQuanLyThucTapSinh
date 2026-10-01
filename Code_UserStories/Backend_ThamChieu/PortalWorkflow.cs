using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class PortalWorkflow
{
    public static readonly string[] Kinds = ["programs", "mentors", "assignments", "tasks", "reports", "evaluations", "shifts", "attendance", "leave", "allowances", "support", "meetings", "notifications"];
    public static IQueryable<WorkItem> Visible(CareerDbContext db, ClaimsPrincipal user, string kind)
    {
        var q = db.WorkItems.Where(x => x.Kind == kind);
        if (PortalSecurity.Manager(user)) return q;
        var id = PortalSecurity.UserId(user);
        if (user.IsInRole("Intern"))
        {
            if (kind == "programs") return q.Where(x => db.WorkItems.Any(a => a.Kind == "assignments" && a.ProfileId == id && a.ProgramId == x.Id));
            if (kind == "mentors") return q.Where(x => db.WorkItems.Any(a => a.Kind == "assignments" && a.ProfileId == id && a.MentorId == x.Id));
            return q.Where(x => x.ProfileId == id);
        }
        var mentorIds = db.WorkItems.Where(x => x.Kind == "mentors" && x.Capacity == id).Select(x => x.Id);
        var profileIds = db.WorkItems.Where(x => x.Kind == "assignments" && mentorIds.Contains(x.MentorId ?? 0)).Select(x => x.ProfileId);
        return kind switch {
            "mentors" => q.Where(x => mentorIds.Contains(x.Id)),
            "programs" => q.Where(x => db.WorkItems.Any(a => a.Kind == "assignments" && mentorIds.Contains(a.MentorId ?? 0) && a.ProgramId == x.Id)),
            "allowances" or "support" => q.Where(x => false),
            _ => q.Where(x => profileIds.Contains(x.ProfileId))
        };
    }
    public static async Task<bool> CanWrite(CareerDbContext db, ClaimsPrincipal user, string kind)
    {
        if (user.IsInRole("Admin")) return true;
        if (!await HasPermission(db, user, kind)) return false;
        if (user.IsInRole("HR")) return true;
        var allowed = user.IsInRole("Mentor") ? new[] { "tasks", "evaluations", "meetings" } : new[] { "reports", "leave", "support" };
        if (!allowed.Contains(kind)) return false;
        if (user.IsInRole("Intern")) return true;
        var account = await db.PortalAccounts.FindAsync(PortalSecurity.UserId(user));
        return account is not null && (account.Permissions.Length == 0 || account.Permissions.Split(',').Contains(kind));
    }
    public static async Task<bool> HasPermission(CareerDbContext db, ClaimsPrincipal user, string kind)
    {
        if (user.IsInRole("Admin") || user.IsInRole("Intern")) return true;
        var a = await db.PortalAccounts.FindAsync(PortalSecurity.UserId(user));
        return a is not null && (a.Permissions.Length == 0 || a.Permissions.Split(',', StringSplitOptions.TrimEntries).Contains(kind));
    }
    public static async Task<string?> Validate(CareerDbContext db, WorkItem item, ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Length > 240 || item.Detail.Length > 10000) return "Nhập tiêu đề tối đa 240 ký tự; nội dung tối đa 10.000 ký tự.";
        if (item.End < item.Start) return "Ngày kết thúc phải sau ngày bắt đầu.";
        if (item.ProfileId is int pid && !await db.InternProfiles.AnyAsync(x => x.Id == pid)) return "Thực tập sinh không tồn tại.";
        if (item.Kind is not ("programs" or "mentors") && item.ProfileId is null) return "Chọn thực tập sinh.";
        if (user.IsInRole("Intern") && item.ProfileId != PortalSecurity.UserId(user)) return "Chỉ được thao tác dữ liệu cá nhân.";
        if (user.IsInRole("Mentor") && !await Visible(db, user, "assignments").AnyAsync(x => x.ProfileId == item.ProfileId)) return "Thực tập sinh không thuộc mentor này.";
        if (item.Kind is "programs" or "shifts" or "leave" or "meetings")
            if (item.Start is null || item.End is null) return "Nhập đủ thời gian bắt đầu và kết thúc.";
        if (item.Kind == "programs" && (item.Capacity <= 0 || string.IsNullOrWhiteSpace(item.Department))) return "Nhập phòng ban và chỉ tiêu lớn hơn 0.";
        if (item.Kind == "mentors")
        {
            if (item.Amount < 1 || item.Amount > 1000 || string.IsNullOrWhiteSpace(item.Department)) return "Nhập phòng ban và sức chứa mentor từ 1 đến 1000.";
            if (!await db.PortalAccounts.AnyAsync(x => x.Id == item.Capacity && x.Role == "Mentor" && x.Active)) return "Chọn tài khoản Mentor đang hoạt động.";
            if (await db.WorkItems.AnyAsync(x => x.Kind == "mentors" && x.Id != item.Id && x.Capacity == item.Capacity)) return "Tài khoản đã liên kết một mentor.";
        }
        if (item.Kind == "assignments")
        {
            var program = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == item.ProgramId && x.Kind == "programs");
            var mentor = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == item.MentorId && x.Kind == "mentors");
            if (program is null || mentor is null) return "Chọn chương trình và mentor hợp lệ.";
            if (program.Department != mentor.Department) return "Mentor và chương trình phải cùng phòng ban.";
            if (await db.WorkItems.AnyAsync(x => x.Kind == "assignments" && x.ProfileId == item.ProfileId && x.Id != item.Id)) return "Thực tập sinh đã được phân công. Hãy sửa phân công hiện tại.";
            if (await db.WorkItems.CountAsync(x => x.Kind == "assignments" && x.MentorId == item.MentorId && x.Id != item.Id) >= mentor.Amount) return "Mentor đã đủ số lượng thực tập sinh.";
            if (await db.WorkItems.CountAsync(x => x.Kind == "assignments" && x.ProgramId == item.ProgramId && x.Id != item.Id) >= program.Capacity) return "Chương trình đã đủ chỉ tiêu.";
            item.Start = program.Start; item.End = program.End;
            item.UniqueKey = $"assignment:{item.ProfileId}";
        }
        if (item.Kind == "tasks" && item.End is null) return "Công việc cần có hạn hoàn thành.";
        if (item.Kind == "reports")
        {
            if (item.Start is null) return "Chọn ngày đầu tuần báo cáo.";
            var day = item.Start.Value.Date;
            if (day.DayOfWeek != DayOfWeek.Monday) return "Kỳ báo cáo bắt đầu vào thứ Hai.";
            item.UniqueKey = $"report:{item.ProfileId}:{day:yyyyMMdd}";
            if (await db.WorkItems.AnyAsync(x => x.UniqueKey == item.UniqueKey && x.Id != item.Id)) return "Đã nộp báo cáo kỳ này. Hãy cập nhật báo cáo đã nộp.";
            item.Status = DateTimeOffset.UtcNow > item.Start.Value.AddDays(7) ? "Nộp muộn" : "Đã nộp";
        }
        if (item.Kind == "evaluations" && (item.Amount < 0 || item.Amount > 10 || item.Progress < 0 || item.Progress > 10)) return "Điểm kỹ năng và thái độ phải từ 0 đến 10.";
        if (item.Kind == "tasks" && (item.Progress < 0 || item.Progress > 100)) return "Tiến độ phải từ 0 đến 100%.";
        if (item.Kind == "allowances" && (item.Amount < 0 || item.Start is null)) return "Nhập kỳ áp dụng và số tiền không âm.";
        if (item.Kind is "shifts" or "leave")
            if (await db.WorkItems.AnyAsync(x => x.Kind == item.Kind && x.ProfileId == item.ProfileId && x.Id != item.Id && x.Status != "Từ chối" && x.Start < item.End && x.End > item.Start)) return "Khoảng thời gian bị trùng.";
        return null;
    }
    public static void Notify(CareerDbContext db, int profileId, string title, string detail, bool email = false)
    {
        db.WorkItems.Add(new WorkItem { Kind = "notifications", ProfileId = profileId, Title = title, Detail = detail, Status = "Chưa đọc" });
        if (email)
        {
            var profile = db.InternProfiles.Find(profileId);
            if (profile is not null) db.MailJobs.Add(new MailJob { Recipient = profile.Email, Subject = title, Body = detail });
        }
    }
}

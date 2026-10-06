using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    /// <summary>
    /// GET /api/interns/me/schedule
    /// Lịch tổng hợp của thực tập sinh đang đăng nhập.
    /// Bao gồm: thông tin chương trình, ca làm (shifts),
    /// công việc (tasks), và các mốc quan trọng (meetings).
    /// Quyền: Intern.
    /// </summary>
    private static void MapSchedule(RouteGroupBuilder group)
    {
        group.MapGet("/me/schedule", async (HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Intern")) return Results.Forbid();

            var internId = PortalSecurity.UserId(c.User);

            // ── 1. Lấy assignment của intern ──────────────────────────────
            var assignment = await db.WorkItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Kind == "assignments" && x.ProfileId == internId);

            // ── 2. Thông tin chương trình + mentor ────────────────────────
            ScheduleSummary? programSummary = null;
            if (assignment is not null)
            {
                var program = assignment.ProgramId.HasValue
                    ? await db.WorkItems.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == assignment.ProgramId && x.Kind == "programs")
                    : null;

                // Mentor WorkItem lưu AccountId trong Capacity field
                var mentorItem = assignment.MentorId.HasValue
                    ? await db.WorkItems.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == assignment.MentorId && x.Kind == "mentors")
                    : null;

                string mentorName = "";
                if (mentorItem?.Capacity is int accountId)
                {
                    var mentorAccount = await db.PortalAccounts.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == accountId);
                    mentorName = mentorAccount?.Name ?? "";
                }

                programSummary = new ScheduleSummary(
                    ProgramName: program?.Title ?? "",
                    Department:  program?.Department ?? assignment.Department,
                    MentorName:  mentorName,
                    Start:       assignment.Start,
                    End:         assignment.End);
            }

            // ── 3. Ca làm (shifts) ────────────────────────────────────────
            var shifts = await db.WorkItems
                .AsNoTracking()
                .Where(x => x.Kind == "shifts" && x.ProfileId == internId)
                .OrderBy(x => x.Start)
                .Select(x => new ScheduleItem(
                    x.Id, "shift", x.Title, x.Detail,
                    x.Start, x.End, x.Status, x.Progress))
                .ToListAsync();

            // ── 4. Công việc (tasks) ──────────────────────────────────────
            var tasks = await db.WorkItems
                .AsNoTracking()
                .Where(x => x.Kind == "tasks" && x.ProfileId == internId)
                .OrderBy(x => x.End)   // sắp xếp theo hạn hoàn thành
                .Select(x => new ScheduleItem(
                    x.Id, "task", x.Title, x.Detail,
                    x.Start, x.End, x.Status, x.Progress))
                .ToListAsync();

            // ── 5. Mốc quan trọng (meetings) ─────────────────────────────
            var milestones = await db.WorkItems
                .AsNoTracking()
                .Where(x => x.Kind == "meetings" && x.ProfileId == internId)
                .OrderBy(x => x.Start)
                .Select(x => new ScheduleItem(
                    x.Id, "meeting", x.Title, x.Detail,
                    x.Start, x.End, x.Status, x.Progress))
                .ToListAsync();

            var response = new InternScheduleResponse(
                Program:    programSummary,
                Shifts:     shifts,
                Tasks:      tasks,
                Milestones: milestones);

            return Results.Ok(response);
        })
        .WithName("GetInternSchedule");
    }
}

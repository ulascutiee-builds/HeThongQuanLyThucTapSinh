using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    /// <summary>
    /// PATCH /api/tasks/{id}/progress
    /// Cập nhật tiến độ hoàn thành (0–100%) của một task.
    /// Quyền:
    ///   - Intern: chỉ task thuộc chính mình
    ///   - Mentor: task của intern được assign cho mình
    ///   - HR / Admin: không giới hạn
    /// Tự động set Status:
    ///   - Progress == 100 → "Hoàn thành"
    ///   - Progress  < 100 → "Đang làm"
    /// </summary>
    public static void MapTaskProgressEndpoints(this WebApplication app)
    {
        app.MapMethods("/api/tasks/{id:int}/progress", ["PATCH"], async (
            int id,
            UpdateTaskProgressRequest input,
            HttpContext c,
            CareerDbContext db) =>
        {
            var userId    = PortalSecurity.UserId(c.User);
            var isManager = PortalSecurity.Manager(c.User);

            // Lấy task
            var task = await db.WorkItems
                .FirstOrDefaultAsync(x => x.Id == id && x.Kind == "tasks");
            if (task is null) return Results.NotFound();

            // Kiểm tra quyền
            if (!isManager)
            {
                if (c.User.IsInRole("Intern"))
                {
                    // Intern chỉ cập nhật task của chính mình
                    if (task.ProfileId != userId)
                        return Results.Forbid();
                }
                else if (c.User.IsInRole("Mentor"))
                {
                    // Mentor chỉ cập nhật task của intern được assign cho mình
                    var mentorItemIds = db.WorkItems
                        .Where(x => x.Kind == "mentors" && x.Capacity == userId)
                        .Select(x => x.Id);
                    var assignedProfileIds = db.WorkItems
                        .Where(x => x.Kind == "assignments" && mentorItemIds.Contains(x.MentorId ?? 0))
                        .Select(x => x.ProfileId);
                    if (!await assignedProfileIds.AnyAsync(pid => pid == task.ProfileId))
                        return Results.Forbid();
                }
                else
                {
                    return Results.Forbid();
                }
            }

            // Validate progress (double-check ngoài DataAnnotations)
            if (input.Progress is < 0 or > 100)
                return Results.BadRequest(new { message = "Tiến độ phải từ 0 đến 100%." });

            var oldProgress = task.Progress;
            task.Progress = input.Progress;
            task.Status   = input.Progress == 100 ? "Hoàn thành" : "Đang làm";
            task.History += $"{DateTimeOffset.UtcNow:O} {c.User.Identity!.Name}: " +
                            $"Cập nhật tiến độ {oldProgress}% → {input.Progress}%\n";

            // Nếu Mentor hoặc Admin cập nhật, gửi thông báo cho intern
            if (!c.User.IsInRole("Intern") && task.ProfileId is int targetId)
            {
                PortalWorkflow.Notify(
                    db,
                    targetId,
                    task.Title,
                    $"Tiến độ công việc đã cập nhật: {input.Progress}% – {task.Status}");
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                task.Id,
                task.Title,
                task.Progress,
                task.Status,
                task.End,
            });
        })
        .WithName("PatchTaskProgress")
        .WithTags("Công việc");
    }
}

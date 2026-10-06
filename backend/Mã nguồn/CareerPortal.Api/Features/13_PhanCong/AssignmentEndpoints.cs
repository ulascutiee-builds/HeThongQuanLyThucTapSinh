using Microsoft.EntityFrameworkCore;

public static partial class PortalEndpoints
{
    /// <summary>
    /// POST /api/assignments
    /// Tạo phân công mentor – thực tập sinh.
    /// Quyền: HR hoặc Admin.
    /// Ràng buộc:
    ///   - Intern chưa có assignment (unique per intern)
    ///   - Mentor còn capacity (WorkItem.Amount)
    ///   - Chương trình còn chỉ tiêu (WorkItem.Capacity)
    ///   - Mentor và chương trình cùng phòng ban
    ///   - Ngày bắt đầu chương trình không được sau ngày kết thúc
    /// </summary>
    public static void MapAssignmentEndpoints(this WebApplication app)
    {
        app.MapPost("/api/assignments", async (
            CreateAssignmentRequest input,
            HttpContext c,
            CareerDbContext db) =>
        {
            // Chỉ HR và Admin được tạo phân công
            if (!PortalSecurity.Manager(c.User)) return Results.Forbid();

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            // Dựng WorkItem tạm để chạy qua validator dùng chung
            var row = new WorkItem
            {
                Kind      = "assignments",
                Title     = $"Phân công thực tập sinh #{input.ProfileId}",
                ProfileId = input.ProfileId,
                ProgramId = input.ProgramId,
                MentorId  = input.MentorId,
                Status    = "Mới",
            };

            // Validate toàn bộ ràng buộc nghiệp vụ (duplicate intern,
            // mentor capacity, program capacity, department, start <= end)
            var problem = await PortalWorkflow.Validate(db, row, c.User);
            if (problem is not null)
                return Results.BadRequest(new { message = problem });

            // Lấy tên chương trình để đặt title rõ nghĩa hơn
            var program = await db.WorkItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == input.ProgramId && x.Kind == "programs");
            if (program is not null)
                row.Title = $"Phân công – {program.Title} – Intern #{input.ProfileId}";

            row.History += $"{DateTimeOffset.UtcNow:O} {c.User.Identity!.Name}: Tạo\n";
            db.WorkItems.Add(row);

            // Đồng bộ StartDate / EndDate lên InternProfile
            if (row.ProfileId is int pid)
            {
                var profile = await db.InternProfiles.FindAsync(pid);
                if (profile is not null)
                {
                    if (row.Start is not null)
                        profile.StartDate = DateOnly.FromDateTime(row.Start.Value.Date);
                    if (row.End is not null)
                        profile.EndDate = DateOnly.FromDateTime(row.End.Value.Date);
                }
            }

            // Gửi thông báo cho thực tập sinh
            PortalWorkflow.Notify(
                db,
                input.ProfileId,
                row.Title,
                $"Bạn đã được phân công vào chương trình. Thời gian: {row.Start:dd/MM/yyyy} – {row.End:dd/MM/yyyy}");

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Results.Created($"/api/work/assignments/{row.Id}", new
            {
                row.Id,
                row.Title,
                row.ProfileId,
                row.ProgramId,
                row.MentorId,
                row.Department,
                row.Start,
                row.End,
                row.Status,
                row.UniqueKey,
            });
        })
        .WithName("CreateAssignment")
        .WithTags("Phân công");
    }
}

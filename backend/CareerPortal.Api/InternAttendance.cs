using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class AttendanceReportEndpoints
{
    private static readonly string[] Statuses = ["Có mặt", "Nghỉ phép", "Vắng mặt"];

    public static void MapInternAttendance(this WebApplication app)
    {
        app.MapGet("/api/attendance/report", async (
            HttpContext context,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? profileId,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            if (from is null || to is null)
                return Results.BadRequest(new { message = "Vui lòng chọn ngày bắt đầu và ngày kết thúc." });
            if (to <= from)
                return Results.BadRequest(new { message = "Ngày kết thúc phải sau ngày bắt đầu." });
            if (profileId is <= 0)
                return Results.BadRequest(new { message = "Thực tập sinh được chọn không hợp lệ." });

            var schedules = await db.InternSchedules
                .AsNoTracking()
                .Where(schedule =>
                    schedule.StartsAt >= from &&
                    schedule.StartsAt < to &&
                    schedule.Status != "Đã hủy" &&
                    (profileId == null || schedule.ProfileId == profileId))
                .OrderBy(schedule => schedule.StartsAt)
                .ThenBy(schedule => schedule.Profile!.Name)
                .Select(schedule => new
                {
                    Schedule = schedule,
                    InternName = schedule.Profile!.Name,
                    StudentId = schedule.Profile.StudentId,
                    Attendance = schedule.Attendance
                })
                .ToListAsync(cancellationToken);

            var items = schedules.Select(row =>
            {
                var attendance = row.Attendance;
                var isPresent = attendance?.Status == "Có mặt";
                return new InternAttendanceReportResponse(
                    row.Schedule.Id,
                    row.Schedule.ProfileId,
                    row.InternName,
                    row.StudentId,
                    row.Schedule.Title,
                    row.Schedule.StartsAt,
                    row.Schedule.EndsAt,
                    attendance?.Id,
                    attendance?.WorkDate,
                    attendance?.Status ?? "Chưa chấm",
                    attendance?.ClockInAt,
                    attendance?.ClockOutAt,
                    attendance?.Note,
                    isPresent && attendance!.ClockInAt > row.Schedule.StartsAt,
                    isPresent && attendance!.ClockOutAt < row.Schedule.EndsAt);
            }).ToList();

            var summary = new InternAttendanceSummaryResponse(
                items.Where(item => item.AttendanceStatus == "Có mặt")
                    .GroupBy(item => new { item.ProfileId, item.WorkDate }).Count(),
                items.Where(item => item.IsLate)
                    .GroupBy(item => new { item.ProfileId, item.WorkDate }).Count(),
                items.Where(item => item.LeftEarly)
                    .GroupBy(item => new { item.ProfileId, item.WorkDate }).Count(),
                items.Where(item => item.AttendanceStatus == "Nghỉ phép")
                    .GroupBy(item => new { item.ProfileId, item.WorkDate }).Count(),
                items.Where(item => item.AttendanceStatus == "Vắng mặt")
                    .GroupBy(item => new { item.ProfileId, item.WorkDate }).Count(),
                items.Count(item => item.AttendanceStatus == "Chưa chấm"));

            return Results.Ok(new InternAttendanceReportResult(summary, items));
        })
        .WithName("GetInternAttendanceReport")
        .WithSummary("Lấy báo cáo chấm công thực tập sinh")
        .WithDescription("Lọc theo khoảng thời gian và hồ sơ; tổng hợp ngày công, đi muộn, về sớm, nghỉ phép và ca chưa chấm.")
        .Produces<InternAttendanceReportResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        app.MapPut("/api/hr/attendance/{scheduleId:int}", async (
            int scheduleId,
            SaveInternAttendanceRequest input,
            HttpContext context,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (input.WorkDate is null)
                return Results.BadRequest(new { message = "Vui lòng chọn ngày chấm công." });
            if (!Statuses.Contains(input.Status))
                return Results.BadRequest(new { message = "Trạng thái chấm công không hợp lệ." });

            var schedule = await db.InternSchedules
                .Include(item => item.Attendance)
                .SingleOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);
            if (schedule is null)
                return Results.NotFound(new { message = "Không tìm thấy ca thực tập." });
            if (schedule.Status == "Đã hủy")
                return Results.BadRequest(new { message = "Không thể chấm công cho ca đã hủy." });

            if (input.Status == "Có mặt")
            {
                if (input.ClockInAt is null || input.ClockOutAt is null)
                    return Results.BadRequest(new { message = "Vui lòng nhập giờ vào và giờ ra cho ca có mặt." });
                if (input.ClockOutAt <= input.ClockInAt)
                    return Results.BadRequest(new { message = "Giờ ra phải sau giờ vào." });
            }
            else if (input.ClockInAt is not null || input.ClockOutAt is not null)
            {
                return Results.BadRequest(new { message = "Ca nghỉ phép hoặc vắng mặt không được có giờ vào, giờ ra." });
            }

            var attendance = schedule.Attendance ?? new InternAttendance
            {
                ScheduleId = schedule.Id,
                ProfileId = schedule.ProfileId,
                UpdatedBy = string.Empty,
                Status = input.Status
            };
            attendance.WorkDate = input.WorkDate.Value;
            attendance.Status = input.Status;
            attendance.ClockInAt = input.ClockInAt;
            attendance.ClockOutAt = input.ClockOutAt;
            attendance.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            attendance.UpdatedBy = context.User.FindFirstValue(ClaimTypes.Name) ?? "HR";
            attendance.UpdatedAt = DateTimeOffset.UtcNow;

            if (schedule.Attendance is null) db.InternAttendances.Add(attendance);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { message = "Đã lưu thông tin chấm công." });
        })
        .WithName("SaveInternAttendance")
        .WithSummary("HR nhập hoặc cập nhật chấm công cho một ca")
        .WithDescription("Lưu trạng thái có mặt, nghỉ phép hoặc vắng mặt kèm giờ vào/ra thực tế.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}

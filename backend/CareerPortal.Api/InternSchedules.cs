using Microsoft.EntityFrameworkCore;

public static class InternSchedules
{
    private static readonly string[] Statuses =
    [
        "Đã lên lịch",
        "Đã xác nhận",
        "Đang diễn ra",
        "Đã hoàn thành",
        "Đã hủy"
    ];

    public static void MapInternSchedules(this WebApplication app)
    {
        app.MapGet("/api/interns/me/schedule", async (
            HttpContext context,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!context.User.IsInRole("Intern")) return Results.Forbid();

            var profileId = SprintSecurity.Id(context);
            var items = await db.InternSchedules
                .AsNoTracking()
                .Where(schedule => schedule.ProfileId == profileId)
                .OrderBy(schedule => schedule.StartsAt)
                .ThenBy(schedule => schedule.Id)
                .Select(schedule => new InternScheduleResponse(
                    schedule.Id,
                    schedule.ProfileId,
                    schedule.Profile!.Name,
                    schedule.Title,
                    schedule.StartsAt,
                    schedule.EndsAt,
                    schedule.Detail,
                    schedule.Status,
                    schedule.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        })
        .WithName("GetMyInternSchedule")
        .WithSummary("Lấy lịch của thực tập sinh đang đăng nhập")
        .WithDescription("Chỉ trả về các ca thuộc tài khoản thực tập sinh hiện tại.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        app.MapGet("/api/hr/schedules", async (
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            var items = await db.InternSchedules
                .AsNoTracking()
                .Include(schedule => schedule.Profile)
                .OrderBy(schedule => schedule.StartsAt)
                .ThenBy(schedule => schedule.Id)
                .Select(schedule => new InternScheduleResponse(
                    schedule.Id,
                    schedule.ProfileId,
                    schedule.Profile!.Name,
                    schedule.Title,
                    schedule.StartsAt,
                    schedule.EndsAt,
                    schedule.Detail,
                    schedule.Status,
                    schedule.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(new { items });
        })
        .WithName("GetInternSchedules")
        .WithSummary("Lấy danh sách lịch thực tập")
        .WithDescription("Chỉ tài khoản HR được xem lịch của tất cả thực tập sinh.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        app.MapPost("/api/hr/schedules", async (
            CreateInternScheduleRequest input,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (input.EndsAt <= input.StartsAt)
                return Results.BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });

            var profileExists = await db.InternProfiles
                .AnyAsync(profile => profile.Id == input.ProfileId, cancellationToken);
            if (!profileExists)
                return Results.NotFound(new { message = "Không tìm thấy hồ sơ thực tập sinh." });

            var schedule = new InternSchedule
            {
                ProfileId = input.ProfileId,
                Title = input.Title.Trim(),
                StartsAt = input.StartsAt,
                EndsAt = input.EndsAt,
                Detail = string.IsNullOrWhiteSpace(input.Detail) ? null : input.Detail.Trim()
            };
            db.InternSchedules.Add(schedule);
            await db.SaveChangesAsync(cancellationToken);

            await db.Entry(schedule).Reference(item => item.Profile).LoadAsync(cancellationToken);
            var response = schedule.ToResponse();
            return Results.Created($"/api/hr/schedules/{schedule.Id}", response);
        })
        .WithName("CreateInternSchedule")
        .WithSummary("Tạo ca thực tập")
        .WithDescription("Tạo lịch gắn với một hồ sơ thực tập sinh.")
        .Produces<InternScheduleResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPut("/api/hr/schedules/{id:int}", async (
            int id,
            UpdateInternScheduleRequest input,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!Statuses.Contains(input.Status))
                return Results.BadRequest(new { message = "Trạng thái lịch không hợp lệ." });
            if (input.EndsAt <= input.StartsAt)
                return Results.BadRequest(new { message = "Thời gian kết thúc phải sau thời gian bắt đầu." });

            var schedule = await db.InternSchedules
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (schedule is null) return Results.NotFound();

            var profile = await db.InternProfiles
                .SingleOrDefaultAsync(item => item.Id == input.ProfileId, cancellationToken);
            if (profile is null)
                return Results.NotFound(new { message = "Không tìm thấy hồ sơ thực tập sinh." });

            schedule.ProfileId = input.ProfileId;
            schedule.Profile = profile;
            schedule.Title = input.Title.Trim();
            schedule.StartsAt = input.StartsAt;
            schedule.EndsAt = input.EndsAt;
            schedule.Detail = string.IsNullOrWhiteSpace(input.Detail) ? null : input.Detail.Trim();
            schedule.Status = input.Status;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await db.Entry(schedule).Reference(item => item.Profile).LoadAsync(cancellationToken);
            return Results.Ok(schedule.ToResponse());
        })
        .WithName("UpdateInternSchedule")
        .WithSummary("Cập nhật ca và trạng thái thực tập")
        .WithDescription("Thay đổi lịch được lưu để thực tập sinh thấy ở lần tải lịch tiếp theo.")
        .Produces<InternScheduleResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPatch("/api/hr/schedules/{id:int}/status", async (
            int id,
            UpdateInternScheduleStatusRequest input,
            CareerDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!Statuses.Contains(input.Status))
                return Results.BadRequest(new { message = "Trạng thái lịch không hợp lệ." });

            var schedule = await db.InternSchedules
                .Include(item => item.Profile)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (schedule is null) return Results.NotFound();

            schedule.Status = input.Status;
            schedule.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(schedule.ToResponse());
        })
        .WithName("UpdateInternScheduleStatus")
        .WithSummary("Cập nhật trạng thái ca thực tập")
        .WithDescription("HR có thể đổi trạng thái để thực tập sinh nhìn thấy khi tải lại lịch.")
        .Produces<InternScheduleResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}

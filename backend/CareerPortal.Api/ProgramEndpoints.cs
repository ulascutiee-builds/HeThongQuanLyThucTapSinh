using Microsoft.EntityFrameworkCore;

public static class ProgramEndpoints
{
    private static readonly string[] Statuses = ["Sắp diễn ra", "Đang diễn ra", "Đã kết thúc"];

    public static void MapPrograms(this WebApplication app)
    {
        app.MapGet("/api/departments", async (HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            return Results.Ok(await db.Departments.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
        });

        app.MapPost("/api/departments", async (DepartmentInput input, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            var name = input.Name.Trim();
            if (name.Length == 0) return Results.BadRequest(new { message = "Tên phòng ban bắt buộc." });
            if (await db.Departments.AnyAsync(x => x.Name == name)) return Results.Conflict(new { message = "Phòng ban đã tồn tại." });

            var department = new Department { Name = name };
            db.Departments.Add(department);
            await db.SaveChangesAsync();
            return Results.Created($"/api/departments/{department.Id}", department);
        });

        app.MapGet("/api/programs", async (HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            var programs = await db.InternshipPrograms
                .AsNoTracking()
                .Include(x => x.Department)
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.DepartmentId,
                    departmentName = x.Department!.Name,
                    x.Description,
                    x.Capacity,
                    x.StartDate,
                    x.EndDate,
                    x.Status
                })
                .ToListAsync();
            return Results.Ok(programs);
        });

        app.MapPost("/api/programs", async (ProgramInput input, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            if (await Validate(input, db) is string error) return Results.BadRequest(new { message = error });

            var program = new InternshipProgram
            {
                Name = input.Name.Trim(),
                DepartmentId = input.DepartmentId,
                Description = input.Description?.Trim() ?? "",
                Capacity = input.Capacity,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Status = input.Status
            };
            db.InternshipPrograms.Add(program);
            await db.SaveChangesAsync();
            return Results.Created($"/api/programs/{program.Id}", new { program.Id });
        });

        app.MapPut("/api/programs/{id:int}", async (int id, ProgramInput input, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            var program = await db.InternshipPrograms.FindAsync(id);
            if (program is null) return Results.NotFound();
            if (await Validate(input, db) is string error) return Results.BadRequest(new { message = error });

            program.Name = input.Name.Trim();
            program.DepartmentId = input.DepartmentId;
            program.Description = input.Description?.Trim() ?? "";
            program.Capacity = input.Capacity;
            program.StartDate = input.StartDate;
            program.EndDate = input.EndDate;
            program.Status = input.Status;
            await db.SaveChangesAsync();
            return Results.Ok(new { program.Id });
        });

        app.MapDelete("/api/programs/{id:int}", async (int id, HttpContext context, CareerDbContext db) =>
        {
            if (!SprintSecurity.HR(context)) return Results.Forbid();
            var program = await db.InternshipPrograms.FindAsync(id);
            if (program is null) return Results.NotFound();
            db.InternshipPrograms.Remove(program);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<string?> Validate(ProgramInput input, CareerDbContext db)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) return "Tên chương trình bắt buộc.";
        if (!Statuses.Contains(input.Status)) return "Trạng thái chương trình không hợp lệ.";
        if (input.EndDate < input.StartDate) return "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";
        if (!await db.Departments.AnyAsync(x => x.Id == input.DepartmentId)) return "Phòng ban không tồn tại.";
        return null;
    }
}
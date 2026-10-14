// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 12-29; Sprint 3 Excel rows 50
    static bool ValidDates(DateOnly a, DateOnly b) => a.Year>=2000 && b.Year<=2100 && a<=b;
    static IQueryable<InternAssignment> Assignments(CareerDbContext db) => db.InternAssignments
        .Include(x=>x.Profile).Include(x=>x.Program!).ThenInclude(x=>x.Department)
        .Include(x=>x.Mentor!).ThenInclude(x=>x.User).Include(x=>x.Mentor!).ThenInclude(x=>x.Department);
    static IQueryable<InternAssignment> Visible(CareerDbContext db,HttpContext c)
    {
        var q=Assignments(db);
        if(Can(c,"assignments.manage")||Can(c,"attendance.report")) return q;
        if(c.User.IsInRole("Intern")) return q.Where(x=>x.ProfileId==SprintSecurity.Id(c));
        var uid=AccountId(c); return q.Where(x=>x.Mentor!.UserId==uid);
    }
    static object AssignmentDto(InternAssignment a) => new {
        a.Id,a.ProfileId,a.ProgramId,a.MentorId,a.AssignedAt,
        profile=a.Profile?.ToResponse(),
        program=a.Program is null?null:new {a.Program.Id,a.Program.Name,a.Program.DepartmentId,departmentName=a.Program.Department?.Name,a.Program.Description,a.Program.Capacity,a.Program.StartDate,a.Program.EndDate,a.Program.Status},
        mentor=a.Mentor is null?null:new {a.Mentor.Id,a.Mentor.UserId,name=a.Mentor.User?.Name,email=a.Mentor.User?.Email,a.Mentor.DepartmentId,departmentName=a.Mentor.Department?.Name,a.Mentor.Capacity}
    };
    static object ScheduleDto(ProgramSchedule s) => new {s.Id,s.ProgramId,s.Date,s.StartTime,s.EndTime,s.Title,s.Kind,status=s.Date<Today?"Past":s.Date==Today?"Today":"Upcoming"};

// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 133-135; Sprint 3 Excel rows 50
        app.MapGet("/api/assignments",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"schedule.read")&&!Can(c,"attendance.report"))return Results.Forbid();
            return Results.Ok((await Visible(db,c).AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync()).Select(AssignmentDto));


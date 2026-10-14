// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 161-165; Sprint 3 Excel rows 35
        app.MapGet("/api/schedules",async(int? programId,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage")&&!Can(c,"schedule.read"))return Results.Forbid();var q=db.ProgramSchedules.AsNoTracking();
            if(!Can(c,"programs.manage")){var ids=Visible(db,c).Select(x=>x.ProgramId);q=q.Where(x=>ids.Contains(x.ProgramId));}
            if(programId.HasValue)q=q.Where(x=>x.ProgramId==programId);return Results.Ok((await q.OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToListAsync()).Select(ScheduleDto));
        });

// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 187-192; Sprint 3 Excel rows 35
        app.MapGet("/api/interns/me/schedule",async(HttpContext c,CareerDbContext db)=>{
            if(!c.User.IsInRole("Intern")||!Can(c,"schedule.read"))return Results.Forbid();var id=SprintSecurity.Id(c);
            var a=await Assignments(db).AsNoTracking().FirstOrDefaultAsync(x=>x.ProfileId==id);
            var items=a is null?new List<ProgramSchedule>():await db.ProgramSchedules.AsNoTracking().Where(x=>x.ProgramId==a.ProgramId).OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToListAsync();
            return Results.Ok(new{assignment=a is null?null:AssignmentDto(a),items=items.Select(ScheduleDto),today=Today});
        });


// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 200-200; Sprint 3 Excel rows 56
        app.MapGet("/api/attendance/report",Report);

// Original: backend/CareerPortal.Api/Sprint2Endpoints.cs, lines 246-290; Sprint 3 Excel rows 56
        if(!c.User.IsInRole("Intern")||!Can(c,"attendance.write"))return Results.Forbid();var pid=SprintSecurity.Id(c);var now=LocalNow;var today=Today;
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(await db.AttendanceRecords.AnyAsync(x=>x.ProfileId==pid&&x.Date==today))return Results.Conflict(new{message="Bạn đã check-in hôm nay."});
        var a=await db.InternAssignments.Include(x=>x.Program).FirstOrDefaultAsync(x=>x.ProfileId==pid);
        if(a is null||a.Program!.Status is not ("Open" or "Active")||today<a.Program.StartDate||today>a.Program.EndDate)return Bad("Bạn chưa có chương trình đang hoạt động hôm nay.");
        if(await db.LeaveRequests.AnyAsync(x=>x.ProfileId==pid&&x.Status=="Approved"&&x.From<=today&&x.To>=today))return Bad("Hôm nay bạn có nghỉ phép được duyệt.");
        var shift=await db.ProgramSchedules.FirstOrDefaultAsync(x=>x.ProgramId==a.ProgramId&&x.Date==today&&x.Kind=="Shift");if(shift is null)return Bad("Hôm nay chưa có ca làm việc.");
        var begin=new DateTimeOffset(today.ToDateTime(shift.StartTime),TimeSpan.FromHours(7));var end=new DateTimeOffset(today.ToDateTime(shift.EndTime),TimeSpan.FromHours(7));
        var window=Math.Clamp(config.GetValue("Attendance:EarlyCheckInMinutes",60),0,180);
        if(now<begin.AddMinutes(-window)||now>end)return Bad("Chỉ check-in từ trước ca 60 phút đến khi kết thúc ca (theo cấu hình).");
        var grace=Math.Clamp(config.GetValue("Attendance:GraceMinutes",5),0,60);var late=(int)Math.Max(0,Math.Ceiling((now-begin).TotalMinutes));
        var r=new AttendanceRecord{ProfileId=pid,ScheduleId=shift.Id,Date=today,CheckIn=now,LateMinutes=late<=grace?0:late,IpAddress=c.Connection.RemoteIpAddress?.ToString()??"",Device=c.Request.Headers.UserAgent.ToString()[..Math.Min(c.Request.Headers.UserAgent.ToString().Length,500)]};
        db.AttendanceRecords.Add(r);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(AttendanceDto(r));
    }
    static async Task<IResult> CheckOut(HttpContext c,CareerDbContext db,IConfiguration config)
    {
        if(!c.User.IsInRole("Intern")||!Can(c,"attendance.write"))return Results.Forbid();var pid=SprintSecurity.Id(c);var today=Today;var now=LocalNow;
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var r=await db.AttendanceRecords.Include(x=>x.Schedule).FirstOrDefaultAsync(x=>x.ProfileId==pid&&x.Date==today);
        if(r is null)return Bad("Bạn chưa check-in hôm nay.");if(r.CheckOut is not null)return Results.Conflict(new{message="Bạn đã check-out hôm nay."});
        var end=new DateTimeOffset(today.ToDateTime(r.Schedule!.EndTime),TimeSpan.FromHours(7));var window=Math.Clamp(config.GetValue("Attendance:LateCheckOutMinutes",360),0,720);
        if(now<r.CheckIn||now>end.AddMinutes(window))return Bad("Thời gian check-out nằm ngoài ca được phép.");var early=(int)Math.Max(0,Math.Ceiling((end-now).TotalMinutes));var grace=Math.Clamp(config.GetValue("Attendance:GraceMinutes",5),0,60);
        r.CheckOut=now;r.EarlyMinutes=early<=grace?0:early;await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(AttendanceDto(r));
    }
    static async Task<IResult> Report(DateOnly? from,DateOnly? to,int? profileId,HttpContext c,CareerDbContext db)
    {
        if(!Can(c,"attendance.report"))return Results.Forbid();var start=from??new DateOnly(Today.Year,Today.Month,1);var end=to??Today;
        if(!ValidDates(start,end)||end.DayNumber-start.DayNumber>366)return Bad("Bộ lọc ngày phải hợp lệ và không quá 366 ngày.");
        var profiles=await db.InternProfiles.AsNoTracking().Where(x=>!profileId.HasValue||x.Id==profileId).OrderBy(x=>x.Name).ToListAsync();
        var rows=await db.AttendanceRecords.Include(x=>x.Profile).AsNoTracking().Where(x=>x.Date>=start&&x.Date<=end&&(!profileId.HasValue||x.ProfileId==profileId)).ToListAsync();
        var leaves=await db.LeaveRequests.AsNoTracking().Where(x=>x.Status=="Approved"&&x.From<=end&&x.To>=start&&(!profileId.HasValue||x.ProfileId==profileId)).ToListAsync();
        var assignments=await db.InternAssignments.AsNoTracking().Where(x=>!profileId.HasValue||x.ProfileId==profileId).ToListAsync();
        var schedules=await db.ProgramSchedules.AsNoTracking().Where(x=>x.Kind=="Shift"&&x.Date>=start&&x.Date<=end).ToListAsync();
        var details=new List<object>();var summaries=new List<object>();
        foreach(var p in profiles)
        {
            var records=rows.Where(x=>x.ProfileId==p.Id).ToList();var a=assignments.FirstOrDefault(x=>x.ProfileId==p.Id);var leaveDates=new HashSet<DateOnly>();
            if(a is not null)foreach(var s in schedules.Where(x=>x.ProgramId==a.ProgramId))
                if(leaves.Any(x=>x.ProfileId==p.Id&&x.From<=s.Date&&x.To>=s.Date))leaveDates.Add(s.Date);
            foreach(var r in records)details.Add(AttendanceDto(r));
            foreach(var day in leaveDates)details.Add(new{id=0,profileId=p.Id,name=p.Name,date=day,checkIn=(DateTimeOffset?)null,checkOut=(DateTimeOffset?)null,hours=0,lateMinutes=0,earlyMinutes=0,leave=true,status="Leave"});
            var expected=a is null?new List<DateOnly>():schedules.Where(x=>x.ProgramId==a.ProgramId&&new DateTimeOffset(x.Date.ToDateTime(x.EndTime),TimeSpan.FromHours(7))<=LocalNow).Select(x=>x.Date).Distinct().ToList();
            foreach(var day in expected.Where(d=>!leaveDates.Contains(d)&&records.All(r=>r.Date!=d)))details.Add(new{id=0,profileId=p.Id,name=p.Name,date=day,checkIn=(DateTimeOffset?)null,checkOut=(DateTimeOffset?)null,hours=0,lateMinutes=0,earlyMinutes=0,leave=false,status="Absent"});
            summaries.Add(new{profileId=p.Id,name=p.Name,workDays=records.Count(x=>x.CheckOut is not null),lateDays=records.Count(x=>x.LateMinutes>0),earlyDays=records.Count(x=>x.EarlyMinutes>0),leaveDays=leaveDates.Count,hours=Math.Round(records.Sum(Hours),2),absentDays=expected.Count(d=>!leaveDates.Contains(d)&&records.All(r=>r.Date!=d))});
        }
        return Results.Ok(new{from=start,to=end,summary=summaries,details});


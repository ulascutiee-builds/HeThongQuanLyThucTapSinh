// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 166-185 | Excel row(s): 58, 59
        app.MapPost("/api/schedules",async(ScheduleInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var error=await ValidateSchedule(input,db);if(error is not null)return Bad(error);
            var s=new ProgramSchedule{ProgramId=input.ProgramId,Date=input.Date,StartTime=input.StartTime,EndTime=input.EndTime,Title=input.Title.Trim(),Kind=input.Kind};db.ProgramSchedules.Add(s);await db.SaveChangesAsync();
            var recipients=await db.InternAssignments.Include(x=>x.Profile).Where(x=>x.ProgramId==s.ProgramId).Select(x=>x.Profile!).ToListAsync();
            foreach(var profile in recipients){db.PortalNotifications.Add(new(){ProfileId=profile.Id,Title="Lịch chương trình đã cập nhật",Message=$"Đã thêm {s.Title} vào ngày {s.Date:dd/MM/yyyy}.",Link="attendance.html"});db.MailJobs.Add(new(){EventKey=$"schedule:{s.Id}:{profile.Id}",Recipient=profile.Email,Subject="Lịch thực tập mới",Body=$"Xin chào {profile.Name},\nLịch {s.Title} ngày {s.Date:dd/MM/yyyy} ({s.StartTime:HH\\:mm}–{s.EndTime:HH\\:mm}) đã được thêm vào chương trình. Vui lòng đăng nhập Career Portal để xem chi tiết."});}
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/schedules/{s.Id}",ScheduleDto(s));
        });
        app.MapPut("/api/schedules/{id:int}",async(int id,ScheduleInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var s=await db.ProgramSchedules.FindAsync(id);if(s is null)return Results.NotFound();
            if(await db.AttendanceRecords.AnyAsync(x=>x.ScheduleId==id))return Results.Conflict(new{message="Ca đã có chấm công, không thể chỉnh sửa."});
            var error=await ValidateSchedule(input,db,id);if(error is not null)return Bad(error);
            s.ProgramId=input.ProgramId;s.Date=input.Date;s.StartTime=input.StartTime;s.EndTime=input.EndTime;s.Title=input.Title.Trim();s.Kind=input.Kind;await db.SaveChangesAsync();
            var recipients=await db.InternAssignments.Include(x=>x.Profile).Where(x=>x.ProgramId==s.ProgramId).Select(x=>x.Profile!).ToListAsync();
            foreach(var profile in recipients){db.PortalNotifications.Add(new(){ProfileId=profile.Id,Title="Lịch chương trình đã cập nhật",Message=$"Lịch {s.Title} ngày {s.Date:dd/MM/yyyy} đã được thay đổi.",Link="attendance.html"});db.MailJobs.Add(new(){EventKey=$"schedule:{s.Id}:{profile.Id}:{s.Date:yyyyMMdd}",Recipient=profile.Email,Subject="Lịch thực tập được cập nhật",Body=$"Xin chào {profile.Name},\nLịch {s.Title} ngày {s.Date:dd/MM/yyyy} đã được cập nhật. Vui lòng đăng nhập Career Portal để xem chi tiết."});}
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(ScheduleDto(s));
        });
        app.MapDelete("/api/schedules/{id:int}",async(int id,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();var s=await db.ProgramSchedules.FindAsync(id);if(s is null)return Results.NotFound();
            if(await db.AttendanceRecords.AnyAsync(x=>x.ScheduleId==id))return Results.Conflict(new{message="Ca đã có chấm công, không thể xóa."});db.ProgramSchedules.Remove(s);await db.SaveChangesAsync();return Results.NoContent();


// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 12-12 | Excel row(s): 39
    static bool ValidDates(DateOnly a, DateOnly b) => a.Year>=2000 && b.Year<=2100 && a<=b;

// Source: backend/CareerPortal.Api/Sprint2Endpoints.cs | lines 206-214 | Excel row(s): 39
        app.MapPost("/api/leave",async(LeaveInput input,HttpContext c,CareerDbContext db)=>{
            if(!c.User.IsInRole("Intern")||!Can(c,"attendance.write"))return Results.Forbid();var id=SprintSecurity.Id(c);
            if(!ValidDates(input.From,input.To)||input.To.DayNumber-input.From.DayNumber>366||input.Reason.Trim().Length==0)return Bad("Ngày nghỉ hoặc lý do không hợp lệ.");
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var a=await db.InternAssignments.Include(x=>x.Program).FirstOrDefaultAsync(x=>x.ProfileId==id);if(a is null||input.From<a.Program!.StartDate||input.To>a.Program.EndDate)return Bad("Ngày nghỉ phải thuộc thời gian chương trình được phân công.");
            if(await db.LeaveRequests.AnyAsync(x=>x.ProfileId==id&&x.Status!="Rejected"&&x.From<=input.To&&x.To>=input.From))return Results.Conflict(new{message="Trùng đơn nghỉ phép."});
            if(await db.AttendanceRecords.AnyAsync(x=>x.ProfileId==id&&x.Date>=input.From&&x.Date<=input.To))return Bad("Ngày đã chấm công không thể xin nghỉ phép.");
            var r=new LeaveRequest{ProfileId=id,From=input.From,To=input.To,Reason=input.Reason.Trim()};db.LeaveRequests.Add(r);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/leave/{r.Id}",new{r.Id});
        });


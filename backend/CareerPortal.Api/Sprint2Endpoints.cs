using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public static class Sprint2Endpoints
{
    public static DateTimeOffset LocalNow => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
    public static DateOnly Today => DateOnly.FromDateTime(LocalNow.DateTime);
    static int AccountId(HttpContext c) => int.TryParse(c.User.FindFirstValue("account_id"), out var id) ? id : 0;
    static bool Can(HttpContext c, string key) => SprintSecurity.HasPermission(c,key);
    static IResult Bad(string message) => Results.BadRequest(new {message});
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
    static object AttendanceDto(AttendanceRecord r) => new {r.Id,r.ProfileId,name=r.Profile?.Name,r.Date,r.CheckIn,r.CheckOut,hours=Hours(r),r.LateMinutes,r.EarlyMinutes,leave=false,status=r.CheckOut is null?"Working":"Completed",r.ScheduleId};
    static double Hours(AttendanceRecord r) => r.CheckOut is null?0:Math.Round(Math.Max(0,(r.CheckOut.Value-r.CheckIn).TotalHours),2);
    static object LeaveDto(LeaveRequest r) => new {r.Id,r.ProfileId,name=r.Profile?.Name,r.From,r.To,r.Reason,r.Status,r.Note,r.ReviewedBy,r.ReviewedAt};
    public static void ConfigureSprint2(this ModelBuilder b)
    {
        b.Entity<Department>(e=>{e.ToTable("DEPARTMENT");e.Property(x=>x.Name).HasMaxLength(120);e.HasIndex(x=>x.Name).IsUnique();});
        b.Entity<InternshipProgram>(e=>{
            e.ToTable("PROGRAM",t=>{t.HasCheckConstraint("CK_Program_Dates","[EndDate] >= [StartDate]");t.HasCheckConstraint("CK_Program_Capacity","[Capacity] > 0");});
            e.Property(x=>x.Name).HasMaxLength(160);e.Property(x=>x.Description).HasMaxLength(2000);e.Property(x=>x.Status).HasMaxLength(20);
            e.HasOne(x=>x.Department).WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Mentor>(e=>{
            e.ToTable("MENTOR",t=>t.HasCheckConstraint("CK_Mentor_Capacity","[Capacity] > 0"));e.HasIndex(x=>x.UserId).IsUnique();
            e.HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Department).WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<InternAssignment>(e=>{
            e.ToTable("ASSIGNMENT");e.HasIndex(x=>x.ProfileId).IsUnique();
            e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Program).WithMany().HasForeignKey(x=>x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Mentor).WithMany().HasForeignKey(x=>x.MentorId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ProgramSchedule>(e=>{
            e.ToTable("SCHEDULE",t=>t.HasCheckConstraint("CK_Schedule_Time","[EndTime] > [StartTime]"));e.Property(x=>x.Title).HasMaxLength(200);e.Property(x=>x.Kind).HasMaxLength(20);
            e.HasIndex(x=>new{x.ProgramId,x.Date});e.HasOne(x=>x.Program).WithMany().HasForeignKey(x=>x.ProgramId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AttendanceRecord>(e=>{
            e.ToTable("ATTENDANCE",t=>t.HasCheckConstraint("CK_Attendance_Time","[CheckOut] IS NULL OR [CheckOut] >= [CheckIn]"));
            e.HasIndex(x=>new{x.ProfileId,x.Date}).IsUnique();e.Property(x=>x.IpAddress).HasMaxLength(64);e.Property(x=>x.Device).HasMaxLength(500);
            e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Schedule).WithMany().HasForeignKey(x=>x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LeaveRequest>(e=>{
            e.ToTable("LEAVE_REQUEST",t=>t.HasCheckConstraint("CK_Leave_Dates","[To] >= [From]"));e.Property(x=>x.Reason).HasMaxLength(1000);e.Property(x=>x.Note).HasMaxLength(1000);e.Property(x=>x.Status).HasMaxLength(20);
            e.HasIndex(x=>new{x.ProfileId,x.From,x.To});e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Restrict);
        });
    }
    public static void MapSprint2(this WebApplication app)
    {
        app.MapGet("/api/departments",async(HttpContext c,CareerDbContext db)=> {
            if(!Can(c,"programs.manage")&&!Can(c,"schedule.read")&&!Can(c,"assignments.manage"))return Results.Forbid();
            return Results.Ok(await db.Departments.AsNoTracking().OrderBy(x=>x.Name).ToListAsync());
        });
        app.MapPost("/api/departments",async(DepartmentInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();var name=input.Name.Trim();if(name.Length==0)return Bad("Tên phòng ban bắt buộc.");
            if(await db.Departments.AnyAsync(x=>x.Name==name))return Results.Conflict(new{message="Phòng ban đã tồn tại."});
            var d=new Department{Name=name};db.Departments.Add(d);await db.SaveChangesAsync();return Results.Created($"/api/departments/{d.Id}",d);
        });
        app.MapGet("/api/programs",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage")&&!Can(c,"schedule.read")&&!Can(c,"assignments.manage"))return Results.Forbid();
            var q=db.InternshipPrograms.Include(x=>x.Department).AsNoTracking();
            if(!Can(c,"programs.manage")&&!Can(c,"assignments.manage")){var ids=Visible(db,c).Select(x=>x.ProgramId);q=q.Where(x=>ids.Contains(x.Id));}
            var rows=await q.OrderByDescending(x=>x.Id).ToListAsync();var counts=await db.InternAssignments.GroupBy(x=>x.ProgramId).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
            return Results.Ok(rows.Select(p=>new{p.Id,p.Name,p.DepartmentId,departmentName=p.Department?.Name,p.Description,p.Capacity,p.StartDate,p.EndDate,p.Status,assignedCount=counts.GetValueOrDefault(p.Id)}));
        });
        app.MapPost("/api/programs",async(ProgramInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();var error=await ValidateProgram(input,db);if(error is not null)return Bad(error);
            var p=new InternshipProgram{Name=input.Name.Trim(),DepartmentId=input.DepartmentId,Description=(input.Description??"").Trim(),Capacity=input.Capacity,StartDate=input.StartDate,EndDate=input.EndDate,Status=input.Status};
            db.InternshipPrograms.Add(p);await db.SaveChangesAsync();return Results.Created($"/api/programs/{p.Id}",new{p.Id});
        });
        app.MapPut("/api/programs/{id:int}",async(int id,ProgramInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();var p=await db.InternshipPrograms.FindAsync(id);if(p is null)return Results.NotFound();
            var error=await ValidateProgram(input,db);if(error is not null)return Bad(error);
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var assignments=await db.InternAssignments.Include(x=>x.Profile).Include(x=>x.Mentor).Where(x=>x.ProgramId==id).ToListAsync();
            if(input.Capacity<assignments.Count)return Results.Conflict(new{message="Chỉ tiêu thấp hơn số thực tập sinh đã phân công."});
            if(assignments.Any(x=>x.Mentor!.DepartmentId!=input.DepartmentId))return Bad("Phòng ban mới không khớp mentor đang phân công.");
            if(await db.ProgramSchedules.AnyAsync(x=>x.ProgramId==id&&(x.Date<input.StartDate||x.Date>input.EndDate)))return Bad("Lịch hiện có nằm ngoài khoảng ngày mới. Hãy điều chỉnh lịch trước.");
            if(await db.LeaveRequests.AnyAsync(x=>assignments.Select(a=>a.ProfileId).Contains(x.ProfileId)&&x.Status!="Rejected"&&(x.From<input.StartDate||x.To>input.EndDate)))return Bad("Đơn nghỉ phép hiện có nằm ngoài khoảng ngày mới.");
            p.Name=input.Name.Trim();p.DepartmentId=input.DepartmentId;p.Description=(input.Description??"").Trim();p.Capacity=input.Capacity;p.StartDate=input.StartDate;p.EndDate=input.EndDate;p.Status=input.Status;
            foreach(var a in assignments){a.Profile!.StartDate=p.StartDate;a.Profile.EndDate=p.EndDate;}
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{p.Id});
        });
        app.MapDelete("/api/programs/{id:int}",async(int id,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage"))return Results.Forbid();var p=await db.InternshipPrograms.FindAsync(id);if(p is null)return Results.NotFound();
            if(await db.InternAssignments.AnyAsync(x=>x.ProgramId==id)||await db.ProgramSchedules.AnyAsync(x=>x.ProgramId==id))return Results.Conflict(new{message="Chương trình đang có phân công/lịch. Hãy gỡ dữ liệu liên quan hoặc đóng chương trình."});
            db.InternshipPrograms.Remove(p);await db.SaveChangesAsync();return Results.NoContent();
        });
        app.MapGet("/api/mentors/candidates",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage"))return Results.Forbid();
            var ids=db.UserRoles.Where(x=>x.RoleName=="Mentor").Select(x=>x.UserId);
            return Results.Ok(await db.AppUsers.Where(x=>ids.Contains(x.Id)&&x.IsActive&&!x.RequiresActivation).Select(x=>new{x.Id,x.Name,x.Email}).ToListAsync());
        });
        app.MapGet("/api/mentors",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage")&&!Can(c,"schedule.read"))return Results.Forbid();
            var q=db.Mentors.Include(x=>x.User).Include(x=>x.Department).AsNoTracking();
            if(!Can(c,"assignments.manage")&&!Can(c,"programs.manage")){var ids=Visible(db,c).Select(x=>x.MentorId);q=q.Where(x=>ids.Contains(x.Id));}
            var rows=await q.OrderBy(x=>x.Id).ToListAsync();var counts=await db.InternAssignments.GroupBy(x=>x.MentorId).Select(x=>new{Id=x.Key,Count=x.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
            return Results.Ok(rows.Select(m=>new{m.Id,m.UserId,name=m.User?.Name,email=m.User?.Email,m.DepartmentId,departmentName=m.Department?.Name,m.Capacity,assignedCount=counts.GetValueOrDefault(m.Id)}));
        });
        app.MapPost("/api/mentors",async(MentorInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();var error=await ValidateMentor(input,db);if(error is not null)return Bad(error);
            if(await db.Mentors.AnyAsync(x=>x.UserId==input.UserId))return Results.Conflict(new{message="Tài khoản đã có hồ sơ mentor."});
            var m=new Mentor{UserId=input.UserId,DepartmentId=input.DepartmentId,Capacity=input.Capacity};db.Mentors.Add(m);await db.SaveChangesAsync();return Results.Created($"/api/mentors/{m.Id}",new{m.Id});
        });
        app.MapPut("/api/mentors/{id:int}",async(int id,MentorInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();var m=await db.Mentors.FindAsync(id);if(m is null)return Results.NotFound();
            var error=await ValidateMentor(input,db);if(error is not null)return Bad(error);await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var assigned=await db.InternAssignments.Include(x=>x.Program).Where(x=>x.MentorId==id).ToListAsync();
            if(input.Capacity<assigned.Count)return Bad("Khả năng nhận thấp hơn số đã phân công.");
            if(assigned.Any(x=>x.Program!.DepartmentId!=input.DepartmentId))return Bad("Mentor đang phụ trách chương trình của phòng ban khác.");
            m.UserId=input.UserId;m.DepartmentId=input.DepartmentId;m.Capacity=input.Capacity;await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{m.Id});
        });
        app.MapGet("/api/assignments",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage")&&!Can(c,"schedule.read")&&!Can(c,"attendance.report"))return Results.Forbid();
            return Results.Ok((await Visible(db,c).AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync()).Select(AssignmentDto));
        });
        app.MapPost("/api/assignments",async(AssignmentInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var p=await db.InternProfiles.FindAsync(input.ProfileId);var program=await db.InternshipPrograms.FindAsync(input.ProgramId);var mentor=await db.Mentors.FindAsync(input.MentorId);
            if(p is null||program is null||mentor is null)return Bad("Hồ sơ, chương trình hoặc mentor không tồn tại.");
            if(program.Status is not ("Open" or "Active"))return Bad("Chương trình chưa mở hoặc đã đóng.");
            if(await db.InternAssignments.AnyAsync(x=>x.ProfileId==p.Id))return Results.Conflict(new{message="Thực tập sinh đã có phân công."});
            if(mentor.DepartmentId!=program.DepartmentId)return Bad("Mentor phải thuộc cùng phòng ban với chương trình.");
            if(!await MentorActive(mentor.UserId,db))return Bad("Tài khoản mentor chưa hoạt động.");
            if(await db.InternAssignments.CountAsync(x=>x.ProgramId==program.Id)>=program.Capacity||await db.InternAssignments.CountAsync(x=>x.MentorId==mentor.Id)>=mentor.Capacity)return Results.Conflict(new{message="Chương trình hoặc mentor đã đủ chỉ tiêu."});
            var a=new InternAssignment{ProfileId=p.Id,ProgramId=program.Id,MentorId=mentor.Id};db.InternAssignments.Add(a);p.StartDate=program.StartDate;p.EndDate=program.EndDate;
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/assignments/{a.Id}",new{a.Id});
        });
        app.MapPut("/api/assignments/{id:int}",async(int id,ChangeMentorInput input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var a=await db.InternAssignments.Include(x=>x.Program).FirstOrDefaultAsync(x=>x.Id==id);var m=await db.Mentors.FindAsync(input.MentorId);if(a is null)return Results.NotFound();
            if(m is null||m.DepartmentId!=a.Program!.DepartmentId||!await MentorActive(m.UserId,db))return Bad("Mentor không hợp lệ hoặc khác phòng ban.");
            if(await db.InternAssignments.CountAsync(x=>x.MentorId==m.Id&&x.Id!=id)>=m.Capacity)return Results.Conflict(new{message="Mentor đã đủ tải."});
            a.MentorId=m.Id;await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(new{a.Id});
        });
        app.MapDelete("/api/assignments/{id:int}",async(int id,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"assignments.manage"))return Results.Forbid();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var a=await db.InternAssignments.FindAsync(id);if(a is null)return Results.NotFound();
            if(await db.AttendanceRecords.AnyAsync(x=>x.ProfileId==a.ProfileId)||await db.LeaveRequests.AnyAsync(x=>x.ProfileId==a.ProfileId))return Results.Conflict(new{message="Đã có chấm công/nghỉ phép. Giữ phân công để bảo toàn báo cáo."});
            db.InternAssignments.Remove(a);await db.SaveChangesAsync();await tx.CommitAsync();return Results.NoContent();
        });
        app.MapGet("/api/schedules",async(int? programId,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"programs.manage")&&!Can(c,"schedule.read"))return Results.Forbid();var q=db.ProgramSchedules.AsNoTracking();
            if(!Can(c,"programs.manage")){var ids=Visible(db,c).Select(x=>x.ProgramId);q=q.Where(x=>ids.Contains(x.ProgramId));}
            if(programId.HasValue)q=q.Where(x=>x.ProgramId==programId);return Results.Ok((await q.OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToListAsync()).Select(ScheduleDto));
        });
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
        });
        app.MapGet("/api/interns/me/schedule",async(HttpContext c,CareerDbContext db)=>{
            if(!c.User.IsInRole("Intern")||!Can(c,"schedule.read"))return Results.Forbid();var id=SprintSecurity.Id(c);
            var a=await Assignments(db).AsNoTracking().FirstOrDefaultAsync(x=>x.ProfileId==id);
            var items=a is null?new List<ProgramSchedule>():await db.ProgramSchedules.AsNoTracking().Where(x=>x.ProgramId==a.ProgramId).OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToListAsync();
            return Results.Ok(new{assignment=a is null?null:AssignmentDto(a),items=items.Select(ScheduleDto),today=Today});
        });
        app.MapGet("/api/attendance/me",async(HttpContext c,CareerDbContext db)=>{
            if(!c.User.IsInRole("Intern")||!Can(c,"attendance.write"))return Results.Forbid();var id=SprintSecurity.Id(c);var today=Today;
            var rows=await db.AttendanceRecords.Include(x=>x.Profile).AsNoTracking().Where(x=>x.ProfileId==id).OrderByDescending(x=>x.Date).ToListAsync();var current=rows.FirstOrDefault(x=>x.Date==today);
            return Results.Ok(new{items=rows.Select(AttendanceDto),today=current is null?null:AttendanceDto(current)});
        });
        app.MapPost("/api/attendance/check-in",CheckIn);
        app.MapPost("/api/attendance/check-out",CheckOut);
        app.MapGet("/api/attendance/report",Report);
        app.MapGet("/api/leave",async(HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"attendance.report")&&!Can(c,"attendance.write"))return Results.Forbid();var q=db.LeaveRequests.Include(x=>x.Profile).AsNoTracking();
            if(!Can(c,"attendance.report")){if(!c.User.IsInRole("Intern"))return Results.Forbid();var id=SprintSecurity.Id(c);q=q.Where(x=>x.ProfileId==id);}
            return Results.Ok((await q.OrderByDescending(x=>x.Id).ToListAsync()).Select(LeaveDto));
        });
        app.MapPost("/api/leave",async(LeaveInput input,HttpContext c,CareerDbContext db)=>{
            if(!c.User.IsInRole("Intern")||!Can(c,"attendance.write"))return Results.Forbid();var id=SprintSecurity.Id(c);
            if(!ValidDates(input.From,input.To)||input.To.DayNumber-input.From.DayNumber>366||input.Reason.Trim().Length==0)return Bad("Ngày nghỉ hoặc lý do không hợp lệ.");
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var a=await db.InternAssignments.Include(x=>x.Program).FirstOrDefaultAsync(x=>x.ProfileId==id);if(a is null||input.From<a.Program!.StartDate||input.To>a.Program.EndDate)return Bad("Ngày nghỉ phải thuộc thời gian chương trình được phân công.");
            if(await db.LeaveRequests.AnyAsync(x=>x.ProfileId==id&&x.Status!="Rejected"&&x.From<=input.To&&x.To>=input.From))return Results.Conflict(new{message="Trùng đơn nghỉ phép."});
            if(await db.AttendanceRecords.AnyAsync(x=>x.ProfileId==id&&x.Date>=input.From&&x.Date<=input.To))return Bad("Ngày đã chấm công không thể xin nghỉ phép.");
            var r=new LeaveRequest{ProfileId=id,From=input.From,To=input.To,Reason=input.Reason.Trim()};db.LeaveRequests.Add(r);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/leave/{r.Id}",new{r.Id});
        });
        app.MapPatch("/api/leave/{id:int}",async(int id,LeaveDecision input,HttpContext c,CareerDbContext db)=>{
            if(!Can(c,"attendance.report"))return Results.Forbid();if(input.Status is not ("Approved" or "Rejected")||(input.Status=="Rejected"&&string.IsNullOrWhiteSpace(input.Note)))return Bad("Trạng thái hoặc lý do từ chối không hợp lệ.");
            await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);var r=await db.LeaveRequests.Include(x=>x.Profile).FirstOrDefaultAsync(x=>x.Id==id);if(r is null)return Results.NotFound();if(r.Status!="Pending")return Results.Conflict(new{message="Đơn đã được xử lý."});
            if(input.Status=="Approved"&&await db.AttendanceRecords.AnyAsync(x=>x.ProfileId==r.ProfileId&&x.Date>=r.From&&x.Date<=r.To))return Bad("Ngày đã có chấm công không thể duyệt nghỉ phép.");
            r.Status=input.Status;r.Note=input.Note?.Trim();r.ReviewedBy=c.User.Identity?.Name;r.ReviewedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(LeaveDto(r));
        });
    }
    static async Task<string?> ValidateProgram(ProgramInput input,CareerDbContext db)
    {
        if(input.Name.Trim().Length==0||!ValidDates(input.StartDate,input.EndDate))return "Tên hoặc thời gian chương trình không hợp lệ.";
        if(input.Status is not ("Draft" or "Open" or "Active" or "Completed" or "Closed"))return "Trạng thái chương trình không hợp lệ.";
        if(!await db.Departments.AnyAsync(x=>x.Id==input.DepartmentId))return "Phòng ban không tồn tại.";return null;
    }
    static async Task<bool> MentorActive(int uid,CareerDbContext db)
    {
        return await db.AppUsers.AnyAsync(x=>x.Id==uid&&x.IsActive&&!x.RequiresActivation)&&await db.UserRoles.AnyAsync(x=>x.UserId==uid&&x.RoleName=="Mentor");
    }
    static async Task<string?> ValidateMentor(MentorInput input,CareerDbContext db)
    {
        if(!await MentorActive(input.UserId,db))return "Hãy chọn tài khoản Mentor đã kích hoạt.";
        if(!await db.Departments.AnyAsync(x=>x.Id==input.DepartmentId))return "Phòng ban không tồn tại.";return null;
    }
    static async Task<string?> ValidateSchedule(ScheduleInput input,CareerDbContext db,int exceptId=0)
    {
        var p=await db.InternshipPrograms.FindAsync(input.ProgramId);if(p is null)return "Chương trình không tồn tại.";
        if(input.Date<p.StartDate||input.Date>p.EndDate||input.StartTime>=input.EndTime||input.Title.Trim().Length==0||input.Kind is not ("Shift" or "Milestone"))return "Ngày, giờ, tiêu đề hoặc loại lịch không hợp lệ.";
        if(input.Kind=="Shift"&&await db.ProgramSchedules.AnyAsync(x=>x.Id!=exceptId&&x.ProgramId==input.ProgramId&&x.Date==input.Date&&x.Kind=="Shift"))return "Mỗi chương trình có một ca làm việc trong ngày. Hãy sửa ca hiện có.";
        return null;
    }
    static async Task<IResult> CheckIn(HttpContext c,CareerDbContext db,IConfiguration config)
    {
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
    }
}

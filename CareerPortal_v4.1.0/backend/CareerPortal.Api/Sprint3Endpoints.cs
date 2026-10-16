using Microsoft.EntityFrameworkCore;

public static class Sprint3Endpoints
{
    static bool Can(HttpContext c,string key)=>SprintSecurity.HasPermission(c,key);
    static int Account(HttpContext c)=>SprintSecurity.AccountId(c);
    static int Profile(HttpContext c)=>SprintSecurity.Id(c);
    static int NotificationProfile(HttpContext c)=>c.User.IsInRole("Intern") ? Profile(c) : 0;
    static IResult Bad(string message)=>Results.BadRequest(new{message});
    static object TaskDto(InternshipTask t)=>new{t.Id,t.Title,t.Description,t.DueDate,t.Priority,t.Status,t.Progress,t.ProfileId,profile=t.Profile is null?null:new{t.Profile.Id,t.Profile.Name,t.Profile.Email},t.MentorId,mentor=t.Mentor?.User is null?null:new{t.Mentor.User.Id,name=t.Mentor.User.Name,email=t.Mentor.User.Email},t.ProgramId,program=t.Program is null?null:new{t.Program.Id,t.Program.Name},t.CreatedAt,t.UpdatedAt};
    static object ReportDto(WeeklyReport r)=>new{r.Id,r.ProfileId,name=r.Profile?.Name,email=r.Profile?.Email,r.WeekStart,r.Summary,r.Blockers,r.EvidenceUrl,r.Status,r.MentorFeedback,r.SubmittedAt,r.ReviewedAt,r.ReviewedBy};
    static bool CanReadAllTasks(HttpContext c) => (c.User.IsInRole("HR") || c.User.IsInRole("Admin"))
        && (Can(c, "assignments.manage") || Can(c, "profiles.read"));
    static async Task<IResult> ListTasks(HttpContext c, CareerDbContext db)
    {
        var query = db.InternshipTasks.Include(x => x.Profile).Include(x => x.Mentor!).ThenInclude(x => x.User)
            .Include(x => x.Program).AsNoTracking();
        if (!CanReadAllTasks(c))
        {
            if (c.User.IsInRole("Intern")) query = query.Where(x => x.ProfileId == Profile(c));
            else if (c.User.IsInRole("Mentor"))
            {
                var mentorId = await db.Mentors.Where(x => x.UserId == Account(c)).Select(x => x.Id).SingleOrDefaultAsync();
                if (mentorId == 0) return Results.Ok(Array.Empty<object>());
                query = query.Where(x => db.InternAssignments.Any(a => a.ProfileId == x.ProfileId && a.MentorId == mentorId
                    && (x.ProgramId == null || a.ProgramId == x.ProgramId)));
            }
            else return Results.Forbid();
        }
        return Results.Ok((await query.OrderBy(x => x.DueDate).ThenByDescending(x => x.Id).ToListAsync()).Select(TaskDto));
    }
    static async Task<IResult> TaskHistory(int id, HttpContext c, CareerDbContext db)
    {
        var task = await db.InternshipTasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (task is null) return Results.NotFound();
        var visible = CanReadAllTasks(c) || c.User.IsInRole("Intern") && task.ProfileId == Profile(c);
        if (!visible && c.User.IsInRole("Mentor"))
        {
            var mentorId = await db.Mentors.Where(x => x.UserId == Account(c)).Select(x => x.Id).SingleOrDefaultAsync();
            visible = mentorId != 0 && await db.InternAssignments.AnyAsync(a => a.ProfileId == task.ProfileId && a.MentorId == mentorId
                && (task.ProgramId == null || a.ProgramId == task.ProgramId));
        }
        if (!visible) return Results.Forbid();
        return Results.Ok(await db.TaskProgressHistories.AsNoTracking().Where(x => x.TaskId == id)
            .OrderByDescending(x => x.ChangedAt).Select(x => new { x.Id, x.Progress, x.Status, x.Note, x.ChangedAt, x.ChangedByUserId }).ToListAsync());
    }
    static async Task<IResult> UpdateTask(int id, TaskInput input, HttpContext c, CareerDbContext db)
    {
        var manager = (c.User.IsInRole("HR") || c.User.IsInRole("Admin")) && Can(c, "assignments.manage");
        var mentor = c.User.IsInRole("Mentor") && Can(c, "tasks.manage");
        if (!manager && !mentor) return Results.Forbid();

        var task = await db.InternshipTasks.SingleOrDefaultAsync(x => x.Id == id);
        if (task is null) return Results.NotFound();
        var currentMentorId = 0;
        if (!manager)
        {
            currentMentorId = await db.Mentors.Where(x => x.UserId == Account(c)).Select(x => x.Id).SingleOrDefaultAsync();
            if (currentMentorId == 0 || !await db.InternAssignments.AnyAsync(a => a.ProfileId == task.ProfileId && a.MentorId == currentMentorId
                && (task.ProgramId == null || a.ProgramId == task.ProgramId))) return Results.Forbid();
        }
        if (string.IsNullOrWhiteSpace(input.Title)) return Bad("Tên nhiệm vụ bắt buộc.");
        if (input.DueDate is DateOnly dueDate && dueDate < DateOnly.FromDateTime(DateTime.Now) && input.DueDate != task.DueDate)
            return Bad("Deadline không được ở quá khứ.");

        if (input.ProfileId != task.ProfileId || input.ProgramId != task.ProgramId)
        {
            var assignment = await db.InternAssignments.SingleOrDefaultAsync(x => x.ProfileId == input.ProfileId);
            if (assignment is null || (input.ProgramId.HasValue && assignment.ProgramId != input.ProgramId.Value))
                return Bad("Thực tập sinh chưa được phân công trong chương trình này.");
            if (!manager && assignment.MentorId != currentMentorId) return Results.Forbid();
        }

        task.Title = input.Title.Trim();
        task.Description = input.Description?.Trim() ?? "";
        task.DueDate = input.DueDate;
        task.Priority = string.IsNullOrWhiteSpace(input.Priority) ? "Normal" : input.Priority;
        task.ProfileId = input.ProfileId;
        task.ProgramId = input.ProgramId;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { task.Id });
    }
    static async Task<IResult> ListEvaluations(HttpContext c, CareerDbContext db)
    {
        var query = db.InternshipEvaluations.Include(x => x.Profile).Include(x => x.Program).AsNoTracking();
        if (c.User.IsInRole("Intern")) query = query.Where(x => x.ProfileId == Profile(c));
        else if (c.User.IsInRole("Mentor") && !Can(c, "profiles.read"))
        {
            var mentorId = await db.Mentors.Where(x => x.UserId == Account(c)).Select(x => x.Id).SingleOrDefaultAsync();
            if (mentorId == 0) return Results.Ok(Array.Empty<object>());
            query = query.Where(x => db.InternAssignments.Any(a => a.MentorId == mentorId && a.ProfileId == x.ProfileId && (x.ProgramId == null || a.ProgramId == x.ProgramId)));
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var evaluations = await query.OrderByDescending(x => x.UpdatedAt).ToListAsync();
        return Results.Ok(evaluations.Select(x => new
        {
            x.Id, x.ProfileId, name = x.Profile!.Name, x.ProgramId, program = x.Program?.Name,
            x.Skills, x.Attitude, x.Communication, x.Teamwork, x.Comment,
            isLocked = x.IsLocked || x.Program is not null && x.Program.EndDate < today,
            x.UpdatedAt,
            average = Math.Round((x.Skills + x.Attitude + x.Communication + (x.Teamwork ?? 0)) / (x.Teamwork.HasValue ? 4d : 3d), 2)
        }));
    }
    static async Task<IResult> UpdateWeeklyReport(int id, WeeklyReportInput input, HttpContext c, CareerDbContext db)
    {
        if (!c.User.IsInRole("Intern")) return Results.Forbid();
        var report = await db.WeeklyReports.SingleOrDefaultAsync(x => x.Id == id && x.ProfileId == Profile(c));
        if (report is null) return Results.NotFound();
        if (report.Status == "Reviewed") return Bad("Báo cáo đã được mentor phản hồi, không thể sửa trực tiếp.");
        if (input.WeekStart != report.WeekStart && await db.WeeklyReports.AnyAsync(x => x.ProfileId == report.ProfileId && x.WeekStart == input.WeekStart && x.Id != id))
            return Results.Conflict(new { message = "Tuần này đã có báo cáo. Hãy mở báo cáo đó để cập nhật." });

        report.WeekStart = input.WeekStart;
        report.Summary = input.Summary.Trim();
        report.Blockers = input.Blockers?.Trim();
        report.EvidenceUrl = input.EvidenceUrl?.Trim();
        report.Status = "Submitted";
        report.SubmittedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(ReportDto(await db.WeeklyReports.Include(x => x.Profile).SingleAsync(x => x.Id == report.Id)));
    }
    static async Task<IResult> SaveEvaluation(EvaluationInput input, HttpContext c, CareerDbContext db)
    {
        if (!c.User.IsInRole("Mentor") || (!Can(c, "evaluations.manage") && !Can(c, "assignments.manage"))) return Results.Forbid();
        if (input.Teamwork is < 1 or > 5) return Bad("Điểm làm việc nhóm phải từ 1 đến 5.");

        var mentor = await db.Mentors.SingleOrDefaultAsync(x => x.UserId == Account(c));
        if (mentor is null) return Bad("Tài khoản chưa được cấu hình mentor.");
        var assignment = await db.InternAssignments.SingleOrDefaultAsync(x => x.MentorId == mentor.Id && x.ProfileId == input.ProfileId);
        if (assignment is null || (input.ProgramId.HasValue && assignment.ProgramId != input.ProgramId.Value)) return Results.Forbid();
        var profile = await db.InternProfiles.SingleOrDefaultAsync(x => x.Id == input.ProfileId);
        if (profile is null) return Results.NotFound();
        var programId = input.ProgramId ?? assignment.ProgramId;
        var program = await db.InternshipPrograms.SingleOrDefaultAsync(x => x.Id == programId);
        if (program is null) return Bad("Chương trình thực tập không còn tồn tại.");
        if (program.EndDate < DateOnly.FromDateTime(DateTime.Now)) return Bad("Kỳ thực tập đã kết thúc, đánh giá không thể chỉnh sửa.");

        // The database permits one evaluation per intern and program. A reassigned
        // intern keeps the existing evaluation, now owned by the current mentor.
        var evaluation = await db.InternshipEvaluations.SingleOrDefaultAsync(x => x.ProfileId == input.ProfileId && x.ProgramId == programId);
        evaluation ??= await db.InternshipEvaluations.Where(x => x.ProfileId == input.ProfileId && x.ProgramId == null)
            .OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync();
        if (evaluation?.IsLocked == true) return Bad("Đánh giá đã khóa sau khi kết thúc kỳ thực tập.");
        if (evaluation is null)
        {
            evaluation = new InternshipEvaluation { MentorId = mentor.Id, ProfileId = input.ProfileId, ProgramId = programId };
            db.InternshipEvaluations.Add(evaluation);
        }
        evaluation.MentorId = mentor.Id;
        evaluation.ProgramId = programId;
        evaluation.Skills = input.Skills;
        evaluation.Attitude = input.Attitude;
        evaluation.Communication = input.Communication;
        evaluation.Teamwork = input.Teamwork;
        evaluation.Comment = input.Comment?.Trim() ?? "";
        evaluation.UpdatedAt = DateTimeOffset.UtcNow;
        db.PortalNotifications.Add(new() { ProfileId = profile.Id, Title = "Đã có đánh giá mới", Message = "Mentor đã cập nhật đánh giá thực tập của bạn.", Link = "attendance.html" });
        db.MailJobs.Add(new() { EventKey = $"evaluation:{profile.Id}:{programId}:{evaluation.UpdatedAt.Ticks}", Recipient = profile.Email, Subject = "Đánh giá thực tập đã được cập nhật", Body = $"Xin chào {profile.Name},\nMentor đã cập nhật đánh giá thực tập của bạn. Vui lòng đăng nhập Career Portal để xem chi tiết." });
        await db.SaveChangesAsync();
        return Results.Ok(evaluation.Id);
    }
    public static void ConfigureSprint3(this ModelBuilder b)
    {
        b.Entity<InternshipTask>(e=>{e.ToTable("INTERNSHIP_TASK",t=>t.HasCheckConstraint("CK_Task_Progress","[Progress] >= 0 AND [Progress] <= 100"));e.Property(x=>x.Title).HasMaxLength(180);e.Property(x=>x.Description).HasMaxLength(3000);e.Property(x=>x.Priority).HasMaxLength(20);e.Property(x=>x.Status).HasMaxLength(20);e.HasIndex(x=>new{x.ProfileId,x.Status});e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.Mentor).WithMany().HasForeignKey(x=>x.MentorId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Program).WithMany().HasForeignKey(x=>x.ProgramId).OnDelete(DeleteBehavior.SetNull);});
        b.Entity<TaskProgressHistory>(e=>{e.ToTable("TASK_PROGRESS_HISTORY");e.Property(x=>x.Status).HasMaxLength(20);e.Property(x=>x.Note).HasMaxLength(1000);e.HasIndex(x=>new{x.TaskId,x.ChangedAt});e.HasOne(x=>x.Task).WithMany().HasForeignKey(x=>x.TaskId).OnDelete(DeleteBehavior.Cascade);});
        b.Entity<WeeklyReport>(e=>{e.ToTable("WEEKLY_REPORT",t=>t.HasCheckConstraint("CK_Report_Status","[Status] IN ('Submitted','Reviewed','ChangesRequested')"));e.Property(x=>x.Summary).HasMaxLength(6000);e.Property(x=>x.Blockers).HasMaxLength(2000);e.Property(x=>x.EvidenceUrl).HasMaxLength(500);e.Property(x=>x.Status).HasMaxLength(30);e.Property(x=>x.MentorFeedback).HasMaxLength(2000);e.HasIndex(x=>new{x.ProfileId,x.WeekStart}).IsUnique();e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);});
        b.Entity<PortalNotification>(e=>{e.ToTable("PORTAL_NOTIFICATION");e.Property(x=>x.Title).HasMaxLength(180);e.Property(x=>x.Message).HasMaxLength(1000);e.Property(x=>x.Link).HasMaxLength(300);e.HasIndex(x=>new{x.AccountId,x.IsRead,x.CreatedAt});});
        b.Entity<InternshipEvaluation>(e=>{e.ToTable("INTERNSHIP_EVALUATION",t=>t.HasCheckConstraint("CK_Evaluation_Scores","[Skills] BETWEEN 1 AND 5 AND [Attitude] BETWEEN 1 AND 5 AND [Communication] BETWEEN 1 AND 5"));e.Property(x=>x.Comment).HasMaxLength(3000);e.HasIndex(x=>new{x.ProfileId,x.ProgramId}).IsUnique();e.HasOne(x=>x.Mentor).WithMany().HasForeignKey(x=>x.MentorId).OnDelete(DeleteBehavior.Restrict);e.HasOne(x=>x.Profile).WithMany().HasForeignKey(x=>x.ProfileId).OnDelete(DeleteBehavior.Cascade);e.HasOne(x=>x.Program).WithMany().HasForeignKey(x=>x.ProgramId).OnDelete(DeleteBehavior.SetNull);});
    }
    public static void MapSprint3(this WebApplication app)
    {
        app.MapGet("/api/tasks", ListTasks);
        app.MapPost("/api/tasks",async(TaskInput input,HttpContext c,CareerDbContext db)=>{if(!c.User.IsInRole("Mentor")||(!Can(c,"tasks.manage")&&!Can(c,"assignments.manage")))return Results.Forbid();var mentor=await db.Mentors.SingleOrDefaultAsync(x=>x.UserId==Account(c));if(mentor is null)return Bad("Tài khoản chưa được cấu hình mentor.");if(input.DueDate is DateOnly d&&d<DateOnly.FromDateTime(DateTime.Now))return Bad("Deadline không được ở quá khứ.");var profile=await db.InternProfiles.SingleOrDefaultAsync(x=>x.Id==input.ProfileId);if(profile is null)return Results.NotFound();var assignment=await db.InternAssignments.AnyAsync(x=>x.ProfileId==input.ProfileId&&x.MentorId==mentor.Id&&(!input.ProgramId.HasValue||x.ProgramId==input.ProgramId));if(!assignment)return Results.BadRequest(new{message="Thực tập sinh chưa được phân công cho mentor này."});var t=new InternshipTask{Title=input.Title.Trim(),Description=input.Description?.Trim()??"",DueDate=input.DueDate,Priority=string.IsNullOrWhiteSpace(input.Priority)?"Normal":input.Priority,ProfileId=input.ProfileId,MentorId=mentor.Id,ProgramId=input.ProgramId};db.InternshipTasks.Add(t);db.PortalNotifications.Add(new(){ProfileId=input.ProfileId,Title="Bạn có nhiệm vụ mới",Message=$"Mentor đã giao nhiệm vụ: {t.Title}",Link="attendance.html"});db.MailJobs.Add(new(){EventKey=$"task:{t.Id}:{DateTimeOffset.UtcNow.Ticks}",Recipient=profile.Email,Subject="Bạn có nhiệm vụ mới",Body=$"Xin chào {profile.Name},\nMentor đã giao cho bạn nhiệm vụ: {t.Title}.\nVui lòng đăng nhập Career Portal để xem chi tiết và cập nhật tiến độ."});await db.SaveChangesAsync();return Results.Created($"/api/tasks/{t.Id}",TaskDto(await db.InternshipTasks.Include(x=>x.Profile).Include(x=>x.Mentor!).ThenInclude(x=>x.User).Include(x=>x.Program).SingleAsync(x=>x.Id==t.Id)));});
        app.MapPut("/api/tasks/{id:int}", UpdateTask);
        app.MapPatch("/api/tasks/{id:int}/progress",async(int id,TaskProgressInput input,HttpContext c,CareerDbContext db)=>{var t=await db.InternshipTasks.Include(x=>x.Mentor).SingleOrDefaultAsync(x=>x.Id==id);if(t is null)return Results.NotFound();if(!c.User.IsInRole("Intern")||t.ProfileId!=Profile(c))return Results.Forbid();var status=input.Progress==100?"Done":input.Progress>0?"InProgress":"Todo";if(!new[]{"Todo","InProgress","Done"}.Contains(input.Status))return Bad("Trạng thái không hợp lệ.");if(input.Status!=status)return Bad("Trạng thái phải phù hợp với phần trăm tiến độ.");t.Progress=input.Progress;t.Status=input.Status;t.UpdatedAt=DateTimeOffset.UtcNow;db.TaskProgressHistories.Add(new TaskProgressHistory{TaskId=t.Id,Progress=t.Progress,Status=t.Status,Note=input.Note,ChangedByUserId=Account(c)});await db.SaveChangesAsync();return Results.Ok(new{t.Id,t.Progress,t.Status});});
        app.MapGet("/api/tasks/{id:int}/history", TaskHistory);
        app.MapGet("/api/reports/weekly",async(HttpContext c,CareerDbContext db)=>{var q=db.WeeklyReports.Include(x=>x.Profile).AsNoTracking();if(c.User.IsInRole("Intern"))q=q.Where(x=>x.ProfileId==Profile(c));else if(!Can(c,"profiles.read")){var mentor=await db.Mentors.Where(x=>x.UserId==Account(c)).Select(x=>x.Id).SingleOrDefaultAsync();var ids=await db.InternAssignments.Where(x=>x.MentorId==mentor).Select(x=>x.ProfileId).ToListAsync();q=q.Where(x=>ids.Contains(x.ProfileId));}return Results.Ok((await q.OrderByDescending(x=>x.WeekStart).ToListAsync()).Select(ReportDto));});
        app.MapPost("/api/reports/weekly",async(WeeklyReportInput input,HttpContext c,CareerDbContext db)=>{if(!c.User.IsInRole("Intern"))return Results.Forbid();var id=Profile(c);if(await db.WeeklyReports.AnyAsync(x=>x.ProfileId==id&&x.WeekStart==input.WeekStart))return Results.Conflict(new{message="Tuần này đã có báo cáo. Hãy mở báo cáo để cập nhật."});var r=new WeeklyReport{ProfileId=id,WeekStart=input.WeekStart,Summary=input.Summary.Trim(),Blockers=input.Blockers?.Trim(),EvidenceUrl=input.EvidenceUrl?.Trim()};db.WeeklyReports.Add(r);var assignment=await db.InternAssignments.Include(x=>x.Mentor!).ThenInclude(x=>x.User).SingleOrDefaultAsync(x=>x.ProfileId==id);if(assignment?.Mentor?.User is not null){db.PortalNotifications.Add(new(){AccountId=assignment.Mentor.UserId,Title="Có báo cáo tuần mới",Message=$"Thực tập sinh đã nộp báo cáo tuần bắt đầu ngày {r.WeekStart:dd/MM/yyyy}.",Link="mentor.html"});db.MailJobs.Add(new(){EventKey=$"report-submitted:{id}:{r.WeekStart:yyyyMMdd}:{DateTimeOffset.UtcNow.Ticks}",Recipient=assignment.Mentor.User.Email,Subject="Có báo cáo tuần mới cần xem",Body=$"Bạn có báo cáo tuần mới từ thực tập sinh. Vui lòng đăng nhập Career Portal để xem và phản hồi."});}await db.SaveChangesAsync();return Results.Created($"/api/reports/weekly/{r.Id}",ReportDto(await db.WeeklyReports.Include(x=>x.Profile).SingleAsync(x=>x.Id==r.Id)));});
        app.MapPut("/api/reports/weekly/{id:int}", UpdateWeeklyReport);
        app.MapPatch("/api/reports/weekly/{id:int}/feedback",async(int id,WeeklyFeedbackInput input,HttpContext c,CareerDbContext db)=>{if(!Can(c,"reports.review")&&!Can(c,"assignments.manage"))return Results.Forbid();var r=await db.WeeklyReports.Include(x=>x.Profile).SingleOrDefaultAsync(x=>x.Id==id);if(r is null)return Results.NotFound();if(c.User.IsInRole("Mentor")){var mentor=await db.Mentors.Where(x=>x.UserId==Account(c)).Select(x=>x.Id).SingleOrDefaultAsync();if(!await db.InternAssignments.AnyAsync(x=>x.MentorId==mentor&&x.ProfileId==r.ProfileId))return Results.Forbid();}r.MentorFeedback=input.Feedback.Trim();r.Status="Reviewed";r.ReviewedAt=DateTimeOffset.UtcNow;r.ReviewedBy=c.User.Identity?.Name;db.PortalNotifications.Add(new(){ProfileId=r.ProfileId,Title="Báo cáo tuần đã được phản hồi",Message="Mentor đã gửi phản hồi cho báo cáo tuần của bạn.",Link="attendance.html"});db.MailJobs.Add(new(){EventKey=$"report-feedback:{r.Id}:{r.ReviewedAt.Value.Ticks}",Recipient=r.Profile!.Email,Subject="Phản hồi báo cáo tuần",Body=$"Xin chào {r.Profile.Name},\nMentor đã phản hồi báo cáo tuần bắt đầu ngày {r.WeekStart:dd/MM/yyyy}:\n{r.MentorFeedback}\nVui lòng đăng nhập Career Portal để xem đầy đủ."});await db.SaveChangesAsync();return Results.Ok(ReportDto(r));});
        app.MapGet("/api/notifications",async(HttpContext c,CareerDbContext db)=>{var aid=Account(c);var pid=NotificationProfile(c);var rows=await db.PortalNotifications.AsNoTracking().Where(x=>x.AccountId==aid||(pid!=0&&x.ProfileId==pid)).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync();return Results.Ok(new{items=rows,unread=rows.Count(x=>!x.IsRead)});});
        app.MapPatch("/api/notifications/{id:int}/read",async(int id,HttpContext c,CareerDbContext db)=>{var aid=Account(c);var pid=NotificationProfile(c);var n=await db.PortalNotifications.SingleOrDefaultAsync(x=>x.Id==id&&(x.AccountId==aid||(pid!=0&&x.ProfileId==pid)));if(n is null)return Results.NotFound();n.IsRead=true;await db.SaveChangesAsync();return Results.Ok();});
        app.MapPost("/api/notifications/read-all",async(HttpContext c,CareerDbContext db)=>{var aid=Account(c);var pid=NotificationProfile(c);await db.PortalNotifications.Where(x=>x.AccountId==aid||(pid!=0&&x.ProfileId==pid)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsRead,true));return Results.Ok();});
        app.MapGet("/api/evaluations", ListEvaluations);
        app.MapPost("/api/evaluations", SaveEvaluation);
        app.MapGet("/api/evaluations/summary",async(HttpContext c,CareerDbContext db)=>{if(!Can(c,"profiles.read"))return Results.Forbid();var rows=await db.InternshipEvaluations.Include(x=>x.Profile).Include(x=>x.Program).AsNoTracking().ToListAsync();return Results.Ok(rows.GroupBy(x=>new{x.ProgramId,Program=x.Program?.Name}).Select(g=>new{g.Key.ProgramId,g.Key.Program,count=g.Count(),average=Math.Round(g.Average(x=>(x.Skills+x.Attitude+x.Communication+(x.Teamwork??0))/(x.Teamwork.HasValue?4d:3d)),2),items=g.Select(x=>new{ x.ProfileId,name=x.Profile!.Name,x.Skills,x.Attitude,x.Communication,x.Teamwork,x.Comment})}));});
    }
}

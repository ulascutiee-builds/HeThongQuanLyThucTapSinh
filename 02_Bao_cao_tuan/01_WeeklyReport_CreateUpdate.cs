// Source: backend/CareerPortal.Api/Sprint3Endpoints.cs | lines 106-123 | Excel row(s): 14, 15
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

// Source: backend/CareerPortal.Api/Sprint3Endpoints.cs | lines 180-181 | Excel row(s): 14, 15
        app.MapPost("/api/reports/weekly",async(WeeklyReportInput input,HttpContext c,CareerDbContext db)=>{if(!c.User.IsInRole("Intern"))return Results.Forbid();var id=Profile(c);if(await db.WeeklyReports.AnyAsync(x=>x.ProfileId==id&&x.WeekStart==input.WeekStart))return Results.Conflict(new{message="Tuần này đã có báo cáo. Hãy mở báo cáo để cập nhật."});var r=new WeeklyReport{ProfileId=id,WeekStart=input.WeekStart,Summary=input.Summary.Trim(),Blockers=input.Blockers?.Trim(),EvidenceUrl=input.EvidenceUrl?.Trim()};db.WeeklyReports.Add(r);var assignment=await db.InternAssignments.Include(x=>x.Mentor!).ThenInclude(x=>x.User).SingleOrDefaultAsync(x=>x.ProfileId==id);if(assignment?.Mentor?.User is not null){db.PortalNotifications.Add(new(){AccountId=assignment.Mentor.UserId,Title="Có báo cáo tuần mới",Message=$"Thực tập sinh đã nộp báo cáo tuần bắt đầu ngày {r.WeekStart:dd/MM/yyyy}.",Link="mentor.html"});db.MailJobs.Add(new(){EventKey=$"report-submitted:{id}:{r.WeekStart:yyyyMMdd}:{DateTimeOffset.UtcNow.Ticks}",Recipient=assignment.Mentor.User.Email,Subject="Có báo cáo tuần mới cần xem",Body=$"Bạn có báo cáo tuần mới từ thực tập sinh. Vui lòng đăng nhập Career Portal để xem và phản hồi."});}await db.SaveChangesAsync();return Results.Created($"/api/reports/weekly/{r.Id}",ReportDto(await db.WeeklyReports.Include(x=>x.Profile).SingleAsync(x=>x.Id==r.Id)));});
        app.MapPut("/api/reports/weekly/{id:int}", UpdateWeeklyReport);


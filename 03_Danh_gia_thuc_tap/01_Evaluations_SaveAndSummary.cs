// Source: backend/CareerPortal.Api/Sprint3Endpoints.cs | lines 124-163 | Excel row(s): 23, 28
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

// Source: backend/CareerPortal.Api/Sprint3Endpoints.cs | lines 187-188 | Excel row(s): 23, 28
        app.MapPost("/api/evaluations", SaveEvaluation);
        app.MapGet("/api/evaluations/summary",async(HttpContext c,CareerDbContext db)=>{if(!Can(c,"profiles.read"))return Results.Forbid();var rows=await db.InternshipEvaluations.Include(x=>x.Profile).Include(x=>x.Program).AsNoTracking().ToListAsync();return Results.Ok(rows.GroupBy(x=>new{x.ProgramId,Program=x.Program?.Name}).Select(g=>new{g.Key.ProgramId,g.Key.Program,count=g.Count(),average=Math.Round(g.Average(x=>(x.Skills+x.Attitude+x.Communication+(x.Teamwork??0))/(x.Teamwork.HasValue?4d:3d)),2),items=g.Select(x=>new{ x.ProfileId,name=x.Profile!.Name,x.Skills,x.Attitude,x.Communication,x.Teamwork,x.Comment})}));});


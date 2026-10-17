// Source: backend/CareerPortal.Api/Sprint3Endpoints.cs | lines 159-162 | Excel row(s): 59
        db.PortalNotifications.Add(new() { ProfileId = profile.Id, Title = "Đã có đánh giá mới", Message = "Mentor đã cập nhật đánh giá thực tập của bạn.", Link = "attendance.html" });
        db.MailJobs.Add(new() { EventKey = $"evaluation:{profile.Id}:{programId}:{evaluation.UpdatedAt.Ticks}", Recipient = profile.Email, Subject = "Đánh giá thực tập đã được cập nhật", Body = $"Xin chào {profile.Name},\nMentor đã cập nhật đánh giá thực tập của bạn. Vui lòng đăng nhập Career Portal để xem chi tiết." });
        await db.SaveChangesAsync();
        return Results.Ok(evaluation.Id);


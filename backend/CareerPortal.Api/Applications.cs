using Microsoft.EntityFrameworkCore;
public static class Applications
{
    public static void MapApplications(this WebApplication app)
    {
        app.MapPost("/api/interns/{id:int}/applications", Submit);
        app.MapPost("/api/interns/{id:int}/apply", Submit);
        app.MapPatch("/api/hr/applications/{id:int}", async(int id,DecideStatusRequest input,HttpContext c,CareerDbContext db)=>{
            if(input.Status is not("Đã duyệt" or "Từ chối")||(input.Status=="Từ chối"&&string.IsNullOrWhiteSpace(input.Note)))return Results.BadRequest(new{message="Nhập trạng thái hợp lệ và lý do từ chối."});
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var a=await db.InternApplications.Include(x=>x.Profile).SingleOrDefaultAsync(x=>x.Id==id);if(a is null)return Results.NotFound();if(a.Status!="Chờ duyệt")return Results.Conflict(new{message="Hồ sơ không còn chờ duyệt."});
            a.Status=input.Status;a.Note=input.Note?.Trim();a.ReviewedAt=DateTimeOffset.UtcNow;a.ReviewedBy=c.User.Identity!.Name;
            a.Profile!.Status=input.Status=="Đã duyệt"?"Đang thực tập":"Từ chối";
            if(input.Status=="Đã duyệt")
            {
                var applicationDocuments=await db.InternDocuments.Where(x=>x.ProfileId==a.ProfileId&&x.IsCurrent&&(x.Type=="CV"||x.Type=="Đơn xin thực tập")&&x.Status=="Chờ duyệt").ToListAsync();
                foreach(var applicationDocument in applicationDocuments)
                {
                    applicationDocument.Status="Đã duyệt";
                    applicationDocument.ReviewedBy=a.ReviewedBy;
                    applicationDocument.ReviewedAt=a.ReviewedAt;
                    db.InternReviewHistories.Add(new(){ProfileId=a.ProfileId,TargetType="Tài liệu",TargetId=applicationDocument.Id,TargetLabel=applicationDocument.Type,Status=applicationDocument.Status,ReviewedBy=applicationDocument.ReviewedBy!});
                }
            }
            db.InternReviewHistories.Add(new(){ProfileId=a.ProfileId,TargetType="Hồ sơ",TargetId=a.Id,TargetLabel="Hồ sơ đăng ký",Status=a.Status,Note=a.Note,ReviewedBy=a.ReviewedBy!});
            db.MailJobs.Add(new(){EventKey=$"decision:{a.Id}:{a.SubmissionVersion}",Recipient=a.Profile.Email,Subject="Kết quả xét duyệt hồ sơ thực tập",Body=$"Xin chào {a.Profile.Name},\nHồ sơ thực tập của bạn: {a.Status}.\n"+(a.Status=="Từ chối"?$"Lý do: {a.Note}\nBạn có thể bổ sung tài liệu và nộp lại.":"Vui lòng đăng nhập để xem thông tin và hợp đồng thực tập.")});
            await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(a.ToResponse());
        });
        app.MapGet("/api/hr/email-status",async(CareerDbContext db)=>Results.Ok(await db.MailJobs.OrderByDescending(x=>x.Id).Select(x=>new{x.Id,x.EventKey,x.Recipient,x.Status,x.Attempts,x.LastError,x.SentAt}).Take(100).ToListAsync()));
    }
    static async Task<IResult> Submit(int id,HttpContext c,CareerDbContext db)
    {
        if(!c.User.IsInRole("Intern")||SprintSecurity.Id(c)!=id)return Results.Forbid();
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var p=await db.InternProfiles.Include(x=>x.Application).Include(x=>x.Documents).SingleOrDefaultAsync(x=>x.Id==id);if(p is null)return Results.NotFound();
        if(!p.EmailVerified)return Results.BadRequest(new{message="Cần xác thực email trước khi nộp hồ sơ."});
        if(p.Application?.Status is "Chờ duyệt" or "Đã duyệt")return Results.Conflict(new{message="Hồ sơ đã được nộp."});
        if(!new[]{"CV","Đơn xin thực tập"}.All(t=>p.Documents.Any(d=>d.Type==t&&d.IsCurrent&&d.Status!="Từ chối")))return Results.BadRequest(new{message="Cần CV và đơn xin thực tập hợp lệ trước khi nộp."});
        if(p.Application is null)p.Application=new(){ProfileId=id};else{p.Application.SubmissionVersion++;p.Application.Status="Chờ duyệt";p.Application.Note=null;p.Application.ReviewedAt=null;p.Application.ReviewedBy=null;p.Application.AppliedAt=DateTimeOffset.UtcNow;}
        p.Status="Chờ duyệt";await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(p.Application.ToResponse());
    }
}

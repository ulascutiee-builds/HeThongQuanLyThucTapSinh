using Microsoft.EntityFrameworkCore;
public static class Documents
{
    public static void MapDocuments(this WebApplication app)
    {
        app.MapPost("/api/interns/{id:int}/documents",(int id,HttpContext c,CareerDbContext db)=>Upload(id,false,c,db));
        app.MapPost("/api/hr/profiles/{id:int}/contract",(int id,HttpContext c,CareerDbContext db)=>Upload(id,true,c,db));
        app.MapGet("/api/interns/{id:int}/documents",async(int id,HttpContext c,CareerDbContext db)=>{
            if(!SprintSecurity.Own(c,id))return Results.Forbid();if(!await db.InternProfiles.AnyAsync(x=>x.Id==id))return Results.NotFound();
            var list=await db.InternDocuments.Where(x=>x.ProfileId==id).OrderByDescending(x=>x.Version).AsNoTracking().ToListAsync();return Results.Ok(list.Select(x=>x.ToResponse()));
        });
        app.MapGet("/api/documents/{id:int}/file",async(int id,HttpContext c,CareerDbContext db)=>{
            var d=await db.InternDocuments.FindAsync(id);if(d is null)return Results.NotFound();if(!SprintSecurity.Own(c,d.ProfileId))return Results.Forbid();
            c.Response.Headers.XContentTypeOptions="nosniff";
            return Results.File(d.Content,d.ContentType,d.FileName);
        });
        app.MapGet("/api/documents/{id:int}/preview",async(int id,HttpContext c,CareerDbContext db)=>{
            var d=await db.InternDocuments.FindAsync(id);if(d is null)return Results.NotFound();if(!SprintSecurity.Own(c,d.ProfileId))return Results.Forbid();
            if(d.ContentType!="application/pdf")return Results.BadRequest(new{message="DOCX được xem bằng chức năng tải xuống."});c.Response.Headers.XContentTypeOptions="nosniff";c.Response.Headers.ContentSecurityPolicy="sandbox";return Results.File(d.Content,"application/pdf");
        });
        app.MapPatch("/api/hr/documents/{id:int}",async(int id,DecideStatusRequest input,HttpContext c,CareerDbContext db)=>{
            if(input.Status is not("Đã duyệt" or "Từ chối")||(input.Status=="Từ chối"&&string.IsNullOrWhiteSpace(input.Note)))return Results.BadRequest(new{message="Chọn trạng thái hợp lệ và nhập lý do từ chối."});
            await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var d=await db.InternDocuments.FindAsync(id);if(d is null)return Results.NotFound();if(!d.IsCurrent||d.Status!="Chờ duyệt"||d.Type=="Hợp đồng thực tập")return Results.Conflict(new{message="Tài liệu không còn chờ duyệt."});
            d.Status=input.Status;d.Note=input.Note?.Trim();d.ReviewedBy=c.User.Identity!.Name;d.ReviewedAt=DateTimeOffset.UtcNow;
            db.InternReviewHistories.Add(new InternReviewHistory{ProfileId=d.ProfileId,TargetType="Tài liệu",TargetId=d.Id,TargetLabel=d.Type,Status=d.Status,Note=d.Note,ReviewedBy=d.ReviewedBy!});await db.SaveChangesAsync();await tx.CommitAsync();return Results.Ok(d.ToResponse());
        });
        app.MapPost("/api/contracts/{id:int}/confirm",(int id,HttpContext c,CareerDbContext db)=>Confirm(id,null,c,db));
        app.MapPost("/api/interns/{profileId:int}/documents/{id:int}/confirm-contract",(int profileId,int id,HttpContext c,CareerDbContext db)=>Confirm(id,profileId,c,db));
    }
    static async Task<IResult> Upload(int id,bool contract,HttpContext c,CareerDbContext db)
    {
        if(contract?!SprintSecurity.HR(c):!SprintSecurity.Own(c,id))return Results.Forbid();var p=await db.InternProfiles.FindAsync(id);if(p is null)return Results.NotFound();
        var type=contract?"Hợp đồng thực tập":c.Request.Query["type"].ToString();if(!contract&&type is not("CV" or "Đơn xin thực tập"))return Results.BadRequest(new{message="Loại tài liệu không hợp lệ."});
        if(!contract&&await db.InternApplications.AnyAsync(x=>x.ProfileId==id&&(x.Status=="Chờ duyệt"||x.Status=="Đã duyệt")))return Results.Conflict(new{message="Hồ sơ đã nộp. Không thay tài liệu khi đang chờ duyệt hoặc đã duyệt."});
        DateTimeOffset? expires=null;if(contract){expires=DateTimeOffset.UtcNow.AddDays(90);if(c.Request.Query.TryGetValue("expiresAt",out var value)){if(!DateTimeOffset.TryParse(value,out var date)||date<=DateTimeOffset.UtcNow)return Results.BadRequest(new{message="Hạn hợp đồng phải nằm trong tương lai."});expires=date;}}
        var file=await UploadValidator.Read(c.Request);if(file.Error!=null)return Results.BadRequest(new{message=file.Error});
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var old=await db.InternDocuments.Where(x=>x.ProfileId==id&&x.Type==type).ToListAsync();foreach(var d in old)d.IsCurrent=false;
        var row=new InternDocument{ProfileId=id,Type=type,FileName=file.Name,ContentType=file.Mime,Content=file.Data!,Status=contract?"Chờ xác nhận":"Chờ duyệt",Version=old.Select(x=>x.Version).DefaultIfEmpty(0).Max()+1,UploadedBy=c.User.Identity!.Name??"",ExpiresAt=expires};
        db.InternDocuments.Add(row);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/documents/{row.Id}/file",row.ToResponse());
    }
    static async Task<IResult> Confirm(int id,int? profileId,HttpContext c,CareerDbContext db)
    {
        if(!c.User.IsInRole("Intern"))return Results.Forbid();var row=await db.InternDocuments.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);
        if(row is null)return Results.NotFound();if(row.ProfileId!=SprintSecurity.Id(c)||(profileId!=null&&profileId!=row.ProfileId))return Results.Forbid();
        var now=DateTimeOffset.UtcNow;
        var count=await db.InternDocuments.Where(x=>x.Id==id&&x.Type=="Hợp đồng thực tập"&&x.IsCurrent&&x.Status=="Chờ xác nhận"&&(x.ExpiresAt==null||x.ExpiresAt>now)).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Đã xác nhận").SetProperty(x=>x.ConfirmedAt,now));
        if(count==0)return Results.Conflict(new{message="Hợp đồng đã xác nhận, hết hạn hoặc đã được thay bằng phiên bản mới."});
        var updated=await db.InternDocuments.AsNoTracking().SingleAsync(x=>x.Id==id);return Results.Ok(updated.ToResponse());
    }
}

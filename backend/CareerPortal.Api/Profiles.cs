using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
public static class Profiles
{
    public static string? NormalizePhone(string? phone)
    {
        if(string.IsNullOrWhiteSpace(phone))return null;
        var digits=phone.Trim();
        if(digits.StartsWith('+'))digits=digits[1..];
        if(digits.StartsWith("84",StringComparison.Ordinal)&&digits.Length==11)return "0"+digits[2..];
        return digits;
    }
    public static Task<bool> PhoneExistsAsync(CareerDbContext db,string? phone,int? exceptId=null)
    {
        if(phone is null)return Task.FromResult(false);
        var candidates=new List<string>{phone,"+"+phone};
        if(phone.Length==10&&phone[0]=='0')
        {
            candidates.Add("84"+phone[1..]);
            candidates.Add("+84"+phone[1..]);
        }
        IQueryable<InternProfile> profiles=db.InternProfiles;
        if(exceptId is int id)profiles=profiles.Where(x=>x.Id!=id);
        return profiles.AnyAsync(x=>x.Phone!=null&&candidates.Contains(x.Phone));
    }
    public static readonly string[] Statuses=["Chờ hồ sơ","Chờ duyệt","Đang thực tập","Chờ bắt đầu","Đã hoàn thành","Từ chối"];
    public static void MapProfiles(this WebApplication app)
    {
        app.MapPost("/api/interns",Create);app.MapPost("/api/hr/profiles",Create);
        app.MapPut("/api/hr/profiles/{id:int}",UpdateHR);
        app.MapPut("/api/interns/{id:int}",async(int id,UpdateInternProfileRequest input,HttpContext c,CareerDbContext db,IConfiguration config)=>{
            if(!SprintSecurity.Own(c,id))return Results.Forbid();var p=await db.InternProfiles.FindAsync(id);if(p is null)return Results.NotFound();
            if(input.DateOfBirth is DateOnly birthDate&&birthDate>DateOnly.FromDateTime(DateTime.Today))return Results.ValidationProblem(new Dictionary<string,string[]>{{"DateOfBirth",["Ngày sinh không hợp lệ."]}});
            var email=input.Email.Trim().ToLowerInvariant();var phone=NormalizePhone(input.Phone);
            if(await db.InternProfiles.AnyAsync(x=>x.Id!=id&&x.Email==email)||await PhoneExistsAsync(db,phone,id))return Results.Conflict(new{message="Email hoặc số điện thoại đã tồn tại."});
            if(p.Email!=email){p.Email=email;p.EmailVerified=false;AuthEndpoints.Verification(db,p,config);}
            p.Name=input.Name.Trim();p.Phone=phone;p.DateOfBirth=input.DateOfBirth;p.School=input.School.Trim();p.Major=input.Major.Trim();await db.SaveChangesAsync();return Results.Ok(p.ToResponse());
        });
        app.MapGet("/api/interns",async(HttpContext c,CareerDbContext db,string? search,string? school,string? major,string? status,int page=1,int pageSize=5)=>{
            if(!SprintSecurity.HR(c))return Results.Forbid();if(page<1||page>10000000||pageSize<1||pageSize>100)return Results.BadRequest(new{message="Phân trang không hợp lệ."});
            var q=db.InternProfiles.AsNoTracking();
            if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim();q=q.Where(x=>x.Name.Contains(s)||x.StudentId.Contains(s)||x.Email.Contains(s)||x.School.Contains(s)||x.Major.Contains(s));}
            if(!string.IsNullOrWhiteSpace(school))q=q.Where(x=>x.School==school);if(!string.IsNullOrWhiteSpace(major))q=q.Where(x=>x.Major==major);if(!string.IsNullOrWhiteSpace(status))q=q.Where(x=>x.Status==status);
            var total=await q.CountAsync();var rows=await q.OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
            return Results.Ok(new{items=rows.Select(x=>x.ToResponse()),total,page,pageSize});
        });
        app.MapGet("/api/interns/{id:int}/workspace",async(int id,HttpContext c,CareerDbContext db)=>{
            if(!SprintSecurity.Own(c,id))return Results.Forbid();var p=await db.InternProfiles.Include(x=>x.Application).Include(x=>x.Documents).AsNoTracking().FirstOrDefaultAsync(x=>x.Id==id);if(p is null)return Results.NotFound();
            var history=await db.InternReviewHistories.Where(x=>x.ProfileId==id).OrderByDescending(x=>x.ReviewedAt).ToListAsync();
            return Results.Ok(new InternWorkspaceResponse(p.ToResponse(),p.Application?.ToResponse(),p.Documents.Where(x=>x.IsCurrent).OrderByDescending(x=>x.UploadedAt).Select(x=>x.ToResponse()).ToArray(),history.Select(x=>x.ToResponse()).ToArray()));
        });
        app.MapGet("/api/hr/dashboard",async(CareerDbContext db)=>{
            var p=await db.InternProfiles.AsNoTracking().OrderByDescending(x=>x.CreatedAt).ToListAsync();var a=await db.InternApplications.AsNoTracking().ToListAsync();
            var d=await db.InternDocuments.AsNoTracking().OrderByDescending(x=>x.UploadedAt).ToListAsync();var h=await db.InternReviewHistories.AsNoTracking().OrderByDescending(x=>x.ReviewedAt).ToListAsync();
            return Results.Ok(new HrDashboardResponse(p.Select(x=>x.ToResponse()).ToArray(),a.Select(x=>x.ToResponse()).ToArray(),d.Select(x=>x.ToResponse()).ToArray(),h.Select(x=>x.ToResponse()).ToArray()));
        });
    }
    static async Task<IResult> Create(CreateHrProfileRequest input,HttpContext c,CareerDbContext db,IPasswordHasher<InternProfile> hasher)
    {
        if(!SprintSecurity.HR(c))return Results.Forbid();if(input.EndDate<input.StartDate||!Statuses.Contains(input.Status))return Results.BadRequest(new{message="Ngày hoặc trạng thái hồ sơ không hợp lệ."});
        var email=input.Email.Trim().ToLowerInvariant();var sid=input.StudentId.Trim();var phone=NormalizePhone(input.Phone);
        if(await db.InternProfiles.AnyAsync(x=>x.Email==email)||await PhoneExistsAsync(db,phone))return Results.Conflict(new{message="Email hoặc số điện thoại đã tồn tại."});
        var p=new InternProfile{Name=input.Name.Trim(),Email=email,StudentId=sid,Phone=phone,School=input.School.Trim(),Major=input.Major.Trim(),StartDate=input.StartDate,EndDate=input.EndDate,Status=input.Status,PasswordHash=""};
        p.PasswordHash=hasher.HashPassword(p,Guid.NewGuid().ToString("N"));db.InternProfiles.Add(p);await db.SaveChangesAsync();return Results.Created($"/api/interns/{p.Id}/workspace",p.ToResponse());
    }
    static async Task<IResult> UpdateHR(int id,UpdateHrProfileRequest input,HttpContext c,CareerDbContext db,IConfiguration config)
    {
        if(!SprintSecurity.HR(c))return Results.Forbid();if(input.EndDate<input.StartDate||!Statuses.Contains(input.Status))return Results.BadRequest(new{message="Ngày hoặc trạng thái hồ sơ không hợp lệ."});
        var p=await db.InternProfiles.FindAsync(id);if(p is null)return Results.NotFound();var email=input.Email.Trim().ToLowerInvariant();var phone=NormalizePhone(input.Phone);
        if(await db.InternProfiles.AnyAsync(x=>x.Id!=id&&x.Email==email)||await PhoneExistsAsync(db,phone,id))return Results.Conflict(new{message="Email hoặc số điện thoại đã tồn tại."});
        if(p.Email!=email){p.Email=email;p.EmailVerified=false;AuthEndpoints.Verification(db,p,config);}
        p.Name=input.Name.Trim();p.Phone=phone;p.School=input.School.Trim();p.Major=input.Major.Trim();p.StartDate=input.StartDate;p.EndDate=input.EndDate;
        // Decisions must use the review API so every decision has a reason/history/email.
        if(p.Status!=input.Status && input.Status is "Từ chối" or "Đang thực tập")return Results.BadRequest(new{message="Duyệt/từ chối qua màn hình Đơn xin để ghi lịch sử và gửi email."});
        p.Status=input.Status;await db.SaveChangesAsync();return Results.Ok(p.ToResponse());
    }
}

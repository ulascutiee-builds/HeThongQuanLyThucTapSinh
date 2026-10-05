using System.Security.Cryptography;
using System.Text;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
public static class AuthEndpoints
{
    static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static bool RequireEmailVerification(IConfiguration config)=>config.GetValue("Sprint1:RequireEmailVerification",false);
    public static void Verification(CareerDbContext db,InternProfile p,IConfiguration config)
    {
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));p.VerificationHash=Hash(token);p.VerificationExpires=DateTimeOffset.UtcNow.AddHours(24);
        var verifyUrl=$"{(config["PublicUrl"]??"http://localhost:5135").TrimEnd('/')}/dang-nhap.html?verify={Uri.EscapeDataString(token)}&profileId={p.Id}";
        var safeName=WebUtility.HtmlEncode(p.Name);
        var safeUrl=WebUtility.HtmlEncode(verifyUrl);
        db.MailJobs.Add(new MailJob
        {
            EventKey="verify:"+Guid.NewGuid(),
            Recipient=p.Email,
            Subject="Xác thực tài khoản thực tập",
            IsHtml=true,
            Body=$"""<html><body style="font-family:Arial,sans-serif;color:#202e29"><p>Xin chào {safeName},</p><p>Nhấn nút bên dưới trong vòng 24 giờ để xác thực email:</p><p><a href="{safeUrl}" style="display:inline-block;padding:12px 20px;background:#17473b;color:#fff;text-decoration:none;border-radius:6px">Xác thực email</a></p></body></html>"""
        });
    }
    public static void MapSprintAuth(this WebApplication app)
    {
        app.MapPost("/api/auth/login",async(LoginInternRequest input,HttpContext c,IConfiguration config)=>{
            var password=config["Sprint1:HrPassword"];
            if(string.IsNullOrEmpty(password)||!string.Equals(input.Identity.Trim(),config["Sprint1:HrEmail"],StringComparison.OrdinalIgnoreCase)||!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(input.Password)),SHA256.HashData(Encoding.UTF8.GetBytes(password))))return Results.Unauthorized();
            await SprintSecurity.SignIn(c,0,"HR","Nhân sự Sprint 1");return Results.Ok(new{id=0,name="Nhân sự Sprint 1",role="HR"});
        });
        app.MapGet("/api/auth/me",(HttpContext c)=>Results.Ok(new{id=SprintSecurity.Id(c),name=c.User.Identity!.Name,role=SprintSecurity.HR(c)?"HR":"Intern"}));
        app.MapPost("/api/auth/logout",async(HttpContext c)=>{await c.SignOutAsync("Sprint1");return Results.Ok();});
        app.MapPost("/api/interns/register",async(RegisterInternRequest input,CareerDbContext db,IPasswordHasher<InternProfile> hasher,IConfiguration config)=>{
            var email=input.Email.Trim().ToLowerInvariant();var student=input.StudentId.Trim();var phone=Profiles.NormalizePhone(input.Phone);
            if(input.DateOfBirth is DateOnly birthDate&&birthDate>DateOnly.FromDateTime(DateTime.Today))return Results.ValidationProblem(new Dictionary<string,string[]>{{"DateOfBirth",["Ngày sinh không hợp lệ."]}});
            var errors=new Dictionary<string,string[]>();
            if(await db.InternProfiles.AnyAsync(x=>x.Email==email))errors["Email"]=["Email này đã được sử dụng."];
            if(await Profiles.PhoneExistsAsync(db,phone))errors["Phone"]=["Số điện thoại này đã được sử dụng."];
            if(errors.Count>0)return Results.ValidationProblem(errors,statusCode:StatusCodes.Status409Conflict);
            await using var tx=await db.Database.BeginTransactionAsync();
            var p=new InternProfile{Name=input.Name.Trim(),Email=email,StudentId=student,Phone=phone,DateOfBirth=input.DateOfBirth,School=input.School.Trim(),Major=input.Major.Trim(),PasswordHash="",Status="Chờ hồ sơ",EmailVerified=!RequireEmailVerification(config)};
            p.PasswordHash=hasher.HashPassword(p,input.Password);db.InternProfiles.Add(p);await db.SaveChangesAsync();if(RequireEmailVerification(config))Verification(db,p,config);await db.SaveChangesAsync();await tx.CommitAsync();return Results.Created($"/api/interns/{p.Id}/workspace",p.ToResponse());
        });
        app.MapPost("/api/interns/login",async(LoginInternRequest input,HttpContext c,CareerDbContext db,IPasswordHasher<InternProfile> hasher)=>{
            var email=input.Identity.Trim().ToLowerInvariant();var p=await db.InternProfiles.FirstOrDefaultAsync(x=>x.Email==email);
            if(p is null||hasher.VerifyHashedPassword(p,p.PasswordHash,input.Password)==PasswordVerificationResult.Failed)return Results.Json(new{message="Tài khoản hoặc mật khẩu không đúng."},statusCode:401);
            await SprintSecurity.SignIn(c,p.Id,"Intern",p.Name);return Results.Ok(p.ToResponse());
        });
        app.MapPost("/api/auth/verify-email",async(VerificationInput input,CareerDbContext db)=>{
            var p=await db.InternProfiles.FindAsync(input.ProfileId);
            if(p is null||p.EmailVerified||p.VerificationExpires<DateTimeOffset.UtcNow||p.VerificationHash!=Hash(input.Token??""))return Results.BadRequest(new{message="Liên kết xác thực không hợp lệ hoặc hết hạn."});
            p.EmailVerified=true;p.VerificationHash=null;p.VerificationExpires=null;await db.SaveChangesAsync();return Results.Ok(new{message="Email đã xác thực. Bạn có thể nộp hồ sơ."});
        });
        app.MapPost("/api/auth/resend-verification",async(HttpContext c,CareerDbContext db,IConfiguration config)=>{
            if(!c.User.IsInRole("Intern"))return Results.Forbid();var p=await db.InternProfiles.FindAsync(SprintSecurity.Id(c));if(p is null)return Results.NotFound();
            if(p.EmailVerified)return Results.BadRequest(new{message="Email đã được xác thực."});
            if(p.VerificationExpires>DateTimeOffset.UtcNow.AddHours(24).AddMinutes(-1))return Results.BadRequest(new{message="Vui lòng đợi một phút trước khi gửi lại."});
            Verification(db,p,config);await db.SaveChangesAsync();return Results.Ok(new{message="Đã xếp thư xác thực vào hàng đợi."});
        });
    }
}
public record VerificationInput(int ProfileId,string Token);

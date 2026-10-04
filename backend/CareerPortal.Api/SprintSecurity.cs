using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
public sealed class SprintSecurity(RequestDelegate next)
{
    public static int Id(HttpContext c)=>int.TryParse(c.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    public static bool HR(HttpContext c)=>c.User.IsInRole("HR");
    public static bool Own(HttpContext c,int id)=>HR(c)||(c.User.IsInRole("Intern")&&Id(c)==id);
    public static Task SignIn(HttpContext c,int id,string role,string name)=>c.SignInAsync("Sprint1",new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,id.ToString()),new Claim(ClaimTypes.Role,role),new Claim(ClaimTypes.Name,name)},"Sprint1")));
    public async Task InvokeAsync(HttpContext c)
    {
        var path=(c.Request.Path.Value??"").ToLowerInvariant();
        if(!path.StartsWith("/api/")){await next(c);return;}
        var origin=c.Request.Headers.Origin.ToString();
        if(c.Request.Method is not("GET" or "HEAD" or "OPTIONS")&&origin.Length>0&&origin!=$"{c.Request.Scheme}://{c.Request.Host}"){c.Response.StatusCode=403;return;}
        var open=path is "/api/auth/login" or "/api/interns/login" or "/api/interns/register" or "/api/auth/verify-email" or "/api/health/database";
        if(!open&&c.User.Identity?.IsAuthenticated!=true){c.Response.StatusCode=401;return;}
        if(path.StartsWith("/api/hr/")&&!HR(c)){c.Response.StatusCode=403;return;}
        try{await next(c);}
        catch(DbUpdateException){c.Response.StatusCode=409;await c.Response.WriteAsJsonAsync(new{message="Dữ liệu trùng hoặc vừa thay đổi. Hãy tải lại."});}
    }
}

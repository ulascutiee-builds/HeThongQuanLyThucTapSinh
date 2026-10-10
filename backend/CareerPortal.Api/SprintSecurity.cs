using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

public sealed class SprintSecurity(RequestDelegate next)
{
    public static int Id(HttpContext context) => int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public static int AccountId(HttpContext context) => int.TryParse(context.User.FindFirstValue("account_id"), out var id) ? id : 0;
    public static bool HR(HttpContext context) => context.User.IsInRole("HR") || context.User.IsInRole("Admin");
    public static bool HasPermission(HttpContext context, string permission) => context.User.HasClaim("permission", permission);
    public static bool Own(HttpContext context, int id)
    {
        if (context.User.IsInRole("Intern") && Id(context) == id) return true;
        var path = (context.Request.Path.Value ?? "").ToLowerInvariant();
        var permission = path.Contains("/documents") || path.StartsWith("/api/documents/") ? "documents.read" : context.Request.Method is "PUT" or "PATCH" ? "profiles.write" : "profiles.read";
        return HasPermission(context, permission);
    }
    static ClaimsPrincipal Principal(AppUser user, string[] roles, string[] permissions)
    {
        var identityId = user.ProfileId is int profileId && roles.Contains("Intern") ? profileId : user.Id;
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, identityId.ToString()), new("account_id", user.Id.ToString()), new(ClaimTypes.Name, user.Name) };
        if (user.ProfileId is int id) claims.Add(new("profile_id", id.ToString()));
        claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
        claims.AddRange(permissions.Select(x => new Claim("permission", x)));
        return new(new ClaimsIdentity(claims, "Sprint1"));
    }
    public static async Task SignIn(HttpContext context, AppUser user, string[] roles, string[] permissions)
    {
        context.User = Principal(user, roles, permissions);
        await context.SignInAsync("Sprint1", context.User);
    }
    public static object IdentityResponse(HttpContext context)
    {
        var roles = context.User.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray();
        var role = Accounts.Roles.FirstOrDefault(roles.Contains) ?? "Intern";
        int? profileId = int.TryParse(context.User.FindFirstValue("profile_id"), out var id) ? id : null;
        return new
        {
            id = Id(context), name = context.User.Identity?.Name, role, roles, accountId = AccountId(context), profileId,
            permissions = context.User.FindAll("permission").Select(x => x.Value).Distinct().ToArray(),
            href = role switch { "Admin" => "admin.html", "HR" => "hr.html", "Mentor" => "mentor.html", _ => "thuc-tap-sinh.html" }
        };
    }
    static string? RequiredPermission(string path, string method)
    {
        if (path.StartsWith("/api/accounts")) return "accounts.manage";
        if (path.StartsWith("/api/roles/")) return "roles.manage";
        if (path == "/api/hr/dashboard") return "profiles.read";
        if (path.StartsWith("/api/hr/profiles") && path.EndsWith("/contract")) return "contracts.write";
        if (path == "/api/hr/profiles" || path.StartsWith("/api/hr/profiles/")) return "profiles.write";
        if (path.StartsWith("/api/hr/documents/")) return "documents.review";
        if (path.StartsWith("/api/hr/applications/") || path == "/api/hr/email-status") return "applications.review";
        if (path == "/api/interns") return method == "GET" ? "profiles.read" : "profiles.write";
        return null;
    }
    public async Task InvokeAsync(HttpContext context, CareerDbContext db)
    {
        var path = (context.Request.Path.Value ?? "").ToLowerInvariant();
        if (!path.StartsWith("/api/")) { await next(context); return; }
        var origin = context.Request.Headers.Origin.ToString();
        if (context.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && origin.Length > 0 && origin != $"{context.Request.Scheme}://{context.Request.Host}") { context.Response.StatusCode = 403; return; }
        var open = path is "/api/auth/login" or "/api/interns/login" or "/api/interns/register" or "/api/auth/verify-email" or "/api/auth/activate" or "/api/health/database";
        if (!open)
        {
            if (context.User.Identity?.IsAuthenticated != true) { context.Response.StatusCode = 401; return; }
            var userId = AccountId(context);
            var user = await db.AppUsers.AsNoTracking().Include(x => x.UserRoles).ThenInclude(x => x.Role).ThenInclude(x => x!.RolePermissions).Include(x => x.UserPermissions).SingleOrDefaultAsync(x => x.Id == userId);
            if (user is null || !user.IsActive || user.RequiresActivation || user.UserRoles.Count == 0)
            {
                await context.SignOutAsync("Sprint1"); context.Response.StatusCode = 401; return;
            }
            // Re-read RBAC on each request so revocation and account disabling apply to existing sessions.
            context.User = Principal(user, user.UserRoles.Select(x => x.RoleName).ToArray(), Accounts.EffectivePermissions(user));
            var permission = RequiredPermission(path, context.Request.Method);
            if (permission is not null && !HasPermission(context, permission)) { context.Response.StatusCode = 403; return; }
            if (path.StartsWith("/api/hr/") && !HR(context) && permission is null) { context.Response.StatusCode = 403; return; }
        }
        try { await next(context); }
        catch (DbUpdateException)
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(new { message = "Dữ liệu trùng hoặc vừa thay đổi. Hãy tải lại." });
        }
    }
}

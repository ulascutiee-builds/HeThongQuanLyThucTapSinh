using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

public sealed class PortalSecurity(RequestDelegate next)
{
    public static bool Manager(ClaimsPrincipal user) => user.IsInRole("HR") || user.IsInRole("Admin");
    public static int UserId(ClaimsPrincipal user) => int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public static async Task SignIn(HttpContext context, int id, string role, string name) =>
        await context.SignInAsync("Portal", new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Name, name)
        }, "Portal")));

    public async Task InvokeAsync(HttpContext c, CareerDbContext db)
    {
        var path = c.Request.Path.Value ?? "";
        if (!path.StartsWith("/api/")) { await next(c); return; }
        var publicPath = path is "/api/auth/login" or "/api/auth/bootstrap" or "/api/auth/verify-email" or "/api/auth/activate-account" or "/api/interns/register" or "/api/interns/login" or "/api/health/database";
        // Cookie-authenticated writes must originate from the same site or the two development frontends.
        var origin = c.Request.Headers.Origin.ToString();
        if (c.Request.Method is not ("GET" or "HEAD" or "OPTIONS") && origin.Length > 0 &&
            origin != $"{c.Request.Scheme}://{c.Request.Host}" && origin is not ("http://localhost:8000" or "http://127.0.0.1:8000"))
        { c.Response.StatusCode = 403; return; }
        if (!publicPath)
        {
            if (c.User.Identity?.IsAuthenticated != true) { c.Response.StatusCode = 401; return; }
            var id = UserId(c.User);
            if (c.User.IsInRole("Intern") && await db.WorkItems.AnyAsync(x => x.Kind == "intern-access" && x.ProfileId == id && x.Status == "Disabled"))
            { await c.SignOutAsync("Portal"); c.Response.StatusCode = 401; return; }
            if (!c.User.IsInRole("Intern"))
            {
                var account = await db.PortalAccounts.FindAsync(id);
                if (account is null || !account.Active || !c.User.IsInRole(account.Role))
                { await c.SignOutAsync("Portal"); c.Response.StatusCode = 401; return; }
            }
            if (path.StartsWith("/api/hr/") && !Manager(c.User)) { c.Response.StatusCode = 403; return; }
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3 && segments[1] == "interns" && int.TryParse(segments[2], out var profileId) &&
                !Manager(c.User) && !(c.User.IsInRole("Intern") && id == profileId))
            { c.Response.StatusCode = 403; return; }
            if (segments.Length >= 3 && segments[1] == "documents" && int.TryParse(segments[2], out var documentId) && !Manager(c.User))
            {
                if (!c.User.IsInRole("Intern") || !await db.InternDocuments.AnyAsync(d => d.Id == documentId && d.ProfileId == id))
                { c.Response.StatusCode = 403; return; }
            }
        }
        try { await next(c); }
        catch (DbUpdateConcurrencyException) { c.Response.StatusCode = 409; await c.Response.WriteAsJsonAsync(new { message = "Dữ liệu vừa thay đổi. Hãy tải lại." }); }
        catch (DbUpdateException) { c.Response.StatusCode = 409; await c.Response.WriteAsJsonAsync(new { message = "Dữ liệu trùng hoặc không thỏa ràng buộc." }); }
        finally
        {
            if (c.User.Identity?.IsAuthenticated == true && c.Request.Method is not ("GET" or "HEAD" or "OPTIONS"))
            {
                db.ChangeTracker.Clear();
                db.PortalAudits.Add(new PortalAudit { Actor = $"{c.User.Identity.Name} ({UserId(c.User)})", Action = c.Request.Method, Resource = path, StatusCode = c.Response.StatusCode });
                await db.SaveChangesAsync();
            }
        }
    }
}

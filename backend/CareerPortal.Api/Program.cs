using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddValidation();
builder.Services.AddDbContext<CareerDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("CareerPortal")));
builder.Services.AddScoped<IPasswordHasher<InternProfile>,PasswordHasher<InternProfile>>();
builder.Services.AddScoped<IPasswordHasher<AppUser>,PasswordHasher<AppUser>>();
var runtimeData=builder.Configuration["Runtime:DataDirectory"]??Path.Combine(builder.Environment.ContentRootPath,"App_Data");
builder.Services.AddDataProtection().SetApplicationName("CareerPortal.Sprint1").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(runtimeData,"keys")));
builder.Services.AddAuthentication("Sprint1").AddCookie("Sprint1",o=>{
    o.Cookie.Name="CareerPortal.Sprint1";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.ExpireTimeSpan=TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
    o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
});
builder.Services.AddAuthorization();builder.Services.AddHostedService<MailWorker>();builder.Services.AddHostedService<InternshipStatusWorker>();
var app=builder.Build();
app.UseAuthentication();app.UseAuthorization();app.UseMiddleware<SprintSecurity>();
var frontend=Path.Combine(app.Environment.ContentRootPath,"frontend");
if(!Directory.Exists(frontend))frontend=Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath,"../../frontend"));
app.UseDefaultFiles(new DefaultFilesOptions{FileProvider=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontend)});
app.Use(async (context,next)=>{
    // Older cached pages may still reference the previous SVG. Serve only the
    // replacement brand, even if a stale publish directory contains that file.
    if(string.Equals(context.Request.Path.Value,"/codegym-logo.svg",StringComparison.OrdinalIgnoreCase)){
        context.Response.Headers["Cache-Control"]="no-store, no-cache, must-revalidate, max-age=0";
        context.Response.Redirect("/ictu-codegym-logo.png?v=c8d8f689",permanent:false);
        return;
    }
    await next(context);
});
app.UseStaticFiles(new StaticFileOptions{
    FileProvider=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontend),
    OnPrepareResponse=context=>{
        // The app ships as a local executable: never leave a previous release's
        // HTML, scripts, styles or logo in the browser cache after an upgrade.
        context.Context.Response.Headers["Cache-Control"]="no-store, no-cache, must-revalidate, max-age=0";
        context.Context.Response.Headers["Pragma"]="no-cache";
        context.Context.Response.Headers["Expires"]="0";
    }
});
app.MapSprintAuth();app.MapEmailChange();app.MapProfiles();app.MapDocuments();app.MapApplications();app.MapAccounts();app.MapSprint2();app.MapSprint3();
app.MapGet("/api/health/database",async(CareerDbContext db)=>await db.Database.CanConnectAsync()?Results.Ok(new{connected=true}):Results.StatusCode(503));
if(!app.Configuration.GetValue("Database:SkipInitialization",false))
{
    using var scope=app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CareerDbContext>().Database.MigrateAsync();
    await app.InitializeAccountsAsync();
    await app.SeedDemoAsync();
}
app.Run();

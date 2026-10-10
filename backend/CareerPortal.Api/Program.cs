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
app.UseStaticFiles(new StaticFileOptions{FileProvider=new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontend)});
app.MapSprintAuth();app.MapProfiles();app.MapDocuments();app.MapApplications();app.MapAccounts();app.MapSprint2();app.MapSprint3();
app.MapGet("/api/health/database",async(CareerDbContext db)=>await db.Database.CanConnectAsync()?Results.Ok(new{connected=true}):Results.StatusCode(503));
if(!app.Configuration.GetValue("Database:SkipInitialization",false))
{
    using var scope=app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CareerDbContext>().Database.MigrateAsync();
    await app.InitializeAccountsAsync();
}
app.Run();

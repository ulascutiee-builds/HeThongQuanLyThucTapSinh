using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddValidation();
builder.Services.AddAuthentication("Portal").AddCookie("Portal", options =>
{
    options.Cookie.Name = "CareerPortal.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
Microsoft.AspNetCore.DataProtection.DataProtectionBuilderExtensions.PersistKeysToFileSystem(builder.Services.AddDataProtection(), new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));
builder.Services.AddHostedService<PortalJobs>();
builder.Services.AddCors(options => options.AddPolicy("LocalFrontend", policy =>
    policy.WithOrigins("http://127.0.0.1:8000", "http://localhost:8000")
        .AllowAnyHeader()
        .AllowAnyMethod().AllowCredentials()));
builder.Services.AddDbContext<CareerDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CareerPortal")
        ?? throw new InvalidOperationException("Connection string 'CareerPortal' is missing.")));
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<InternProfile>, Microsoft.AspNetCore.Identity.PasswordHasher<InternProfile>>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("LocalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PortalSecurity>();
app.MapPortalEndpoints();

app.MapInternEndpoints();
app.MapHrEndpoints();

app.MapGet("/api/health/database", async (CareerDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();

    return connected
        ? Results.Ok(new { connected = true, database = "CareerPortal" })
        : Results.Problem(
            title: "Database unavailable",
            detail: "The API could not connect to the CareerPortal SQL Server database.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

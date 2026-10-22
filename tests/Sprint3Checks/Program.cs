using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// This suite starts its own API on a random port and a uniquely named LocalDB
// database. It never connects to the application's normal database.
var tag = Guid.NewGuid().ToString("N");
var databaseName = "CareerPortalSprint3Checks_" + tag;
var connection = new SqlConnectionStringBuilder
{
    DataSource = Environment.GetEnvironmentVariable("SPRINT3_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
    InitialCatalog = databaseName,
    IntegratedSecurity = true,
    TrustServerCertificate = true
}.ConnectionString;
var options = new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options;
await using var db = new CareerDbContext(options);
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var apiProject = Path.Combine(root, "backend", "CareerPortal.Api");
var apiDll = Path.Combine(apiProject, "bin", "Release", "net10.0", "CareerPortal.Api.dll");
var runtime = Path.Combine(root, ".local", "sprint3-checks-" + tag);
Directory.CreateDirectory(runtime);
using var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
listener.Stop();
var address = new Uri($"http://localhost:{port}");
const string password = "TestOnly123!";
var hrEmail = $"s3.hr.{tag}@example.test";
var adminEmail = $"s3.admin.{tag}@example.test";
var mentorEmail = $"s3.mentor.{tag}@example.test";
var internEmail = $"s3.intern.{tag}@example.test";
var checks = 0;
var passed = false;
var databaseRemoved = false;
Process? server = null;
var logs = new StringBuilder();

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

HttpClient Client() => new(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = address };

async Task<JsonNode?> Request(HttpClient client, HttpMethod method, string path, object? body = null, int expected = 200)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    var content = await response.Content.ReadAsStringAsync();
    Check((int)response.StatusCode == expected,
        $"{method} {path}: expected {expected}, got {(int)response.StatusCode}. {content}");
    return string.IsNullOrWhiteSpace(content) ? null : JsonNode.Parse(content);
}

Task<JsonNode?> Get(HttpClient client, string path, int expected = 200)
    => Request(client, HttpMethod.Get, path, expected: expected);
Task<JsonNode?> Post(HttpClient client, string path, object body, int expected = 200)
    => Request(client, HttpMethod.Post, path, body, expected);
Task<JsonNode?> Put(HttpClient client, string path, object body, int expected = 200)
    => Request(client, HttpMethod.Put, path, body, expected);
Task<JsonNode?> Patch(HttpClient client, string path, object body, int expected = 200)
    => Request(client, HttpMethod.Patch, path, body, expected);
Task<JsonNode?> Login(HttpClient client, string email)
    => Post(client, "/api/auth/login", new { identity = email, password });
int Id(JsonNode? node) => node!["id"]!.GetValue<int>();
bool HasId(JsonNode? list, int id) => list!.AsArray().Any(x => x!["id"]!.GetValue<int>() == id);

try
{
    Check(File.Exists(apiDll), "Release API DLL missing. Build tests/Sprint3Checks in Release first.");
    var start = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = apiProject,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (var arg in new[]
    {
        apiDll, "--urls", address.ToString(),
        "--ConnectionStrings:CareerPortal", connection,
        "--Runtime:DataDirectory", runtime,
        "--Sprint1:HrEmail", hrEmail, "--Sprint1:HrPassword", password,
        "--Accounts:AdminEmail", adminEmail, "--Accounts:AdminPassword", password,
        "--Accounts:MentorEmail", mentorEmail, "--Accounts:MentorPassword", password,
        "--Accounts:UserEmail", internEmail, "--Accounts:UserPassword", password,
        "--PublicUrl", address.ToString(), "--Email:SmtpHost", "",
        "--Logging:LogLevel:Default", "Warning"
    }) start.ArgumentList.Add(arg);
    server = new Process { StartInfo = start };
    server.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (logs) logs.AppendLine(e.Data); };
    server.Start();
    server.BeginOutputReadLine();
    server.BeginErrorReadLine();

    using var anon = Client();
    using var admin = Client();
    using var hr = Client();
    using var mentor = Client();
    using var secondMentor = Client();
    using var intern = Client();
    var ready = false;
    for (var i = 0; i < 90; i++)
    {
        try
        {
            using var health = await anon.GetAsync("/api/health/database");
            if (health.IsSuccessStatusCode) { ready = true; break; }
        }
        catch (HttpRequestException) { }
        if (server.HasExited) break;
        await Task.Delay(500);
    }
    Check(ready, "Isolated Sprint 3 API did not start: " + logs);

    var mentorIdentity = await Login(mentor, mentorEmail);
    await Login(admin, adminEmail);
    await Login(hr, hrEmail);
    await Login(intern, internEmail);
    Check(mentorIdentity!["role"]!.GetValue<string>() == "Mentor", "Seeded Mentor cannot log in as Mentor");
    var mentorUser = await db.AppUsers.AsNoTracking().Include(x => x.UserRoles).SingleAsync(x => x.Email == mentorEmail);
    var internProfile = await db.InternProfiles.AsNoTracking().SingleAsync(x => x.Email == internEmail);
    Check(mentorUser.UserRoles.Any(x => x.RoleName == "Mentor"), "Mentor seed did not persist its role");
    var seededMentor = await db.Mentors.AsNoTracking().SingleAsync(x => x.UserId == mentorUser.Id);
    Check(seededMentor.Capacity > 0, "Mentor seed did not create a usable domain profile");
    Check((await Get(mentor, "/api/evaluations"))!.AsArray().Count == 0,
        "New Mentor must get an empty evaluation list, not a server error");
    await Get(anon, "/api/notifications", 401);

    var secondEmail = $"s3.mentor2.{tag}@example.test";
    var secondUser = new AppUser { Name = "Mentor thứ hai", Email = secondEmail, PasswordHash = "" };
    secondUser.PasswordHash = new PasswordHasher<AppUser>().HashPassword(secondUser, password);
    secondUser.UserRoles.Add(new UserRole { RoleName = "Mentor" });
    db.AppUsers.Add(secondUser);
    await db.SaveChangesAsync();
    await Login(secondMentor, secondEmail);

    var departmentId = Id(await Post(hr, "/api/departments", new { name = "Sprint 3 " + tag }, 201));
    var today = InternshipStatusSync.Today;
    var programId = Id(await Post(hr, "/api/programs", new
    {
        name = "Sprint 3 integration " + tag,
        departmentId,
        description = "Disposable integration fixture",
        capacity = 3,
        startDate = today.AddDays(-1),
        endDate = today.AddDays(30),
        status = "Active"
    }, 201));
    var mentorId = seededMentor.Id;
    await Put(hr, $"/api/mentors/{mentorId}", new { userId = mentorUser.Id, departmentId, capacity = 3 });
    var secondMentorId = Id(await Post(hr, "/api/mentors", new { userId = secondUser.Id, departmentId, capacity = 3 }, 201));
    var taskInput = new
    {
        title = "Nhiệm vụ Sprint 3",
        description = "Kiểm thử tích hợp",
        dueDate = today.AddDays(2),
        priority = "Normal",
        profileId = internProfile.Id,
        programId
    };
    await Post(mentor, "/api/tasks", taskInput, 400);
    var assignmentId = Id(await Post(hr, "/api/assignments", new
        { profileId = internProfile.Id, programId, mentorId }, 201));

    var statusProfile = await db.InternProfiles.SingleAsync(x => x.Id == internProfile.Id);
    await Put(hr, $"/api/hr/profiles/{internProfile.Id}", new
    {
        name = statusProfile.Name,
        studentId = statusProfile.StudentId,
        email = statusProfile.Email,
        phone = statusProfile.Phone,
        school = statusProfile.School,
        major = statusProfile.Major,
        dateOfBirth = statusProfile.DateOfBirth,
        startDate = today,
        endDate = today.AddDays(30),
        status = "Đang thực tập"
    }, 400);
    statusProfile.Status = "Đang thực tập";
    await db.SaveChangesAsync();
    await Get(intern, $"/api/interns/{internProfile.Id}/documents");
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Chờ hồ sơ", "Profile missing its CV and application did not move to document-pending status");

    var statusApplication = await db.InternApplications.SingleOrDefaultAsync(x => x.ProfileId == internProfile.Id);
    if (statusApplication is null)
    {
        statusApplication = new InternApplication { ProfileId = internProfile.Id, Status = "Chờ duyệt" };
        db.InternApplications.Add(statusApplication);
    }
    else
        statusApplication.Status = "Chờ duyệt";
    var previousDocuments = await db.InternDocuments.Where(x => x.ProfileId == internProfile.Id).ToListAsync();
    foreach (var previousDocument in previousDocuments.Where(x =>
        x.Type is "CV" or "Đơn xin thực tập" or "Hợp đồng thực tập"))
        previousDocument.IsCurrent = false;
    int NextVersion(string type) => previousDocuments.Where(x => x.Type == type)
        .Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
    foreach (var type in new[] { "CV", "Đơn xin thực tập" })
    {
        db.InternDocuments.Add(new InternDocument
        {
            ProfileId = internProfile.Id,
            Type = type,
            FileName = type + ".pdf",
            ContentType = "application/pdf",
            Content = "%PDF-1.4\n%%EOF"u8.ToArray(),
            Status = "Chờ duyệt",
            Version = NextVersion(type),
            UploadedBy = internEmail
        });
    }
    var futureContract = new InternDocument
    {
        ProfileId = internProfile.Id,
        Type = "Hợp đồng thực tập",
        FileName = "status-check.pdf",
        ContentType = "application/pdf",
        Content = "%PDF-1.4\n%%EOF"u8.ToArray(),
        Status = "Chờ xác nhận",
        Version = NextVersion("Hợp đồng thực tập"),
        StartsAt = InternshipStatusSync.StartOfDay(today.AddDays(1)),
        ExpiresAt = InternshipStatusSync.EndOfDay(today.AddDays(30)),
        UploadedBy = internEmail
    };
    statusProfile.Status = "Đang thực tập";
    db.InternDocuments.Add(futureContract);
    await db.SaveChangesAsync();
    await Get(intern, $"/api/interns/{internProfile.Id}/documents");
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Chờ duyệt", "Submitted CV and application did not move the profile to approval-pending status");
    statusApplication.Status = "Đã duyệt";
    var submittedDocs = await db.InternDocuments.Where(x => x.ProfileId == internProfile.Id &&
        (x.Type == "CV" || x.Type == "Đơn xin thực tập") && x.IsCurrent).ToListAsync();
    foreach (var document in submittedDocs)
        document.Status = "Đã duyệt";
    statusProfile.Status = "Đang thực tập";
    await db.SaveChangesAsync();
    await Get(intern, $"/api/interns/{internProfile.Id}/documents");
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Chờ bắt đầu", "Unconfirmed contract did not keep the approved profile waiting to start");
    await Post(intern, $"/api/interns/{internProfile.Id}/documents/{futureContract.Id}/confirm-contract", new { });
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Đang thực tập", "Confirming a future-start contract did not activate the internship");
    await Get(intern, $"/api/interns/{internProfile.Id}/documents");
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Đang thực tập", "Status synchronization reverted a confirmed contract before its start date");

    var taskId = Id(await Post(mentor, "/api/tasks", taskInput, 201));
    Check(HasId(await Get(intern, "/api/tasks"), taskId), "Intern cannot see their task");
    Check(HasId(await Get(mentor, "/api/tasks"), taskId), "Mentor cannot see assigned task");
    await Put(secondMentor, $"/api/tasks/{taskId}", taskInput, 403);
    await Put(intern, $"/api/tasks/{taskId}", taskInput, 403);
    var changedTask = new
    {
        title = "Nhiệm vụ đã sửa",
        description = "Mục tiêu được cập nhật",
        dueDate = today.AddDays(3),
        priority = "High",
        profileId = internProfile.Id,
        programId
    };
    await Put(mentor, $"/api/tasks/{taskId}", changedTask);
    Check((await db.InternshipTasks.AsNoTracking().SingleAsync(x => x.Id == taskId)).Title == changedTask.title,
        "Mentor task edit did not persist");
    await Patch(mentor, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "InProgress" }, 403);
    await Patch(intern, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "Done" }, 400);
    await Patch(intern, $"/api/tasks/{taskId}/progress", new { progress = 40, status = "InProgress", note = "Đang thực hiện" });
    Check((await Get(intern, $"/api/tasks/{taskId}/history"))!.AsArray().Count == 1,
        "Task progress history is missing");

    var week = today.AddDays(-(int)today.DayOfWeek + 1);
    var reportInput = new { weekStart = week, summary = "Đã hoàn thành mục tiêu", blockers = "", evidenceUrl = "https://example.test/evidence" };
    var reportId = Id(await Post(intern, "/api/reports/weekly", reportInput, 201));
    Check(HasId(await Get(mentor, "/api/reports/weekly"), reportId), "Assigned Mentor cannot see weekly report");
    Check(!HasId(await Get(secondMentor, "/api/reports/weekly"), reportId), "Unassigned Mentor can see weekly report");
    var editedReport = new { weekStart = week, summary = "Báo cáo đã chỉnh sửa", blockers = "Cần hỗ trợ", evidenceUrl = "https://example.test/updated" };
    await Put(intern, $"/api/reports/weekly/{reportId}", editedReport);
    Check((await Get(intern, "/api/reports/weekly"))!.AsArray().Any(x => x!["id"]!.GetValue<int>() == reportId
        && x["summary"]!.GetValue<string>() == editedReport.summary), "Weekly report edit is not visible");
    await Patch(secondMentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Unauthorized" }, 403);
    await Patch(mentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Đạt yêu cầu" });
    await Put(intern, $"/api/reports/weekly/{reportId}", editedReport, 400);

    var evaluationInput = new
    {
        profileId = internProfile.Id, programId,
        skills = 4, attitude = 5, communication = 4, teamwork = 5,
        comment = "Tiến bộ tốt"
    };
    await Post(mentor, "/api/evaluations", new
    {
        evaluationInput.profileId, evaluationInput.programId,
        evaluationInput.skills, evaluationInput.attitude, evaluationInput.communication,
        teamwork = 6, evaluationInput.comment
    }, 400);
    var evaluationId = (await Post(mentor, "/api/evaluations", evaluationInput))!.GetValue<int>();
    Check(HasId(await Get(intern, "/api/evaluations"), evaluationId), "Intern cannot see own evaluation");
    Check(!HasId(await Get(secondMentor, "/api/evaluations"), evaluationId), "Unassigned Mentor can see evaluation");

    await Put(hr, $"/api/assignments/{assignmentId}", new { mentorId = secondMentorId });
    Check(!HasId(await Get(mentor, "/api/tasks"), taskId), "Previous Mentor can still list reassigned task");
    Check(HasId(await Get(secondMentor, "/api/tasks"), taskId), "Current Mentor cannot see inherited task");
    await Put(mentor, $"/api/tasks/{taskId}", changedTask, 403);
    await Put(secondMentor, $"/api/tasks/{taskId}", changedTask);
    await Get(mentor, $"/api/tasks/{taskId}/history", 403);
    await Get(secondMentor, $"/api/tasks/{taskId}/history");
    await Post(mentor, "/api/tasks", taskInput, 400);
    await Post(secondMentor, "/api/tasks", taskInput, 201);
    Check(!HasId(await Get(mentor, "/api/reports/weekly"), reportId),
        "Previous Mentor can still list reassigned report");
    Check(HasId(await Get(secondMentor, "/api/reports/weekly"), reportId),
        "Current Mentor cannot see inherited report");
    await Patch(mentor, $"/api/reports/weekly/{reportId}/feedback", new { feedback = "Unauthorized" }, 403);

    Check(!HasId(await Get(mentor, "/api/evaluations"), evaluationId),
        "Previous Mentor can still list reassigned evaluation");
    Check(HasId(await Get(secondMentor, "/api/evaluations"), evaluationId),
        "Current Mentor cannot see inherited evaluation");
    await Post(mentor, "/api/evaluations", evaluationInput, 403);
    var updatedEvaluationId = (await Post(secondMentor, "/api/evaluations", new
    {
        evaluationInput.profileId, evaluationInput.programId,
        skills = 5, attitude = 5, communication = 5, teamwork = 4,
        comment = "Đã tiếp quản"
    }))!.GetValue<int>();
    Check(updatedEvaluationId == evaluationId, "Reassigned Mentor created a duplicate evaluation");
    Check(await db.InternshipEvaluations.AsNoTracking().CountAsync(x => x.ProfileId == internProfile.Id && x.ProgramId == programId) == 1,
        "Reassignment left duplicate evaluations");

    var nextReport = Id(await Post(intern, "/api/reports/weekly", new
        { weekStart = week.AddDays(7), summary = "Tuần tiếp theo", blockers = "", evidenceUrl = "https://example.test/next" }, 201));
    await Patch(mentor, $"/api/reports/weekly/{nextReport}/feedback", new { feedback = "Unauthorized" }, 403);
    await Patch(secondMentor, $"/api/reports/weekly/{nextReport}/feedback", new { feedback = "Đã nhận" });

    // Deliberately make a ProfileId equal to a non-Intern account ID. A role
    // account must not see or mark that profile's notification as read.
    while (await db.InternProfiles.MaxAsync(x => x.Id) < mentorUser.Id)
    {
        var next = await db.InternProfiles.MaxAsync(x => x.Id) + 1;
        db.InternProfiles.Add(new InternProfile
        {
            Name = "Collision fixture " + next,
            StudentId = "S3-COL-" + next + "-" + tag[..16],
            Email = $"s3.collision.{next}.{tag}@example.test",
            School = "Test", Major = "Test", PasswordHash = "unused", EmailVerified = true
        });
        await db.SaveChangesAsync();
    }
    var collision = await db.InternProfiles.AsNoTracking().SingleAsync(x => x.Id == mentorUser.Id);
    var unreadBeforeFixture = (await Get(mentor, "/api/notifications"))!["unread"]!.GetValue<int>();
    var profileNotification = new PortalNotification
    {
        ProfileId = collision.Id, Title = "Role isolation " + tag, Message = "Private profile notification"
    };
    var accountNotification = new PortalNotification
    {
        AccountId = mentorUser.Id, Title = "Mentor account " + tag, Message = "Account notification"
    };
    db.PortalNotifications.AddRange(profileNotification, accountNotification);
    await db.SaveChangesAsync();
    var notificationResponse = await Get(mentor, "/api/notifications");
    var mentorNotifications = notificationResponse!["items"]!;
    Check(!HasId(mentorNotifications, profileNotification.Id), "Mentor read another profile's notification by ID collision");
    Check(HasId(mentorNotifications, accountNotification.Id), "Mentor cannot read account notification");
    Check(notificationResponse["unread"]!.GetValue<int>() == unreadBeforeFixture + 1,
        "Unread notification count did not include the Mentor's account notification");
    await Patch(mentor, $"/api/notifications/{profileNotification.Id}/read", new { }, 404);
    await Post(mentor, "/api/notifications/read-all", new { });
    var notificationsAfterReadAll = await Get(mentor, "/api/notifications");
    Check(notificationsAfterReadAll!["unread"]!.GetValue<int>() == 0,
        "Marking all notifications as read did not reset the unread count");
    Check(!(await db.PortalNotifications.AsNoTracking().SingleAsync(x => x.Id == profileNotification.Id)).IsRead,
        "Mentor marked another profile's notification read");
    Check((await db.PortalNotifications.AsNoTracking().SingleAsync(x => x.Id == accountNotification.Id)).IsRead,
        "Mentor did not mark own account notification read");
    if (collision.Id == internProfile.Id)
        Check(HasId((await Get(intern, "/api/notifications"))!["items"], profileNotification.Id),
            "Intern cannot read own profile notification");

    futureContract.ExpiresAt = InternshipStatusSync.EndOfDay(today.AddDays(-1));
    await db.SaveChangesAsync();
    await Get(intern, $"/api/interns/{internProfile.Id}/documents");
    await db.Entry(statusProfile).ReloadAsync();
    Check(statusProfile.Status == "Đã hoàn thành", "Expired confirmed contract did not move the profile to completed");

    // Permanent account deletion must remove account-owned records while keeping
    // shared program data and all other users. These are disposable test identities.
    await Request(hr, HttpMethod.Delete, $"/api/accounts/{mentorUser.Id}", expected: 403);
    await Request(admin, HttpMethod.Delete, $"/api/accounts/{(await db.AppUsers.AsNoTracking().SingleAsync(x => x.Email == adminEmail)).Id}", expected: 409);
    await Request(admin, HttpMethod.Delete, $"/api/accounts/{mentorUser.Id}", expected: 204);
    Check(!await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == mentorUser.Id), "Deleted Mentor account remained in SQL");
    Check(!await db.Mentors.AsNoTracking().AnyAsync(x => x.UserId == mentorUser.Id), "Deleted Mentor domain row remained in SQL");
    Check(!await db.InternshipTasks.AsNoTracking().AnyAsync(x => x.Id == taskId), "Deleted Mentor task remained in SQL");
    await Get(mentor, "/api/auth/me", 401);
    await Get(admin, $"/api/accounts/{mentorUser.Id}", 404);

    await Request(admin, HttpMethod.Delete, $"/api/accounts/{secondUser.Id}", expected: 204);
    Check(!await db.InternAssignments.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id),
        "Mentor deletion left an assignment pointing to the removed Mentor");
    Check(!await db.InternshipEvaluations.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id),
        "Mentor-owned evaluation remained after deletion");
    Check(await db.InternProfiles.AsNoTracking().AnyAsync(x => x.Id == internProfile.Id),
        "Mentor deletion unexpectedly removed the intern's profile");

    var internAccountId = await db.AppUsers.AsNoTracking().Where(x => x.ProfileId == internProfile.Id).Select(x => x.Id).SingleAsync();
    var schedule = new ProgramSchedule { ProgramId = programId, Date = today, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), Title = "Deletion fixture" };
    db.ProgramSchedules.Add(schedule); await db.SaveChangesAsync();
    var nextCvVersion = (await db.InternDocuments.Where(x => x.ProfileId == internProfile.Id && x.Type == "CV")
        .Select(x => (int?)x.Version).MaxAsync() ?? 0) + 1;
    db.InternDocuments.Add(new InternDocument { ProfileId = internProfile.Id, Type = "CV", FileName = "fixture.pdf", ContentType = "application/pdf", Content = [1, 2, 3], Version = nextCvVersion });
    db.InternApplications.Add(new InternApplication { ProfileId = internProfile.Id });
    db.AttendanceRecords.Add(new AttendanceRecord { ProfileId = internProfile.Id, ScheduleId = schedule.Id, Date = today, CheckIn = DateTimeOffset.UtcNow, CheckOut = DateTimeOffset.UtcNow.AddHours(1) });
    db.LeaveRequests.Add(new LeaveRequest { ProfileId = internProfile.Id, From = today.AddDays(1), To = today.AddDays(1), Reason = "Fixture" });
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    await Request(admin, HttpMethod.Delete, $"/api/accounts/{internAccountId}", expected: 204);
    Check(!await db.AppUsers.AsNoTracking().AnyAsync(x => x.Id == internAccountId), "Deleted Intern account remained in SQL");
    Check(!await db.InternProfiles.AsNoTracking().AnyAsync(x => x.Id == internProfile.Id), "Deleted Intern profile remained in SQL");
    Check(!await db.InternDocuments.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id), "Deleted Intern document remained in SQL");
    Check(!await db.InternApplications.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id), "Deleted Intern application remained in SQL");
    Check(!await db.AttendanceRecords.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id), "Deleted Intern attendance remained in SQL");
    Check(!await db.LeaveRequests.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id), "Deleted Intern leave remained in SQL");
    Check(!await db.WeeklyReports.AsNoTracking().AnyAsync(x => x.ProfileId == internProfile.Id), "Deleted Intern weekly reports remained in SQL");
    Check(!await db.PortalNotifications.AsNoTracking().AnyAsync(x => x.AccountId == internAccountId || x.ProfileId == internProfile.Id),
        "Deleted Intern notifications remained in SQL");
    Check(await db.InternshipPrograms.AsNoTracking().AnyAsync(x => x.Id == programId), "Account deletion removed a shared program");
    Check(await db.ProgramSchedules.AsNoTracking().AnyAsync(x => x.Id == schedule.Id), "Account deletion removed a shared schedule");
    Check(await db.AppUsers.AsNoTracking().AnyAsync(x => x.Email == adminEmail), "Account deletion removed another user");
    await Get(intern, "/api/auth/me", 401);
    await Get(admin, $"/api/accounts/{internAccountId}", 404);

    passed = true;
}
finally
{
    if (server is not null)
    {
        if (!server.HasExited) { server.Kill(entireProcessTree: true); await server.WaitForExitAsync(); }
        server.Dispose();
    }
    await File.WriteAllTextAsync(Path.Combine(runtime, "server.log"), logs.ToString());
    SqlConnection.ClearAllPools();
    try
    {
        databaseRemoved = await db.Database.EnsureDeletedAsync();
        if (databaseRemoved) Console.WriteLine($"Removed isolated test database {databaseName}.");
    }
    catch (SqlException error)
    {
        Console.Error.WriteLine($"Could not remove isolated test database {databaseName}: {error.Message}");
    }
    if (passed && databaseRemoved)
    {
        var local = Path.GetFullPath(Path.Combine(root, ".local")) + Path.DirectorySeparatorChar;
        var exactRuntime = Path.GetFullPath(runtime);
        if (exactRuntime.StartsWith(local, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(exactRuntime) == "sprint3-checks-" + tag)
            Directory.Delete(exactRuntime, recursive: true);
    }
}
if (passed)
{
    Check(databaseRemoved, $"Isolated test database {databaseName} could not be removed.");
    Console.WriteLine($"PASS: {checks} Sprint 3 API, permission and persistence checks. Disposable database {databaseName}.");
}

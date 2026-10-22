using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

/// <summary>
/// Dữ liệu hoàn toàn giả lập cho môi trường phát triển. Chỉ chạy khi -SeedDemoData được yêu cầu.
/// Không xóa hoặc cập nhật các hồ sơ thật đã tồn tại.
/// </summary>
public static class DemoDataSeeder
{
    const string Suffix = "@careerportal-demo.invalid";
    const string Owner = "Nhân sự (dữ liệu mẫu)";

    static DateOnly Monday(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
    static bool Workday(DateOnly date) => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
    static DateTimeOffset Local(DateOnly date, int hour, int minute = 0) =>
        new(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.FromHours(7));

    static byte[] Pdf(string fileName)
    {
        var assembly = typeof(DemoDataSeeder).Assembly;
        var resource = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("DemoFiles." + fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Thiếu tệp PDF mẫu nhúng: " + fileName);
        using var input = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException("Không thể đọc PDF mẫu: " + resource);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    public static async Task SeedDemoAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue("DemoData:Seed", false)) return;
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CareerDbContext>();
        // Chặn nạp trùng, không đụng dữ liệu người dùng khác.
        if (await db.InternProfiles.AnyAsync(x => x.Email.EndsWith(Suffix)))
        {
            app.Logger.LogInformation("Dữ liệu mẫu đã tồn tại; bỏ qua để không nhân bản hoặc ghi đè.");
            return;
        }

        var accountHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var profileHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<InternProfile>>();
        var demoPassword = app.Configuration["Accounts:UserPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword) || demoPassword.Length < 8)
            throw new InvalidOperationException("Cần cấu hình Accounts:UserPassword ít nhất 8 ký tự để tạo tài khoản mẫu.");
        var today = InternshipStatusSync.Today;
        var week = Monday(today);
        var start = week.AddDays(-28);
        var end = week.AddDays(42);
        var nextStart = week.AddDays(7);
        var nextEnd = week.AddDays(63);
        var pastStart = week.AddDays(-84);
        var pastEnd = week.AddDays(-7);
        var now = DateTimeOffset.UtcNow;
        var cvContent = Pdf("CV-MAU.pdf");
        var applicationContent = Pdf("DON-XIN-THUC-TAP-MAU.pdf");
        var contractContent = Pdf("HOP-DONG-THUC-TAP-MAU.pdf");

        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Phòng ban và chương trình (ba trạng thái và ba thời kỳ).
        var software = new Department { Name = "[DEMO] Công nghệ phần mềm" };
        var qa = new Department { Name = "[DEMO] Kiểm thử phần mềm" };
        var data = new Department { Name = "[DEMO] Phân tích dữ liệu" };
        db.Departments.AddRange(software, qa, data);
        await db.SaveChangesAsync();
        var programs = new[]
        {
            new InternshipProgram { Name = "[DEMO] Full-stack Web 2026", DepartmentId = software.Id, Description = "Thực hành React, ASP.NET Core, SQL Server; Scrum, Git và triển khai sản phẩm.", Capacity = 20, StartDate = start, EndDate = end, Status = "Active" },
            new InternshipProgram { Name = "[DEMO] QA Automation 2026", DepartmentId = qa.Id, Description = "Kiểm thử chức năng, viết test case, Playwright, kiểm thử API và báo cáo lỗi.", Capacity = 12, StartDate = nextStart, EndDate = nextEnd, Status = "Open" },
            new InternshipProgram { Name = "[DEMO] Data Analyst khóa trước", DepartmentId = data.Id, Description = "Làm sạch dữ liệu, SQL, Power BI và trực quan hóa báo cáo.", Capacity = 10, StartDate = pastStart, EndDate = pastEnd, Status = "Completed" },
            new InternshipProgram { Name = "[DEMO] Mobile App kỳ tới", DepartmentId = software.Id, Description = "Dự kiến đào tạo Flutter, kiểm thử và phát hành ứng dụng di động.", Capacity = 15, StartDate = nextStart, EndDate = nextEnd, Status = "Draft" }
        };
        db.InternshipPrograms.AddRange(programs);
        await db.SaveChangesAsync();

        // 2. Mentor: tận dụng mentor có sẵn chỉ nếu đang ở bộ phận mặc định chưa phân công;
        // bổ sung 2 mentor hoàn toàn giả lập, có thể đăng nhập bằng mật khẩu demo.
        var mentors = new List<Mentor>();
        var configuredEmail = app.Configuration["Accounts:MentorEmail"]?.Trim().ToLowerInvariant();
        var configuredAccount = string.IsNullOrEmpty(configuredEmail) ? null
            : await db.AppUsers.FirstOrDefaultAsync(x => x.Email == configuredEmail);
        if (configuredAccount is not null)
        {
            var existingMentor = await db.Mentors.Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.UserId == configuredAccount.Id);
            if (existingMentor is not null)
            {
                if (existingMentor.Department?.Name == "Chưa phân công" &&
                    !await db.InternAssignments.AnyAsync(x => x.MentorId == existingMentor.Id))
                {
                    existingMentor.DepartmentId = software.Id;
                    existingMentor.Capacity = 12;
                    mentors.Add(existingMentor);
                }
            }
        }
        foreach (var spec in new[]
        {
            ("Nguyễn Thu Hà", "mentor.ha" + Suffix, software.Id, 12),
            ("Trần Đức Long", "mentor.long" + Suffix, qa.Id, 10),
            ("Lê Minh Phúc", "mentor.phuc" + Suffix, data.Id, 10)
        })
        {
            var account = new AppUser { Name = spec.Item1, Email = spec.Item2, PasswordHash = "" };
            account.PasswordHash = accountHasher.HashPassword(account, demoPassword);
            account.UserRoles.Add(new UserRole { RoleName = "Mentor" });
            db.AppUsers.Add(account);
            await db.SaveChangesAsync();
            var mentor = new Mentor { UserId = account.Id, DepartmentId = spec.Item3, Capacity = spec.Item4 };
            db.Mentors.Add(mentor);
            mentors.Add(mentor);
        }
        await db.SaveChangesAsync();
        var devMentor = mentors.First(x => x.DepartmentId == software.Id);
        var qaMentor = mentors.First(x => x.DepartmentId == qa.Id);
        var dataMentor = mentors.First(x => x.DepartmentId == data.Id);

        // 3. Mười hồ sơ giả lập, mỗi hồ sơ có tài khoản liên kết để thử đăng nhập vai trò Intern.
        var interns = new (string Name, string Code, string Email, string Major, int State)[]
        {
            ("Nguyễn Minh Anh", "DEMO26001", "tss01" + Suffix, "Công nghệ thông tin", 0),
            ("Trần Quốc Bảo", "DEMO26002", "tss02" + Suffix, "Kỹ thuật phần mềm", 0),
            ("Lê Thị Ngọc", "DEMO26003", "tss03" + Suffix, "Công nghệ thông tin", 0),
            ("Phạm Đức Huy", "DEMO26004", "tss04" + Suffix, "Kỹ thuật phần mềm", 0),
            ("Vũ Thảo My", "DEMO26005", "tss05" + Suffix, "Hệ thống thông tin", 1),
            ("Bùi Hoàng Nam", "DEMO26006", "tss06" + Suffix, "Công nghệ thông tin", 2),
            ("Đỗ Khánh Linh", "DEMO26007", "tss07" + Suffix, "Công nghệ phần mềm", 3),
            ("Nguyễn Gia Hân", "DEMO26008", "tss08" + Suffix, "Hệ thống thông tin", 4),
            ("Hoàng Văn Sơn", "DEMO26009", "tss09" + Suffix, "An toàn thông tin", 5),
            ("Mai Thanh Tùng", "DEMO26010", "tss10" + Suffix, "Khoa học dữ liệu", 6)
        };
        var profiles = new List<(InternProfile Profile, int State)>();
        foreach (var (name, code, email, major, state) in interns)
        {
            var profile = new InternProfile
            {
                Name = name, StudentId = code, Email = email,
                Phone = "0988" + (260000 + profiles.Count).ToString("D6"),
                DateOfBirth = new DateOnly(2003 + profiles.Count % 3, 1 + profiles.Count % 10, 10 + profiles.Count % 15),
                School = profiles.Count % 3 == 0 ? "Đại học Công nghệ Thông tin và Truyền thông - ICTU" :
                    profiles.Count % 3 == 1 ? "Đại học Thái Nguyên" : "Đại học Kỹ thuật Công nghiệp",
                Major = major, EmailVerified = state != 5, PasswordHash = "",
                Status = state switch { 0 => "Đang thực tập", 1 => "Chờ bắt đầu", 2 => "Chờ duyệt", 3 => "Từ chối", 6 => "Đã hoàn thành", _ => "Chờ hồ sơ" },
                StartDate = state switch { 0 => start, 1 => nextStart, 6 => pastStart, _ => null },
                EndDate = state switch { 0 => end, 1 => nextEnd, 6 => pastEnd, _ => null },
                CreatedAt = now.AddDays(-20 + profiles.Count)
            };
            profile.PasswordHash = profileHasher.HashPassword(profile, demoPassword);
            db.InternProfiles.Add(profile);
            await db.SaveChangesAsync();
            var account = new AppUser { Name = name, Email = email, ProfileId = profile.Id, PasswordHash = "" };
            account.PasswordHash = accountHasher.HashPassword(account, demoPassword);
            account.UserRoles.Add(new UserRole { RoleName = "Intern" });
            db.AppUsers.Add(account);
            profiles.Add((profile, state));
        }
        await db.SaveChangesAsync();

        // 4. Dữ liệu của tài khoản thực tập sinh demo mặc định: chỉ bổ sung nếu hoàn toàn nguyên bản,
        // không ảnh hưởng tới tài khoản đã có hồ sơ chỉnh sửa.
        var userEmail = app.Configuration["Accounts:UserEmail"]?.Trim().ToLowerInvariant();
        var builtin = string.IsNullOrEmpty(userEmail) ? null
            : await db.InternProfiles.FirstOrDefaultAsync(x => x.Email == userEmail);
        if (builtin is not null && builtin.Name == "Thực tập sinh" && builtin.StudentId == "DEMO-USER" &&
            !await db.InternDocuments.AnyAsync(x => x.ProfileId == builtin.Id) &&
            !await db.InternAssignments.AnyAsync(x => x.ProfileId == builtin.Id))
        {
            builtin.Name = "Thực tập sinh Demo";
            builtin.StudentId = "DEMO0000";
            builtin.School = "Đại học Công nghệ Thông tin và Truyền thông - ICTU";
            builtin.Major = "Kỹ thuật phần mềm";
            builtin.DateOfBirth = new DateOnly(2004, 4, 12);
            builtin.StartDate = start;
            builtin.EndDate = end;
            builtin.EmailVerified = true;
            builtin.Status = "Đang thực tập";
            var builtinAccount = await db.AppUsers.FirstOrDefaultAsync(x => x.ProfileId == builtin.Id);
            if (builtinAccount is not null) builtinAccount.Name = builtin.Name;
            profiles.Insert(0, (builtin, 0));
        }
        await db.SaveChangesAsync();

        // 5. Tài liệu, đơn xin thực tập, hợp đồng và lịch sử duyệt có dữ liệu thực PDF.
        foreach (var (profile, state) in profiles)
        {
            if (state == 5) continue; // Chưa nộp gì.
            var hasCv = state != 5;
            var hasApplicationPdf = state is 0 or 1 or 2 or 3 or 6;
            string docStatus = state switch { 2 => "Chờ duyệt", 3 => "Từ chối", _ => "Đã duyệt" };
            if (hasCv) AddDocument(profile, "CV", "CV-MAU.pdf", cvContent, state == 4 ? "Chờ duyệt" : docStatus);
            if (hasApplicationPdf) AddDocument(profile, "Đơn xin thực tập", "DON-XIN-THUC-TAP-MAU.pdf", applicationContent, docStatus);
            if (state is 0 or 1 or 2 or 3 or 6)
            {
                string status = state switch { 2 => "Chờ duyệt", 3 => "Từ chối", _ => "Đã duyệt" };
                var reviewAt = state is 2 ? (DateTimeOffset?)null : now.AddDays(-9);
                var appRecord = new InternApplication { ProfileId = profile.Id, Status = status, AppliedAt = now.AddDays(-17), ReviewedAt = reviewAt,
                    ReviewedBy = reviewAt.HasValue ? Owner : null, Note = state == 3 ? "Thông tin hồ sơ chưa đáp ứng yêu cầu (minh họa)." : null };
                db.InternApplications.Add(appRecord);
            }
        }
        await db.SaveChangesAsync();
        foreach (var (profile, state) in profiles.Where(x => x.State is 0 or 1 or 6))
        {
            var pStart = state == 0 ? start : state == 1 ? nextStart : pastStart;
            var pEnd = state == 0 ? end : state == 1 ? nextEnd : pastEnd;
            var status = state == 1 ? "Chờ xác nhận" : "Đã xác nhận";
            db.InternDocuments.Add(new InternDocument
            {
                ProfileId = profile.Id, Type = "Hợp đồng thực tập", FileName = "HOP-DONG-THUC-TAP-MAU.pdf", ContentType = "application/pdf", Content = contractContent,
                Status = status, UploadedBy = Owner, UploadedAt = now.AddDays(-8), StartsAt = InternshipStatusSync.StartOfDay(pStart),
                ExpiresAt = InternshipStatusSync.EndOfDay(pEnd), ConfirmedAt = status == "Đã xác nhận" ? now.AddDays(-7) : null
            });
        }
        await db.SaveChangesAsync();
        foreach (var (profile, state) in profiles.Where(x => x.State is 0 or 1 or 3 or 6))
        {
            var appRecord = await db.InternApplications.SingleAsync(x => x.ProfileId == profile.Id);
            db.InternReviewHistories.Add(new InternReviewHistory
            {
                ProfileId = profile.Id, TargetType = "Hồ sơ", TargetId = appRecord.Id, TargetLabel = "Hồ sơ đăng ký",
                Status = appRecord.Status, Note = appRecord.Note, ReviewedBy = Owner, ReviewedAt = now.AddDays(-9)
            });
            foreach (var document in await db.InternDocuments.Where(x => x.ProfileId == profile.Id && x.Type != "Hợp đồng thực tập").ToListAsync())
                db.InternReviewHistories.Add(new InternReviewHistory
                {
                    ProfileId = profile.Id, TargetType = "Tài liệu", TargetId = document.Id, TargetLabel = document.Type,
                    Status = document.Status, Note = document.Note, ReviewedBy = Owner, ReviewedAt = now.AddDays(-10)
                });
        }
        await db.SaveChangesAsync();

        // 6. Phân công đúng phòng ban - chương trình, kể cả khóa đã hoàn thành.
        foreach (var (profile, state) in profiles)
        {
            InternshipProgram? program = state switch { 0 => programs[0], 1 => programs[1], 6 => programs[2], _ => null };
            if (program is null) continue;
            var assignedMentor = state switch { 0 => devMentor, 1 => qaMentor, _ => dataMentor };
            db.InternAssignments.Add(new InternAssignment { ProfileId = profile.Id, ProgramId = program.Id, MentorId = assignedMentor.Id,
                AssignedAt = now.AddDays(-15) });
        }
        await db.SaveChangesAsync();

        // 7. Mỗi ngày thường một ca 08:00-17:00, thêm mốc báo cáo/sinh hoạt.
        foreach (var program in programs.Take(3))
        {
            for (var date = program.StartDate; date <= program.EndDate; date = date.AddDays(1))
                if (Workday(date))
                    db.ProgramSchedules.Add(new ProgramSchedule { ProgramId = program.Id, Date = date,
                        StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), Title = "Ca thực tập 08:00 - 17:00", Kind = "Shift" });
            var milestone = program.Status == "Completed" ? program.EndDate : program.StartDate.AddDays(21);
            db.ProgramSchedules.Add(new ProgramSchedule { ProgramId = program.Id, Date = milestone,
                StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), Title = "Đánh giá tiến độ giữa kỳ", Kind = "Milestone" });
        }
        await db.SaveChangesAsync();

        // 8. Lịch sử chấm công (không tạo check-in giả trong ngày hiện tại).
        var approvedDate = week.AddDays(-5); // thứ tư tuần trước
        var activeProfiles = profiles.Where(x => x.State == 0).Select(x => x.Profile).ToList();
        for (var i = 0; i < activeProfiles.Count; i++)
        {
            var profile = activeProfiles[i];
            for (var diff = 1; diff <= 17; diff++)
            {
                var date = today.AddDays(-diff);
                if (!Workday(date) || date < start || (i == 0 && date == approvedDate)) continue;
                var schedule = await db.ProgramSchedules.FirstOrDefaultAsync(x => x.ProgramId == programs[0].Id && x.Date == date && x.Kind == "Shift");
                if (schedule is null) continue;
                int late = (i + diff) % 6 == 0 ? 13 : 0;
                int early = (i + diff) % 8 == 0 ? 20 : 0;
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    ProfileId = profile.Id, ScheduleId = schedule.Id, Date = date,
                    CheckIn = Local(date, 8, late == 0 ? 0 : late), CheckOut = Local(date, early == 0 ? 17 : 16, early == 0 ? 10 : 40),
                    LateMinutes = late, EarlyMinutes = early, IpAddress = "127.0.0.1", Device = "CareerPortal Demo Data"
                });
            }
        }
        await db.SaveChangesAsync();
        if (activeProfiles.Count > 0)
        {
            db.LeaveRequests.Add(new LeaveRequest { ProfileId = activeProfiles[0].Id, From = approvedDate, To = approvedDate,
                Reason = "Xin nghỉ vì có lịch học tại trường (dữ liệu mẫu).", Status = "Approved", ReviewedAt = now.AddDays(-5), ReviewedBy = Owner });
            db.LeaveRequests.Add(new LeaveRequest { ProfileId = activeProfiles[0].Id, From = week.AddDays(8), To = week.AddDays(8),
                Reason = "Giải quyết việc gia đình (mẫu chờ duyệt).", Status = "Pending" });
        }
        if (activeProfiles.Count > 1)
            db.LeaveRequests.Add(new LeaveRequest { ProfileId = activeProfiles[1].Id, From = week.AddDays(9), To = week.AddDays(9),
                Reason = "Đề nghị nghỉ để tham dự khóa học.", Status = "Rejected", Note = "Ngày này có lịch nghiệm thu quan trọng (minh họa).",
                ReviewedBy = Owner, ReviewedAt = now.AddDays(-1) });
        await db.SaveChangesAsync();

        // 9. Công việc, tiến độ, báo cáo và đánh giá cho Mentor/Intern.
        foreach (var (profile, state) in profiles.Where(x => x.State is 0 or 1))
        {
            var assigned = await db.InternAssignments.SingleAsync(x => x.ProfileId == profile.Id);
            var tasks = new[]
            {
                ("Xây dựng giao diện dashboard", "Tạo bố cục và các thành phần hiển thị trên trang tổng quan.", "High", 75),
                ("Thiết kế REST API và kết nối SQL", "Tạo endpoint, kiểm tra dữ liệu và xử lý phân trang.", "Normal", 30),
                ("Viết test case và kiểm thử", "Xác định các trường hợp đăng nhập, nộp hồ sơ, chấm công.", "Normal", 0),
                ("Tổng hợp tài liệu báo cáo", "Tổng hợp bằng chứng và chuẩn bị thuyết trình.", "Low", 100)
            };
            for (var i = 0; i < tasks.Length; i++)
            {
                var (title, description, priority, progress) = tasks[i];
                if (state == 1 && i > 1) break;
                var task = new InternshipTask { Title = "[DEMO] " + title, Description = description, Priority = priority,
                    Progress = state == 1 ? 0 : progress, Status = state == 1 ? "Todo" : progress == 100 ? "Done" : progress == 0 ? "Todo" : "InProgress",
                    DueDate = today.AddDays(i * 5 + 2), ProfileId = profile.Id, MentorId = assigned.MentorId, ProgramId = assigned.ProgramId,
                    CreatedAt = now.AddDays(-12) };
                db.InternshipTasks.Add(task);
                await db.SaveChangesAsync();
                if (task.Progress > 0)
                {
                    var author = await db.AppUsers.FirstAsync(x => x.ProfileId == profile.Id);
                    db.TaskProgressHistories.Add(new TaskProgressHistory { TaskId = task.Id, Progress = task.Progress, Status = task.Status,
                        Note = "Đã cập nhật tiến độ làm việc (mẫu).", ChangedAt = now.AddDays(-3), ChangedByUserId = author.Id });
                }
            }
            if (state == 0)
            {
                db.WeeklyReports.Add(new WeeklyReport { ProfileId = profile.Id, WeekStart = week.AddDays(-14),
                    Summary = "Hoàn thành phân tích yêu cầu, thiết kế giao diện và tạo bảng dữ liệu thử nghiệm.",
                    Blockers = "Cần làm rõ một số yêu cầu về phân quyền.", Status = "Reviewed",
                    MentorFeedback = "Tiến độ tốt, cần bổ sung kiểm thử các trường hợp lỗi.", SubmittedAt = now.AddDays(-9),
                    ReviewedAt = now.AddDays(-7), ReviewedBy = "Mentor (dữ liệu mẫu)" });
                db.WeeklyReports.Add(new WeeklyReport { ProfileId = profile.Id, WeekStart = week.AddDays(-7),
                    Summary = "Hoàn thiện API quản lý hồ sơ, kiểm thử các biểu mẫu và cập nhật tài liệu Sprint.",
                    Blockers = "Một số API cần tối ưu thời gian phản hồi.", Status = "Submitted", SubmittedAt = now.AddDays(-1) });
                if (profile == activeProfiles.First())
                    db.WeeklyReports.Add(new WeeklyReport { ProfileId = profile.Id, WeekStart = week,
                        Summary = "Đang phát triển chức năng dashboard và chuẩn bị kiểm thử Sprint.",
                        Blockers = "Chờ dữ liệu UAT.", Status = "ChangesRequested", MentorFeedback = "Cần bổ sung bằng chứng kiểm thử.",
                        SubmittedAt = now.AddHours(-5) });
                db.InternshipEvaluations.Add(new InternshipEvaluation { ProfileId = profile.Id, ProgramId = assigned.ProgramId,
                    MentorId = assigned.MentorId, Skills = 3 + profile.Id % 3, Attitude = 4,
                    Communication = 4, Teamwork = 3 + profile.Id % 2,
                    Comment = "[MẪU] Chủ động thực hiện nhiệm vụ; nên tiếp tục cải thiện kỹ năng trình bày.", UpdatedAt = now.AddDays(-1) });
            }
        }
        await db.SaveChangesAsync();

        // 10. Thông báo + lịch sử email giả lập; KHÔNG đưa vào hàng đợi gửi thật.
        if (activeProfiles.Count > 0)
        {
            foreach (var p in activeProfiles.Take(3))
            {
                db.PortalNotifications.Add(new PortalNotification { ProfileId = p.Id, Title = "[DEMO] Nhiệm vụ mới",
                    Message = "Mentor đã giao nhiệm vụ và cập nhật lịch làm việc.", Link = "thuc-tap-sinh.html?view=attendance&attendanceTab=program",
                    CreatedAt = now.AddDays(-2) });
                db.PortalNotifications.Add(new PortalNotification { ProfileId = p.Id, Title = "[DEMO] Báo cáo tuần",
                    Message = "Báo cáo của bạn đã được mentor phản hồi.", Link = "thuc-tap-sinh.html?view=attendance&attendanceTab=program",
                    IsRead = true, CreatedAt = now.AddDays(-6) });
            }
        }
        var sampleMentorAccount = await db.AppUsers.FindAsync(devMentor.UserId);
        if (sampleMentorAccount is not null)
        {
            db.PortalNotifications.Add(new PortalNotification { AccountId = sampleMentorAccount.Id, Title = "[DEMO] Báo cáo chờ phản hồi",
                Message = "Có báo cáo tuần mới của thực tập sinh được phân công.", Link = "mentor.html", CreatedAt = now.AddHours(-1) });
            db.PortalNotifications.Add(new PortalNotification { AccountId = sampleMentorAccount.Id, Title = "[DEMO] Thực tập sinh mới",
                Message = "Đã có thực tập sinh được phân công vào chương trình Full-stack.", Link = "mentor.html", IsRead = true, CreatedAt = now.AddDays(-4) });
        }
        db.MailJobs.AddRange(
            new MailJob { EventKey = "demo:approval:1", Recipient = "tss01" + Suffix, Subject = "[DỮ LIỆU MẪU] Kết quả xét duyệt thực tập",
                Body = "Thư giả lập để hiển thị trạng thái trên giao diện; không gửi ra ngoài.", Status = "Sent", Attempts = 1,
                SentAt = now.AddDays(-8), DueAt = now.AddDays(-8) },
            new MailJob { EventKey = "demo:report:1", Recipient = "tss02" + Suffix, Subject = "[DỮ LIỆU MẪU] Phản hồi báo cáo tuần",
                Body = "Thư giả lập để hiển thị giao diện, không gửi ra ngoài.", Status = "Sent", Attempts = 1,
                SentAt = now.AddDays(-2), DueAt = now.AddDays(-2) },
            new MailJob { EventKey = "demo:failed:1", Recipient = "tss06" + Suffix, Subject = "[DỮ LIỆU MẪU] Minh họa gửi email lỗi",
                Body = "Không có hành động gửi mail thực tế.", Status = "Failed", Attempts = 5,
                LastError = "Lỗi thử nghiệm - chưa kết nối kênh gửi thư (dữ liệu mẫu).", DueAt = now.AddDays(-1) }
        );
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        app.Logger.LogInformation("Đã nạp dữ liệu DEMO: {Profiles} hồ sơ mẫu, {Programs} chương trình, hồ sơ/tài liệu/chấm công/nhiệm vụ.",
            profiles.Count, programs.Length);

        void AddDocument(InternProfile profile, string type, string fileName, byte[] bytes, string status)
        {
            db.InternDocuments.Add(new InternDocument
            {
                ProfileId = profile.Id, Type = type, FileName = fileName, ContentType = "application/pdf", Content = bytes,
                Status = status, Note = status == "Từ chối" ? "Tài liệu chưa đúng định dạng yêu cầu (minh họa)." : null,
                UploadedBy = profile.Name, UploadedAt = now.AddDays(-16),
                ReviewedBy = status == "Chờ duyệt" ? null : Owner,
                ReviewedAt = status == "Chờ duyệt" ? null : now.AddDays(-10)
            });
        }
    }
}

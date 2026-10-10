using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Synthetic UI data, only in the disposable permission-check database.
internal static class UiFixture
{
    internal static async Task Seed(CareerDbContext db, string password)
    {
        db.ChangeTracker.Clear();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var department = new Department { Name = "Phát triển phần mềm" };
        var program = new InternshipProgram { Name = "Thực tập Full-stack · Khóa thu", Department = department, Capacity = 20,
            StartDate = today.AddDays(-14), EndDate = today.AddDays(76), Status = "Active", Description = "Phát triển ứng dụng web, làm việc nhóm và thực hành dự án thực tế." };
        var mentorUser = Create("Trần Minh Anh", "ui.mentor@example.test", "Mentor");
        var mentor = new Mentor { User = mentorUser, Department = department, Capacity = 8 };
        db.Mentors.Add(mentor); db.InternshipPrograms.Add(program);
        db.InternshipPrograms.Add(new InternshipProgram { Name = "Kiểm thử phần mềm · Khóa mới", Department = department, Capacity = 12,
            StartDate = today.AddDays(14), EndDate = today.AddDays(104), Status = "Open", Description = "Kiểm thử chức năng và tự động hóa." });
        var names = new[] { "Nguyễn Hoàng An", "Lê Ngọc Mai", "Phạm Gia Huy", "Đặng Thu Hà" };
        for (var i = 0; i < names.Length; i++)
        {
            var profile = new InternProfile { Name = names[i], StudentId = $"SV20260{i + 1}", Email = $"ui.intern{i + 1}@example.test",
                School = i % 2 == 0 ? "Đại học Công nghệ" : "Đại học Bách khoa", Major = i % 2 == 0 ? "Công nghệ thông tin" : "Kỹ thuật phần mềm",
                PasswordHash = "", EmailVerified = true, Status = i < 2 ? "Đang thực tập" : "Chờ duyệt", StartDate = program.StartDate, EndDate = program.EndDate };
            profile.PasswordHash = new PasswordHasher<InternProfile>().HashPassword(profile, password);
            var user = Create(names[i], profile.Email, "Intern"); user.Profile = profile;
            db.AppUsers.Add(user);
            db.InternApplications.Add(new InternApplication { Profile = profile, Status = i < 2 ? "Đã duyệt" : "Chờ duyệt" });
            if (i < 2) db.InternAssignments.Add(new InternAssignment { Profile = profile, Program = program, Mentor = mentor });
            if (i == 0)
            {
                db.LeaveRequests.Add(new LeaveRequest { Profile = profile, From = today.AddDays(3), To = today.AddDays(3), Reason = "Lịch bảo vệ đồ án tại trường", Status = "Pending" });
                var pastShift = new ProgramSchedule { Program = program, Date = today.AddDays(-1), StartTime = new(8,0), EndTime = new(17,0), Title = "Thực hành dự án theo nhóm" };
                db.AttendanceRecords.Add(new AttendanceRecord { Profile = profile, Schedule = pastShift, Date = pastShift.Date,
                    CheckIn = new DateTimeOffset(pastShift.Date.ToDateTime(new(8,0)), TimeSpan.FromHours(7)), CheckOut = new DateTimeOffset(pastShift.Date.ToDateTime(new(17,0)), TimeSpan.FromHours(7)) });
            }
        }
        db.ProgramSchedules.Add(new ProgramSchedule { Program = program, Date = today, StartTime = new(8,0), EndTime = new(17,0), Title = "Phát triển tính năng & review mã nguồn" });
        db.ProgramSchedules.Add(new ProgramSchedule { Program = program, Date = today.AddDays(7), StartTime = new(14,0), EndTime = new(16,0), Title = "Báo cáo tiến độ giữa kỳ", Kind = "Milestone" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        AppUser Create(string name, string email, string role)
        {
            var user = new AppUser { Name = name, Email = email, PasswordHash = "" };
            user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
            user.UserRoles.Add(new UserRole { RoleName = role }); return user;
        }
    }
}

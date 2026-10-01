using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

public static class PortalReports
{
    public static void MapPortalReports(this WebApplication app)
    {
        app.MapPost("/api/admin/backups/{name}/restore", async (string name, HttpContext c, CareerDbContext db, IDataProtectionProvider protection, IWebHostEnvironment env) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            if (Path.GetFileName(name) != name || !name.EndsWith(".backup")) return Results.BadRequest();
            if (!File.Exists(Path.Combine(PortalJobs.BackupFolder(env), name))) return Results.NotFound();
            var database = await BackupRestore.Restore(name, db, protection, env); return Results.Ok(new { database, message = "Đã phục hồi vào database mới. Database hiện tại không bị thay đổi." });
        });
        app.MapGet("/api/reports/summary", async (HttpContext c, CareerDbContext db, string? school, string? major, int? programId) =>
        {
            if (!PortalSecurity.Manager(c.User)) return Results.Forbid();
            var profiles = await Profiles(db, school, major, programId).ToListAsync(); var ids = profiles.Select(x => x.Id).ToArray();
            var work = await db.WorkItems.Where(x => ids.Contains(x.ProfileId ?? 0) || x.Kind == "mentors").AsNoTracking().ToListAsync();
            var completed = profiles.Count(x => x.Status == "Đã hoàn thành");
            return Results.Ok(new {
                total = profiles.Count, completed, completionRate = profiles.Count == 0 ? 0 : Math.Round(100m * completed / profiles.Count, 2),
                schools = profiles.GroupBy(x => new { x.School, x.Major }).Select(g => new { g.Key.School, g.Key.Major, count = g.Count() }),
                mentors = work.Where(x => x.Kind == "mentors").Select(m => new { m.Title, capacity = m.Amount, assigned = work.Count(x => x.Kind == "assignments" && x.MentorId == m.Id) }),
                evaluations = work.Where(x => x.Kind == "evaluations").Select(x => new { x.ProfileId, x.Title, skill = x.Amount, attitude = x.Progress, x.Detail }),
                attendance = profiles.Select(p => new { p.Name, days = work.Count(x => x.Kind == "attendance" && x.ProfileId == p.Id), hours = Math.Round(work.Where(x => x.Kind == "attendance" && x.ProfileId == p.Id && x.Start != null && x.End != null).Sum(x => (x.End!.Value - x.Start!.Value).TotalHours), 2), leave = work.Count(x => x.Kind == "leave" && x.ProfileId == p.Id && x.Status == "Đã duyệt") })
            });
        });
        app.MapGet("/api/reports/export.xlsx", async (HttpContext c, CareerDbContext db, string? school, string? major, int? programId) =>
        {
            if (!PortalSecurity.Manager(c.User)) return Results.Forbid();
            var profiles = await Profiles(db, school, major, programId).ToListAsync();
            var evaluations = await db.WorkItems.Where(x => x.Kind == "evaluations").ToListAsync();
            var rows = new List<string[]> { new[] { "Mã sinh viên", "Họ tên", "Trường", "Ngành", "Trạng thái", "Điểm kỹ năng", "Điểm thái độ", "Nhận xét" } };
            foreach (var p in profiles) { var e = evaluations.Where(x => x.ProfileId == p.Id).OrderByDescending(x => x.Id).FirstOrDefault(); rows.Add([p.StudentId, p.Name, p.School, p.Major, p.Status, e?.Amount.ToString() ?? "", e?.Progress.ToString() ?? "", e?.Detail ?? ""]); }
            return Results.File(Xlsx(rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Bao-cao-thuc-tap.xlsx");
        });
        app.MapPost("/api/admin/backups", async (HttpContext c, CareerDbContext db, IDataProtectionProvider protection, IWebHostEnvironment env) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            var file = await PortalJobs.Backup(db, protection, PortalJobs.BackupFolder(env)); return Results.Ok(new { file });
        });
        app.MapGet("/api/admin/backups", (HttpContext c, IWebHostEnvironment env) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid(); var dir = PortalJobs.BackupFolder(env);
            return Results.Ok(Directory.Exists(dir) ? Directory.GetFiles(dir, "*.backup").Select(Path.GetFileName) : []);
        });
        app.MapGet("/api/admin/backups/{name}", (string name, HttpContext c, IWebHostEnvironment env) => {
            if (!c.User.IsInRole("Admin")) return Results.Forbid(); if (Path.GetFileName(name) != name || !name.EndsWith(".backup")) return Results.BadRequest();
            var path = Path.Combine(PortalJobs.BackupFolder(env), name); return File.Exists(path) ? Results.File(File.ReadAllBytes(path), "application/octet-stream", name) : Results.NotFound();
        });
        app.MapPost("/api/admin/integrations/hrm", async (HrmPerson[] people, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            if (people.Length > 500 || people.Any(p => string.IsNullOrWhiteSpace(p.EmployeeCode) || string.IsNullOrWhiteSpace(p.Name) || !System.Net.Mail.MailAddress.TryCreate(p.Email, out _))) return Results.BadRequest(new { message = "Tối đa 500 dòng; mã, tên và email hợp lệ là bắt buộc." });
            await using var tx = await db.Database.BeginTransactionAsync();
            foreach (var p in people)
            {
                var email = p.Email.Trim().ToLowerInvariant(); var code = "HRM-" + p.EmployeeCode.Trim();
                var profile = await db.InternProfiles.FirstOrDefaultAsync(x => x.StudentId == code);
                if (await db.InternProfiles.AnyAsync(x => x.Email == email && x.StudentId != code)) return Results.Conflict(new { message = "Email trùng hồ sơ khác: " + email });
                if (profile is null) { profile = new InternProfile { Name = p.Name, StudentId = code, Email = email, School = p.School, Major = p.Major, PasswordHash = "DISABLED_HRM_IMPORT" }; db.InternProfiles.Add(profile); }
                else { profile.Name = p.Name; profile.Email = email; profile.School = p.School; profile.Major = p.Major; }
                await db.SaveChangesAsync();
            }
            await tx.CommitAsync(); return Results.Ok(new { processed = people.Length });
        });
        app.MapPost("/api/admin/integrations/attendance", async (AttendanceEvent e, HttpContext c, CareerDbContext db) =>
        {
            if (!c.User.IsInRole("Admin")) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(e.EventId) || e.EventId.Length > 180 || e.End < e.Start || e.Start > DateTimeOffset.UtcNow.AddMinutes(5)) return Results.BadRequest();
            var p = await db.InternProfiles.FirstOrDefaultAsync(x => x.StudentId == e.StudentId); if (p is null) return Results.NotFound();
            var key = "device:" + e.EventId; if (await db.WorkItems.AnyAsync(x => x.UniqueKey == key)) return Results.Ok(new { duplicate = true });
            db.WorkItems.Add(new WorkItem { Kind = "attendance", Title = "QR/thẻ " + e.EventId, ProfileId = p.Id, Start = e.Start, End = e.End, UniqueKey = key, Status = e.End is null ? "Đang làm" : "Đã kết thúc" });
            await db.SaveChangesAsync(); return Results.Ok(new { duplicate = false });
        });
    }
    static IQueryable<InternProfile> Profiles(CareerDbContext db, string? school, string? major, int? programId) => db.InternProfiles.AsNoTracking()
        .Where(x => (school == null || x.School.Contains(school)) && (major == null || x.Major.Contains(major)) && (programId == null || db.WorkItems.Any(a => a.Kind == "assignments" && a.ProfileId == x.Id && a.ProgramId == programId)));
    public static byte[] Xlsx(List<string[]> rows)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            void Add(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), Encoding.UTF8); writer.Write(content); }
            Add("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Add("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Add("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Báo cáo\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Add("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            Add("xl/worksheets/sheet1.xml", new XElement(ns + "worksheet", new XElement(ns + "sheetData", rows.Select((row, i) => new XElement(ns + "row", new XAttribute("r", i + 1), row.Select((value, col) => new XElement(ns + "c", new XAttribute("r", $"{(char)('A' + col)}{i + 1}"), new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", value)))))))).ToString());
        }
        return stream.ToArray();
    }
}
public record HrmPerson(string EmployeeCode, string Name, string Email, string School, string Major);
public record AttendanceEvent(string EventId, string StudentId, DateTimeOffset Start, DateTimeOffset? End);

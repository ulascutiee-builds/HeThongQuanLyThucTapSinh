using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
var connection=Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION")??throw new Exception("TEST_DATABASE_CONNECTION must reference an isolated test database.");
if(!connection.Contains("_Test_",StringComparison.OrdinalIgnoreCase))throw new Exception("Report fixture requires a database name containing _Test_.");
var options=new DbContextOptionsBuilder<CareerDbContext>().UseSqlServer(connection).Options;
await using var db=new CareerDbContext(options);
var tag=Guid.NewGuid().ToString("N")[..8];var today=Sprint2Endpoints.Today;
var mentor=await db.Mentors.FirstAsync();
var profile=new InternProfile{Name="Report fixture "+tag,StudentId="REP"+tag,Email=tag+"@report.example.test",School="Test",Major="Test",PasswordHash="fixture-disabled",Status="Đang thực tập",StartDate=today.AddDays(-4),EndDate=today};
var program=new InternshipProgram{Name="Report "+tag,DepartmentId=mentor.DepartmentId,Description="Deterministic test fixture",Capacity=10,StartDate=today.AddDays(-4),EndDate=today,Status="Active"};
db.AddRange(profile,program);await db.SaveChangesAsync();
db.InternAssignments.Add(new(){ProfileId=profile.Id,ProgramId=program.Id,MentorId=mentor.Id});
var shifts=Enumerable.Range(-4,5).Select(i=>new ProgramSchedule{ProgramId=program.Id,Date=today.AddDays(i),StartTime=new(9,0),EndTime=new(17,0),Title="Fixture shift",Kind="Shift"}).ToList();
db.ProgramSchedules.AddRange(shifts);await db.SaveChangesAsync();
DateTimeOffset At(DateOnly d,int hour,int minute=0)=>new(d.ToDateTime(new TimeOnly(hour,minute)),TimeSpan.FromHours(7));
db.AttendanceRecords.AddRange(
 new(){ProfileId=profile.Id,ScheduleId=shifts[0].Id,Date=shifts[0].Date,CheckIn=At(shifts[0].Date,9),CheckOut=At(shifts[0].Date,17)},
 new(){ProfileId=profile.Id,ScheduleId=shifts[1].Id,Date=shifts[1].Date,CheckIn=At(shifts[1].Date,9,30),CheckOut=At(shifts[1].Date,17),LateMinutes=30},
 new(){ProfileId=profile.Id,ScheduleId=shifts[2].Id,Date=shifts[2].Date,CheckIn=At(shifts[2].Date,9),CheckOut=At(shifts[2].Date,16,30),EarlyMinutes=30});
db.LeaveRequests.Add(new(){ProfileId=profile.Id,From=today.AddDays(-1),To=today.AddDays(-1),Reason="Approved test leave",Status="Approved",ReviewedBy="Test"});
await db.SaveChangesAsync();
using var client=new HttpClient(new HttpClientHandler{CookieContainer=new CookieContainer()}){BaseAddress=new(Environment.GetEnvironmentVariable("SPRINT1_URL")??"http://localhost:5135")};
var login=await client.PostAsJsonAsync("/api/auth/login",new{identity="hr@sprint1.local",password=Environment.GetEnvironmentVariable("SPRINT1_TEST_HR_PASSWORD")});login.EnsureSuccessStatusCode();
var result=await client.GetFromJsonAsync<JsonElement>($"/api/attendance/report?from={today.AddDays(-4):yyyy-MM-dd}&to={today:yyyy-MM-dd}&profileId={profile.Id}");
var row=result.GetProperty("summary")[0];
void Check(bool value,string label){if(!value)throw new Exception(label);}
Check(row.GetProperty("workDays").GetInt32()==3,"Work days");Check(row.GetProperty("lateDays").GetInt32()==1,"Late days");Check(row.GetProperty("earlyDays").GetInt32()==1,"Early days");Check(row.GetProperty("leaveDays").GetInt32()==1,"Leave days");Check(row.GetProperty("hours").GetDouble()==23,"Hours total must be 23");
Check(result.GetProperty("details").EnumerateArray().Count(x=>x.GetProperty("leave").GetBoolean())==1,"Approved leave detail");
var expectedAbsent=TimeOnly.FromDateTime(Sprint2Endpoints.LocalNow.DateTime)>=new TimeOnly(17,0)?1:0;
Check(row.GetProperty("absentDays").GetInt32()==expectedAbsent,"Only finished shifts may count as absence");
Console.WriteLine("PASS ReportChecks: seven deterministic aggregate checks (3 work days, 23 hours, one late/early/leave day, absence after shift ends). Fixture retained in isolated test DB.");

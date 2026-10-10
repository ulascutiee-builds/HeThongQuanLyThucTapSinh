using System.ComponentModel.DataAnnotations;

public sealed class Department
{
    public int Id { get; set; }
    public required string Name { get; set; }
}
public sealed class InternshipProgram
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Description { get; set; } = "";
    public int Capacity { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Active";
}
public sealed class Mentor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public AppUser? User { get; set; }
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int Capacity { get; set; } = 5;
}
public sealed class InternAssignment
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public InternProfile? Profile { get; set; }
    public int ProgramId { get; set; }
    public InternshipProgram? Program { get; set; }
    public int MentorId { get; set; }
    public Mentor? Mentor { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class ProgramSchedule
{
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public InternshipProgram? Program { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public required string Title { get; set; }
    public string Kind { get; set; } = "Shift";
}
public sealed class AttendanceRecord
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public InternProfile? Profile { get; set; }
    public int ScheduleId { get; set; }
    public ProgramSchedule? Schedule { get; set; }
    public DateOnly Date { get; set; }
    public DateTimeOffset CheckIn { get; set; }
    public DateTimeOffset? CheckOut { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyMinutes { get; set; }
    public string IpAddress { get; set; } = "";
    public string Device { get; set; } = "";
}
public sealed class LeaveRequest
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public InternProfile? Profile { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public required string Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Note { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
public sealed record DepartmentInput([property:Required,MaxLength(120)] string Name);
public sealed record ProgramInput(
    [property:Required,MaxLength(160)] string Name, int DepartmentId,
    [property:MaxLength(2000)] string Description, [property:Range(1,10000)] int Capacity,
    DateOnly StartDate, DateOnly EndDate, string Status = "Active");
public sealed record MentorInput(int UserId, int DepartmentId, [property:Range(1,1000)] int Capacity);
public sealed record AssignmentInput(int ProfileId, int ProgramId, int MentorId);
public sealed record ChangeMentorInput(int MentorId);
public sealed record ScheduleInput(int ProgramId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
    [property:Required,MaxLength(200)] string Title, string Kind = "Shift");
public sealed record LeaveInput(DateOnly From, DateOnly To, [property:Required,MaxLength(1000)] string Reason);
public sealed record LeaveDecision(string Status, [property:MaxLength(1000)] string? Note);

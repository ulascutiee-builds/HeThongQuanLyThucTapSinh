// Source: backend/CareerPortal.Api/Sprint2Entities.cs | lines 40-50 | Excel row(s): 33, 34
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

// Source: backend/CareerPortal.Api/Sprint2Entities.cs | lines 87-88 | Excel row(s): 33, 34
public sealed record ScheduleInput(int ProgramId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
    [property:Required,MaxLength(200)] string Title, string Kind = "Shift");


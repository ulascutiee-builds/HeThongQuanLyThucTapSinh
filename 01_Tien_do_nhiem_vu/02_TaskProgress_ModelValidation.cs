// Source: backend/CareerPortal.Api/Sprint3Entities.cs | lines 2-3 | Excel row(s): 8, 9
public sealed class InternshipTask { public int Id { get; set; } public required string Title { get; set; } public string Description { get; set; } = ""; public DateOnly? DueDate { get; set; } public string Priority { get; set; } = "Normal"; public string Status { get; set; } = "Todo"; public int Progress { get; set; } public int MentorId { get; set; } public Mentor? Mentor { get; set; } public int ProfileId { get; set; } public InternProfile? Profile { get; set; } public int? ProgramId { get; set; } public InternshipProgram? Program { get; set; } public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; }
public sealed class TaskProgressHistory { public int Id { get; set; } public int TaskId { get; set; } public InternshipTask? Task { get; set; } public int Progress { get; set; } public string Status { get; set; } = "Todo"; public string? Note { get; set; } public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow; public int ChangedByUserId { get; set; } }

// Source: backend/CareerPortal.Api/Sprint3Entities.cs | lines 6-6 | Excel row(s): 8, 9
public sealed record TaskProgressInput([property:Range(0,100)] int Progress, string Status, [property:MaxLength(1000)] string? Note);


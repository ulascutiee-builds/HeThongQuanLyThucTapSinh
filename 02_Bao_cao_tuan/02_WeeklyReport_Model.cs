// Source: backend/CareerPortal.Api/Sprint3Entities.cs | lines 4-4 | Excel row(s): 14, 15
public sealed class WeeklyReport { public int Id { get; set; } public int ProfileId { get; set; } public InternProfile? Profile { get; set; } public DateOnly WeekStart { get; set; } public required string Summary { get; set; } public string? Blockers { get; set; } public string? EvidenceUrl { get; set; } public string Status { get; set; } = "Submitted"; public string? MentorFeedback { get; set; } public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow; public DateTimeOffset? ReviewedAt { get; set; } public string? ReviewedBy { get; set; } }

// Source: backend/CareerPortal.Api/Sprint3Entities.cs | lines 7-7 | Excel row(s): 14, 15
public sealed record WeeklyReportInput(DateOnly WeekStart, [property:Required,MaxLength(6000)] string Summary, [property:MaxLength(2000)] string? Blockers, [property:MaxLength(500)] string? EvidenceUrl);


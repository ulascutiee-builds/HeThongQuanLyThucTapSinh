// Source: backend/CareerPortal.Api/Sprint2Entities.cs | lines 66-78 | Excel row(s): 39
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

// Source: backend/CareerPortal.Api/Sprint2Entities.cs | lines 89-89 | Excel row(s): 39
public sealed record LeaveInput(DateOnly From, DateOnly To, [property:Required,MaxLength(1000)] string Reason);


using System.ComponentModel.DataAnnotations;

public sealed record RegisterInternRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, MaxLength(40)] public required string StudentId { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    [Required, MinLength(8), MaxLength(200)] public required string Password { get; init; }
}

public sealed record LoginInternRequest
{
    [Required, MaxLength(160)] public required string Identity { get; init; }
    [Required, MaxLength(200)] public required string Password { get; init; }
}

public sealed record UpdateInternProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
}

public sealed record CreateHrProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, MaxLength(40)] public required string StudentId { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string Status { get; init; } = "Đang thực tập";
}

public sealed record UpdateHrProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    [Required] public required string Status { get; init; }
}

public sealed record InternProfileResponse(
    int Id, string Name, string StudentId, string Email, string School, string Major,
    string Status, DateOnly? StartDate, DateOnly? EndDate, DateTimeOffset CreatedAt);

public sealed record InternDocumentResponse(
    int Id, int ProfileId, string Type, string FileName, string ContentType,
    long Size, string Status, string? Note, DateTimeOffset UploadedAt, DateTimeOffset? ConfirmedAt);

public sealed record InternApplicationResponse(
    int Id, int ProfileId, string Status, DateTimeOffset AppliedAt);

public sealed record InternReviewHistoryResponse(
    int Id, int ProfileId, string TargetType, int TargetId, string TargetLabel,
    string Status, string? Note, DateTimeOffset ReviewedAt);

public sealed record InternWorkspaceResponse(
    InternProfileResponse Profile,
    InternApplicationResponse? Application,
    IReadOnlyList<InternDocumentResponse> Documents,
    IReadOnlyList<InternReviewHistoryResponse> ReviewHistory);

public sealed record HrDashboardResponse(
    IReadOnlyList<InternProfileResponse> Profiles,
    IReadOnlyList<InternApplicationResponse> Applications,
    IReadOnlyList<InternDocumentResponse> Documents,
    IReadOnlyList<InternReviewHistoryResponse> ReviewHistory);

public sealed record DecideStatusRequest
{
    [Required] public required string Status { get; init; }
    [MaxLength(1000)] public string? Note { get; init; }
}

// ──────────────────────────────────────────────
// Feature 13 – POST /api/assignments
// ──────────────────────────────────────────────

public sealed record CreateAssignmentRequest
{
    /// <summary>ID thực tập sinh (InternProfile.Id)</summary>
    [Required] public required int ProfileId { get; init; }

    /// <summary>ID WorkItem kind="programs"</summary>
    [Required] public required int ProgramId { get; init; }

    /// <summary>ID WorkItem kind="mentors"</summary>
    [Required] public required int MentorId { get; init; }
}

// ──────────────────────────────────────────────
// Feature 14 – GET /api/interns/me/schedule
// ──────────────────────────────────────────────

public sealed record InternScheduleResponse(
    ScheduleSummary? Program,
    IReadOnlyList<ScheduleItem> Shifts,
    IReadOnlyList<ScheduleItem> Tasks,
    IReadOnlyList<ScheduleItem> Milestones);

public sealed record ScheduleSummary(
    string ProgramName,
    string Department,
    string MentorName,
    DateTimeOffset? Start,
    DateTimeOffset? End);

public sealed record ScheduleItem(
    int Id,
    string Type,
    string Title,
    string Detail,
    DateTimeOffset? Start,
    DateTimeOffset? End,
    string Status,
    int Progress);

// ──────────────────────────────────────────────
// Feature 15 – PATCH /api/tasks/{id}/progress
// ──────────────────────────────────────────────

public sealed record UpdateTaskProgressRequest
{
    /// <summary>Tiến độ hoàn thành, giá trị 0–100</summary>
    [Required, Range(0, 100)] public required int Progress { get; init; }
}

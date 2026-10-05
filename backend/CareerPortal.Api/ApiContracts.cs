using System.ComponentModel.DataAnnotations;

public sealed record RegisterInternRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, MaxLength(40)] public required string StudentId { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [Required, RegularExpression(@"^\+?[0-9]{9,15}$"), MaxLength(16)] public required string Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    [Required, MinLength(8), MaxLength(200)] public required string Password { get; init; }
}

public sealed record LoginInternRequest
{
    [Required, EmailAddress, MaxLength(160)] public required string Identity { get; init; }
    [Required, MaxLength(200)] public required string Password { get; init; }
}

public sealed record UpdateInternProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, MaxLength(40)] public required string StudentId { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [RegularExpression(@"^\+?[0-9]{9,15}$"), MaxLength(16)] public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
}

public sealed record CreateHrProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, MaxLength(40)] public required string StudentId { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [RegularExpression(@"^\+?[0-9]{9,15}$"), MaxLength(16)] public string? Phone { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    [Required] public DateOnly? DateOfBirth { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string Status { get; init; } = "Đang thực tập";
}

public sealed record UpdateHrProfileRequest
{
    [Required, MaxLength(120)] public required string Name { get; init; }
    [Required, EmailAddress, MaxLength(160)] public required string Email { get; init; }
    [RegularExpression(@"^\+?[0-9]{9,15}$"), MaxLength(16)] public string? Phone { get; init; }
    [Required, MaxLength(160)] public required string School { get; init; }
    [Required, MaxLength(120)] public required string Major { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    [Required] public required string Status { get; init; }
}

public sealed record InternProfileResponse(
    int Id, string Name, string StudentId, string Email, string School, string Major,
    string Status, DateOnly? StartDate, DateOnly? EndDate, DateTimeOffset CreatedAt, string? Phone, bool EmailVerified,
    DateOnly? DateOfBirth);

public sealed record InternDocumentResponse(
    int Id, int ProfileId, string Type, string FileName, string ContentType,
    long Size, string Status, string? Note, DateTimeOffset UploadedAt, DateTimeOffset? ConfirmedAt, int Version, bool IsCurrent, string UploadedBy, string? ReviewedBy, DateTimeOffset? ReviewedAt, DateTimeOffset? StartsAt, DateTimeOffset? ExpiresAt);

public sealed record InternApplicationResponse(
    int Id, int ProfileId, string Status, DateTimeOffset AppliedAt, string? ReviewedBy, DateTimeOffset? ReviewedAt, string? Note);

public sealed record InternReviewHistoryResponse(
    int Id, int ProfileId, string TargetType, int TargetId, string TargetLabel,
    string Status, string? Note, DateTimeOffset ReviewedAt, string ReviewedBy);

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

public sealed class InternProfile
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string StudentId { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool EmailVerified { get; set; }
    public string? VerificationHash { get; set; }
    public DateTimeOffset? VerificationExpires { get; set; }
    public required string School { get; set; }
    public required string Major { get; set; }
    public required string PasswordHash { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "Chờ hồ sơ";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<InternDocument> Documents { get; set; } = [];
    public InternApplication? Application { get; set; }
}

public sealed class InternDocument
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public required string Type { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
    public string Status { get; set; } = "Chờ duyệt";
    public string? Note { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConfirmedAt { get; set; }
    public int Version { get; set; } = 1;
    public bool IsCurrent { get; set; } = true;
    public string UploadedBy { get; set; } = "";
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public InternProfile? Profile { get; set; }
}

public sealed class InternApplication
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string Status { get; set; } = "Chờ duyệt";
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? Note { get; set; }
    public int SubmissionVersion { get; set; } = 1;
    public InternProfile? Profile { get; set; }
}

public sealed class InternReviewHistory
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTimeOffset ReviewedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ReviewedBy { get; set; } = "";
    public InternProfile? Profile { get; set; }
}

public sealed class InternSchedule
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string? Detail { get; set; }
    public string Status { get; set; } = "Đã lên lịch";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InternProfile? Profile { get; set; }
    public InternAttendance? Attendance { get; set; }
}

public sealed class InternAttendance
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int ProfileId { get; set; }
    public DateOnly WorkDate { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset? ClockInAt { get; set; }
    public DateTimeOffset? ClockOutAt { get; set; }
    public string? Note { get; set; }
    public required string UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public InternSchedule? Schedule { get; set; }
    public InternProfile? Profile { get; set; }
}

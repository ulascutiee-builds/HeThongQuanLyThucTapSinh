public sealed class InternProfile
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string StudentId { get; set; }
    public required string Email { get; set; }
    public required string School { get; set; }
    public required string Major { get; set; }
    public required string PasswordHash { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "Chờ hồ sơ";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<InternDocument> Documents { get; set; } = [];
    public List<Allowance> Allowances { get; set; } = [];
    public InternApplication? Application { get; set; }
}

public sealed class Allowance
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public InternProfile Profile { get; set; } = null!;
    [MaxLength(120)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string Note { get; set; } = "";
    public decimal Amount { get; set; }
    [MaxLength(3)] public string Currency { get; set; } = "VND";
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    [MaxLength(40)] public string PaymentStatus { get; set; } = "Chưa thanh toán";
    [MaxLength(120)] public string CreatedBy { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(120)] public string UpdatedBy { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] Version { get; set; } = [];
    public ICollection<AllowanceHistory> History { get; set; } = new List<AllowanceHistory>();
}

public sealed class AllowanceHistory
{
    public int Id { get; set; }
    public int AllowanceId { get; set; }
    public Allowance Allowance { get; set; } = null!;
    [MaxLength(120)] public string ChangedBy { get; set; } = "";
    [MaxLength(40)] public string Action { get; set; } = "";
    [MaxLength(2000)] public string Comment { get; set; } = "";
    public string PreviousValuesJson { get; set; } = "{}";
    public string NewValuesJson { get; set; } = "{}";
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public InternProfile? Profile { get; set; }
}

public sealed class InternApplication
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public string Status { get; set; } = "Chờ duyệt";
    public DateTimeOffset AppliedAt { get; set; } = DateTimeOffset.UtcNow;
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
    public InternProfile? Profile { get; set; }
}

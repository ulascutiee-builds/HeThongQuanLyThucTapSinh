using System.ComponentModel.DataAnnotations;

public sealed class PortalAccount
{
    public int Id { get; set; }
    [MaxLength(160)] public string Email { get; set; } = "";
    [MaxLength(120)] public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Mentor";
    public bool Active { get; set; } = true;
    public string Permissions { get; set; } = "";
    public int? InternProfileId { get; set; }
    public InternProfile? InternProfile { get; set; }
    [MaxLength(64)] public string? ActivationTokenHash { get; set; }
    public DateTimeOffset? ActivationExpiresAt { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}

// One extensible workflow table, with kind-specific rules in PortalWorkflow.
public sealed class WorkItem
{
    public int Id { get; set; }
    [MaxLength(40)] public string Kind { get; set; } = "";
    [MaxLength(240)] public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public int? ProfileId { get; set; }
    public int? ProgramId { get; set; }
    public int? MentorId { get; set; }
    public string Department { get; set; } = "";
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public string Status { get; set; } = "Mới";
    public int Progress { get; set; }
    public decimal Amount { get; set; }
    public int Capacity { get; set; }
    public string Feedback { get; set; } = "";
    public string History { get; set; } = "";
    public string FileName { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Attachment { get; set; }
    [MaxLength(240)] public string? UniqueKey { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] Version { get; set; } = [];
}
public sealed class PortalAudit
{
    public long Id { get; set; }
    public string Actor { get; set; } = "";
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
    public int StatusCode { get; set; }
    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class MailJob
{
    public int Id { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = "Queued";
    public int Attempts { get; set; }
    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.UtcNow;
    public string Error { get; set; } = "";
}
public record AccountInput(
    string Name,
    string Email,
    string Role,
    string Password = "",
    bool Active = true,
    string Permissions = "",
    int? InternProfileId = null);
public record ActivateAccountInput(string Token, string Password);
public record ActionInput(string Action, int Progress = 0, string Feedback = "", decimal Amount = 0);

public static class ApiMapping
{
    public static InternProfileResponse ToResponse(this InternProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.StudentId,
        profile.Email,
        profile.School,
        profile.Major,
        profile.Status,
        profile.StartDate,
        profile.EndDate,
        profile.CreatedAt, profile.Phone, profile.EmailVerified, profile.DateOfBirth);

    public static InternDocumentResponse ToResponse(this InternDocument document) => new(
        document.Id,
        document.ProfileId,
        document.Type,
        document.FileName,
        document.ContentType,
        document.Content.LongLength,
        document.Status,
        document.Note,
        document.UploadedAt,
        document.ConfirmedAt, document.Version, document.IsCurrent, document.UploadedBy, document.ReviewedBy, document.ReviewedAt, document.ExpiresAt);

    public static InternApplicationResponse ToResponse(this InternApplication application) => new(
        application.Id,
        application.ProfileId,
        application.Status,
        application.AppliedAt, application.ReviewedBy, application.ReviewedAt, application.Note);

    public static InternReviewHistoryResponse ToResponse(this InternReviewHistory history) => new(
        history.Id,
        history.ProfileId,
        history.TargetType,
        history.TargetId,
        history.TargetLabel,
        history.Status,
        history.Note,
        history.ReviewedAt, history.ReviewedBy);
}

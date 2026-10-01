using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapApplicationDecision(RouteGroupBuilder group)
    {
        group.MapPatch("/applications/{applicationId:int}", async (
            int applicationId, DecideStatusRequest request, CareerDbContext db) =>
        {
            if (!DecisionStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái đơn phải là Đã duyệt hoặc Từ chối." });
            var application = await db.InternApplications
                .Include(item => item.Profile)
                .FirstOrDefaultAsync(item => item.Id == applicationId);
            if (application is null) return Results.NotFound();

            application.Status = request.Status;
            if (request.Status == "Từ chối" && string.IsNullOrWhiteSpace(request.Note))
                return Results.BadRequest(new { message = "Nhập lý do từ chối." });
            PortalWorkflow.Notify(db, application.ProfileId, "Kết quả xét duyệt hồ sơ", request.Status + ": " + request.Note, true);
            if (application.Profile is not null)
                application.Profile.Status = request.Status == "Đã duyệt" ? "Đang thực tập" : "Từ chối";
            db.InternReviewHistories.Add(new InternReviewHistory
            {
                ProfileId = application.ProfileId,
                TargetType = "Đơn ứng tuyển",
                TargetId = application.Id,
                TargetLabel = "Đơn xin thực tập",
                Status = request.Status,
                Note = request.Note?.Trim()
            });
            await db.SaveChangesAsync();
            return Results.Ok(application.ToResponse());
        })
        .WithName("DecideInternApplication");
    }
}

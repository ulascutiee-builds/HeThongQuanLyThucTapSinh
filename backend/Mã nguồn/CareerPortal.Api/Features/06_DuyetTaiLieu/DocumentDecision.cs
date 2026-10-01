using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapDocumentDecision(RouteGroupBuilder group)
    {
        group.MapPatch("/documents/{documentId:int}", async (
            int documentId, DecideStatusRequest request, CareerDbContext db) =>
        {
            if (!DecisionStatuses.Contains(request.Status))
                return Results.BadRequest(new { message = "Trạng thái tài liệu phải là Đã duyệt hoặc Từ chối." });
            var item = await db.InternDocuments.FindAsync(documentId);
            if (item is null) return Results.NotFound();
            item.Status = request.Status;
            item.Note = request.Note?.Trim();
            db.InternReviewHistories.Add(new InternReviewHistory
            {
                ProfileId = item.ProfileId,
                TargetType = "Tài liệu",
                TargetId = item.Id,
                TargetLabel = item.Type,
                Status = request.Status,
                Note = request.Note?.Trim()
            });
            await db.SaveChangesAsync();
            return Results.Ok(item.ToResponse());
        })
        .WithName("DecideInternDocument");
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapConfirmContract(RouteGroupBuilder group)
    {
        group.MapPost("/{profileId:int}/documents/{documentId:int}/confirm-contract", async (
            int profileId, int documentId, CareerDbContext db) =>
        {
            var contract = await db.InternDocuments.FirstOrDefaultAsync(document =>
                document.Id == documentId && document.ProfileId == profileId && document.Type == "Hợp đồng thực tập");
            if (contract is null) return Results.NotFound();
            if (contract.Status != "Chờ xác nhận")
                return Results.Conflict(new { message = "Hợp đồng hiện không ở trạng thái chờ xác nhận." });

            contract.Status = "Đã xác nhận";
            contract.ConfirmedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(contract.ToResponse());
        })
        .WithName("ConfirmInternContract");
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapDownloadDocument(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/documents/{documentId:int}/file", async (int documentId, CareerDbContext db) =>
        {
            var item = await db.InternDocuments.AsNoTracking()
                .Where(document => document.Id == documentId)
                .Select(document => new { document.Content, document.ContentType, document.FileName })
                .FirstOrDefaultAsync();
            return item is null
                ? Results.NotFound()
                : Results.File(item.Content, item.ContentType, item.FileName, enableRangeProcessing: true);
        })
        .WithName("GetDocumentFile");
    }
}

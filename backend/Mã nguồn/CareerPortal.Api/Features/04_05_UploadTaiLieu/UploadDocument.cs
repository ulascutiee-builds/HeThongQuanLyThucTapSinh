using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private static void MapUploadDocument(RouteGroupBuilder group)
    {
        group.MapPost("/{profileId:int}/documents", async (
            int profileId,
            HttpRequest request,
            CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();

            var fileName = Path.GetFileName(request.Query["fileName"].ToString());
            var type = request.Query["type"].ToString().Trim();
            var extension = Path.GetExtension(fileName);
            var contentType = request.ContentType?.Split(';', 2)[0].Trim() ?? "application/octet-stream";
            if (!AllowedExtensions.Contains(extension) || type is not ("CV" or "Đơn xin thực tập"))
                return Results.BadRequest(new { message = "Loại tài liệu hoặc định dạng tệp không được hỗ trợ." });
            if (request.ContentLength is > MaxUploadBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            await using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, request.HttpContext.RequestAborted);
            if (buffer.Length == 0 || buffer.Length > MaxUploadBytes)
                return Results.BadRequest(new { message = "Tệp rỗng hoặc vượt quá giới hạn 10 MB." });

            var previous = await db.InternDocuments
                .Where(document => document.ProfileId == profileId && document.Type == type)
                .ToListAsync();
            db.InternDocuments.RemoveRange(previous);
            var document = new InternDocument
            {
                ProfileId = profileId,
                Type = type,
                FileName = fileName,
                ContentType = contentType,
                Content = buffer.ToArray(),
                Status = "Chờ duyệt"
            };
            db.InternDocuments.Add(document);
            profile.Status = "Chờ duyệt";
            await db.SaveChangesAsync();

            return Results.Created($"/api/documents/{document.Id}/file", document.ToResponse());
        })
        .WithName("UploadInternDocument");
    }
}

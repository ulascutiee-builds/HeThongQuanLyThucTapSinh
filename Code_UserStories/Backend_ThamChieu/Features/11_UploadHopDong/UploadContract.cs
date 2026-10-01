using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private static void MapUploadContract(RouteGroupBuilder group)
    {
        group.MapPost("/profiles/{profileId:int}/contract", async (
            int profileId, HttpRequest request, CareerDbContext db) =>
        {
            var profile = await db.InternProfiles.FindAsync(profileId);
            if (profile is null) return Results.NotFound();
            var fileName = Path.GetFileName(request.Query["fileName"].ToString());
            var extension = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(extension))
                return Results.BadRequest(new { message = "Chỉ hỗ trợ tệp PDF, DOC hoặc DOCX." });
            if (request.ContentLength is > MaxUploadBytes)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

            await using var buffer = new MemoryStream();
            await request.Body.CopyToAsync(buffer, request.HttpContext.RequestAborted);
            if (buffer.Length == 0 || buffer.Length > MaxUploadBytes)
                return Results.BadRequest(new { message = "Tệp rỗng hoặc vượt quá giới hạn 10 MB." });

            var oldContracts = await db.InternDocuments
                .Where(document => document.ProfileId == profileId && document.Type == "Hợp đồng thực tập")
                .ToListAsync();
            db.InternDocuments.RemoveRange(oldContracts);
            var contract = new InternDocument
            {
                ProfileId = profileId,
                Type = "Hợp đồng thực tập",
                FileName = fileName,
                ContentType = request.ContentType?.Split(';', 2)[0].Trim() ?? "application/octet-stream",
                Content = buffer.ToArray(),
                Status = "Chờ xác nhận",
                Note = "Hợp đồng do bộ phận nhân sự tải lên."
            };
            db.InternDocuments.Add(contract);
            await db.SaveChangesAsync();
            return Results.Created($"/api/documents/{contract.Id}/file", contract.ToResponse());
        })
        .WithName("UploadHrContract");
    }
}

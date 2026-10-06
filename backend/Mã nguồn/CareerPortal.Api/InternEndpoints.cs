using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class InternEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    public static RouteGroupBuilder MapInternEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/interns").WithTags("Thực tập sinh");

        MapRegistration(group);

        MapLogin(group);

        MapUpdateIntern(group);

        MapWorkspace(group);

        MapUploadDocument(group);

        MapSubmitApplication(group);

        MapConfirmContract(group);

        MapSchedule(group);

        return group;
    }
}

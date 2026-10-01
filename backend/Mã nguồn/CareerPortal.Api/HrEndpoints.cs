using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static partial class HrEndpoints
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };
    private static readonly HashSet<string> ProfileStatuses = new(StringComparer.Ordinal)
    {
        "Chờ hồ sơ", "Chờ duyệt", "Đang thực tập", "Chờ bắt đầu", "Đã hoàn thành"
    };
    private static readonly HashSet<string> DecisionStatuses = new(StringComparer.Ordinal)
    {
        "Đã duyệt", "Từ chối"
    };

    public static RouteGroupBuilder MapHrEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/hr").WithTags("Nhân sự");

        MapHrDashboard(group);

        MapCreateProfile(group);

        MapHrUpdateProfile(group);

        MapApplicationDecision(group);

        MapDocumentDecision(group);

        MapUploadContract(group);

        MapDownloadDocument(routes);

        return group;
    }
}

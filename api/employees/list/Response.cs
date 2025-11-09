using AutoInsight.Common.Response;
namespace AutoInsight.YardEmployees.List;

public record Response(
    IReadOnlyCollection<ResponseItem> Data,
    PageInfo PageInfo,
    int Count
) : PagedResponse<ResponseItem>(Data, PageInfo, Count);

public record ResponseItem(Guid Id, string Name, string? ImageUrl, string Role, string UserId);

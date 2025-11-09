using AutoInsight.Common.Response;
namespace AutoInsight.Yards.List;

public record Response(
    IReadOnlyCollection<ResponseItem> Data,
    PageInfo PageInfo,
    int Count
) : PagedResponse<ResponseItem>(Data, PageInfo, Count);

public record ResponseItem(Guid Id, string Name, string OwnerId, int Capacity);

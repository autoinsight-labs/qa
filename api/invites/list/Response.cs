using AutoInsight.Common.Response;

namespace AutoInsight.EmployeeInvites.List;

public record Response(
    IReadOnlyCollection<ResponseItem> Data,
    PageInfo PageInfo,
    int Count
) : PagedResponse<ResponseItem>(Data, PageInfo, Count);

public record ResponseItem(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    string InviterId
);

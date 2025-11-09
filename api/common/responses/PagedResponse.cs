namespace AutoInsight.Common.Response;

public record PagedResponse<T>(IReadOnlyCollection<T> Data, PageInfo PageInfo, int Count);

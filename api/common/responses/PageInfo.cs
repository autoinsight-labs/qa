namespace AutoInsight.Common.Response;

public record PageInfo(Guid? NextCursor, bool HasNextPage);

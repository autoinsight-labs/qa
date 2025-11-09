namespace AutoInsight.Yards.CapacityForecast;

public sealed record Response(
    Guid YardId,
    DateTime GeneratedAt,
    int Capacity,
    IReadOnlyCollection<ForecastPoint> Points
);

public sealed record ForecastPoint(
    DateTime Timestamp,
    int ExpectedVehicles,
    float OccupancyRatio
);

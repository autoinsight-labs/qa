using System.Globalization;
using System.Linq;
using AutoInsight.Data;
using AutoInsight.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System.Threading;

namespace AutoInsight.ML;

public interface IYardCapacityForecastService
{
    Task<YardCapacityForecastResult> ForecastAsync(Guid yardId, int horizonHours, int capacity, CancellationToken cancellationToken = default);
}

public sealed record YardCapacityForecastPoint(DateTime Timestamp, int ExpectedVehicles, float OccupancyRatio);

public sealed record YardCapacityForecastResult(Guid YardId, DateTime GeneratedAt, int Capacity, IReadOnlyList<YardCapacityForecastPoint> Points);

internal sealed class YardCapacityForecastService : IYardCapacityForecastService
{
    private const int MinimumHorizon = 1;
    private const int MaximumHorizon = 72;
    private const int MinimumSnapshotsForTraining = 24;

    private readonly MLContext _mlContext;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public YardCapacityForecastService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _mlContext = new MLContext(seed: 7321);
    }

    public async Task<YardCapacityForecastResult> ForecastAsync(Guid yardId, int horizonHours, int capacity, CancellationToken cancellationToken = default)
    {
        if (horizonHours < MinimumHorizon || horizonHours > MaximumHorizon)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonHours), $"Horizon must be between {MinimumHorizon} and {MaximumHorizon} hours.");
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var snapshots = await dbContext.YardCapacitySnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.YardId == yardId)
            .OrderBy(snapshot => snapshot.CapturedAt)
            .ToListAsync(cancellationToken);

        if (snapshots.Count == 0)
        {
            var activeVehicles = await dbContext.Vehicles
                .AsNoTracking()
                .Where(vehicle => vehicle.YardId == yardId && vehicle.LeftAt == null)
                .CountAsync(cancellationToken);

            var baseRatio = capacity > 0 ? (float)activeVehicles / capacity : 0f;
            return BuildPersistenceForecast(yardId, capacity, baseRatio, horizonHours);
        }

        if (snapshots.Count < MinimumSnapshotsForTraining)
        {
            return BuildHeuristicForecast(yardId, capacity, snapshots, horizonHours);
        }

        var firstTimestamp = snapshots.First().CapturedAt;
        var lastTimestamp = snapshots.Last().CapturedAt;
        var baselineHours = GetBaselineHours(firstTimestamp, lastTimestamp);

        var observations = snapshots
            .Select(snapshot => CreateObservationFromSnapshot(snapshot, firstTimestamp, baselineHours))
            .ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(observations);

        var pipeline = _mlContext.Transforms.Concatenate(
                "Features",
                nameof(YardCapacityObservation.HourSin),
                nameof(YardCapacityObservation.HourCos),
                nameof(YardCapacityObservation.DaySin),
                nameof(YardCapacityObservation.DayCos),
                nameof(YardCapacityObservation.WeekSin),
                nameof(YardCapacityObservation.WeekCos),
                nameof(YardCapacityObservation.Trend))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(YardCapacityObservation.OccupancyRatio),
                featureColumnName: "Features"));

        var model = pipeline.Fit(dataView);
        using var predictionEngine = _mlContext.Model.CreatePredictionEngine<YardCapacityObservation, YardCapacityPrediction>(model);

        var generatedAt = DateTime.UtcNow;
        var points = new List<YardCapacityForecastPoint>(horizonHours);

        for (var offset = 1; offset <= horizonHours; offset++)
        {
            var targetTimestamp = generatedAt.AddHours(offset);
            var input = CreateObservationFromTimestamp(targetTimestamp, firstTimestamp, baselineHours);
            var prediction = predictionEngine.Predict(input);
            var ratio = Math.Clamp(prediction.Score, 0f, 1f);
            var expectedVehicles = (int)Math.Round(ratio * capacity);

            points.Add(new YardCapacityForecastPoint(targetTimestamp, expectedVehicles, ratio));
        }

        return new YardCapacityForecastResult(yardId, generatedAt, capacity, points);
    }

    private static YardCapacityForecastResult BuildPersistenceForecast(Guid yardId, int capacity, float baseRatio, int horizonHours)
    {
        var ratio = Math.Clamp(baseRatio, 0f, 1f);
        var generatedAt = DateTime.UtcNow;
        var points = new List<YardCapacityForecastPoint>(horizonHours);

        for (var offset = 1; offset <= horizonHours; offset++)
        {
            var timestamp = generatedAt.AddHours(offset);
            var expectedVehicles = (int)Math.Round(ratio * capacity);
            points.Add(new YardCapacityForecastPoint(timestamp, expectedVehicles, ratio));
        }

        return new YardCapacityForecastResult(yardId, generatedAt, capacity, points);
    }

    private static YardCapacityForecastResult BuildHeuristicForecast(Guid yardId, int capacity, IReadOnlyList<YardCapacitySnapshot> snapshots, int horizonHours)
    {
        var generatedAt = DateTime.UtcNow;
        var lastSnapshot = snapshots[^1];
        var fallbackRatio = lastSnapshot.Capacity > 0 ? (float)lastSnapshot.VehiclesInYard / lastSnapshot.Capacity : 0f;

        var averagesByHour = snapshots
            .GroupBy(snapshot => snapshot.CapturedAt.Hour)
            .ToDictionary(
                group => group.Key,
                group => (float)group
                    .Where(item => item.Capacity > 0)
                    .Select(item => item.VehiclesInYard / (float)item.Capacity)
                    .DefaultIfEmpty(fallbackRatio)
                    .Average());

        var points = new List<YardCapacityForecastPoint>(horizonHours);

        for (var offset = 1; offset <= horizonHours; offset++)
        {
            var timestamp = generatedAt.AddHours(offset);
            var ratio = averagesByHour.TryGetValue(timestamp.Hour, out var averageRatio)
                ? averageRatio
                : fallbackRatio;

            ratio = Math.Clamp(ratio, 0f, 1f);
            var expectedVehicles = (int)Math.Round(ratio * capacity);

            points.Add(new YardCapacityForecastPoint(timestamp, expectedVehicles, ratio));
        }

        return new YardCapacityForecastResult(yardId, generatedAt, capacity, points);
    }

    private static YardCapacityObservation CreateObservationFromSnapshot(YardCapacitySnapshot snapshot, DateTime referenceTimestamp, double baselineHours)
    {
        var ratio = snapshot.Capacity > 0 ? snapshot.VehiclesInYard / (float)snapshot.Capacity : 0f;

        return new YardCapacityObservation
        {
            HourSin = MathF.Sin(GetHourAngle(snapshot.CapturedAt)),
            HourCos = MathF.Cos(GetHourAngle(snapshot.CapturedAt)),
            DaySin = MathF.Sin(GetDayAngle(snapshot.CapturedAt)),
            DayCos = MathF.Cos(GetDayAngle(snapshot.CapturedAt)),
            WeekSin = MathF.Sin(GetWeekAngle(snapshot.CapturedAt)),
            WeekCos = MathF.Cos(GetWeekAngle(snapshot.CapturedAt)),
            Trend = NormalizeTrend(snapshot.CapturedAt, referenceTimestamp, baselineHours),
            OccupancyRatio = ratio
        };
    }

    private static YardCapacityObservation CreateObservationFromTimestamp(DateTime timestamp, DateTime referenceTimestamp, double baselineHours)
        => new()
        {
            HourSin = MathF.Sin(GetHourAngle(timestamp)),
            HourCos = MathF.Cos(GetHourAngle(timestamp)),
            DaySin = MathF.Sin(GetDayAngle(timestamp)),
            DayCos = MathF.Cos(GetDayAngle(timestamp)),
            WeekSin = MathF.Sin(GetWeekAngle(timestamp)),
            WeekCos = MathF.Cos(GetWeekAngle(timestamp)),
            Trend = NormalizeTrend(timestamp, referenceTimestamp, baselineHours),
            OccupancyRatio = 0f
        };

    private static double GetBaselineHours(DateTime firstTimestamp, DateTime lastTimestamp)
    {
        var totalHours = (lastTimestamp - firstTimestamp).TotalHours;
        return Math.Max(1d, totalHours);
    }

    private static float NormalizeTrend(DateTime timestamp, DateTime referenceTimestamp, double baselineHours)
    {
        if (baselineHours <= 0d)
        {
            return 0f;
        }

        var value = (float)((timestamp - referenceTimestamp).TotalHours / baselineHours);

        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return 0f;
        }

    return Math.Clamp(value, -1f, 2f);
    }

    private static float GetHourAngle(DateTime timestamp) => (float)(2 * Math.PI * timestamp.Hour / 24d);

    private static float GetDayAngle(DateTime timestamp) => (float)(2 * Math.PI * (int)timestamp.DayOfWeek / 7d);

    private static float GetWeekAngle(DateTime timestamp)
    {
        var week = ISOWeek.GetWeekOfYear(timestamp);
        return (float)(2 * Math.PI * (week - 1) / 53d);
    }
}

internal sealed record YardCapacityObservation
{
    public float HourSin { get; init; }
    public float HourCos { get; init; }
    public float DaySin { get; init; }
    public float DayCos { get; init; }
    public float WeekSin { get; init; }
    public float WeekCos { get; init; }
    public float Trend { get; init; }
    public float OccupancyRatio { get; init; }
}

internal sealed class YardCapacityPrediction
{
    public float Score { get; set; }
}

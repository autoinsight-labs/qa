using AutoInsight.Data;
using AutoInsight.ML;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace AutoInsight.Yards.CapacityForecast;

public static class Endpoint
{
    private const int DefaultHorizon = 24;
    private const int MaxHorizon = 72;

    public static RouteGroupBuilder MapYardCapacityForecastEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{yardId}/capacity-forecast", HandleAsync)
            .WithSummary("Forecast yard vehicle capacity for upcoming hours")
            .WithDescription(
                "Generates a short-term forecast of the expected number of vehicles inside a yard using the latest captured occupancy snapshots. When historical data is limited the service falls back to adaptive heuristics." +
                "\n\n**Path Parameter:**\n" +
                "- `yardId` (UUID): Identifier of the yard to forecast.\n" +
                "\n**Query Parameters:**\n" +
                "- `horizonHours` (int, optional, default 24): Number of future hours to forecast. Must be between 1 and 72.\n" +
                "\n**Responses:**\n" +
                "- `200 OK`: Forecast generated successfully.\n" +
                "- `400 Bad Request`: Invalid yard ID or horizon.\n" +
                "- `404 Not Found`: Yard not found." +
                "\n" +
                "\n**Example Request**\n" +
                "```bash\n" +
                "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/capacity-forecast?horizonHours=12\n" +
                "```"
            )
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(AppDbContext db, IYardCapacityForecastService forecastService, string yardId, int? horizonHours, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yardId) || !Guid.TryParse(yardId, out var parsedYardId))
        {
            return Results.BadRequest(new { error = "'Yard Id' must be a valid UUID." });
        }

        var yard = await db.Yards.AsNoTracking()
            .Where(y => y.Id == parsedYardId)
            .Select(y => new { y.Id, y.Capacity })
            .FirstOrDefaultAsync(cancellationToken);

        if (yard is null)
        {
            return Results.NotFound(new { error = "Yard not found" });
        }

        var effectiveHorizon = horizonHours ?? DefaultHorizon;
        if (effectiveHorizon < 1 || effectiveHorizon > MaxHorizon)
        {
            return Results.BadRequest(new { error = $"'horizonHours' must be between 1 and {MaxHorizon}." });
        }

        var forecast = await forecastService.ForecastAsync(parsedYardId, effectiveHorizon, yard.Capacity, cancellationToken);
        var response = new Response(
            forecast.YardId,
            forecast.GeneratedAt,
            forecast.Capacity,
            forecast.Points
                .Select(p => new ForecastPoint(p.Timestamp, p.ExpectedVehicles, MathF.Round(p.OccupancyRatio, 4)))
                .ToList()
        );

        return Results.Ok(response);
    }
}

using AutoInsight.Common.Response;
using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Vehicles.List;

public static class Endpoint
{
    public static RouteGroupBuilder MapVehicleListEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithSummary("List vehicles for a yard")
            .WithDescription(
                "Retrieves a paginated list of vehicles that belong to the specified yard, ordered by their identifier. By default, only vehicles that are still in the yard (i.e. have not recorded a `leftAt` value) are returned. Each item includes the beacon (uuid, major/minor) currently paired with the vehicle." +
                "\n\n**Path Parameters:**\n" +
                "- `yardId` (UUID, required): Yard whose vehicles will be listed." +
                "\n\n**Query Parameters:**\n" +
                "- `cursor` (UUID, optional): Use the last returned vehicle id to fetch the next page.\n" +
                "- `limit` (integer, optional, default=10, max=100): Maximum number of vehicles to return.\n" +
                "- `filter` (string, optional, default=active): Controls which vehicles are returned. Accepts `active` (still in yard), `departed` (already left), or `all`." +
                "\n\n**Example Requests:**\n" +
                "```bash\n" +
                "# Default behaviour (only active vehicles)\n" +
                "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/vehicles?limit=5\n" +
                "\n# Fetch only departed vehicles\n" +
                "GET /v2/yards/6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83/vehicles?filter=departed\n" +
                "```" +
                "\n\n**Responses:**\n" +
                "- `200 OK`: Returns paginated vehicles with pagination metadata.\n" +
                "- `400 Bad Request`: Invalid yardId, cursor, limit or filter.\n" +
                "- `404 Not Found`: Yard not found." +
                "\n\n**Example Response (200):**\n" +
                "```json\n" +
                "{\n" +
                "  \"data\": [\n" +
                "    { \"id\": \"3fd7b234-11aa-44f5-9a0a-0c6d9ad54a6f\", \"plate\": \"ABC1D23\", \"model\": \"MottuSport110i\", \"status\": \"Waiting\", \"enteredAt\": \"2025-11-07T10:15:32Z\", \"leftAt\": null, \"assigneeId\": null, \"beacon\": { \"id\": \"f5f04b8d-6f13-42ec-800d-342298c5bfa7\", \"uuid\": \"c0a8ff11-42f5-4e19-96c4-c9b5f7b9d8e4\", \"major\": \"200\", \"minor\": \"15\" } }\n" +
                "  ],\n" +
                "  \"pageInfo\": { \"nextCursor\": null, \"hasNext\": false },\n" +
                "  \"count\": 1\n" +
                "}\n" +
                "```"
            )
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static readonly string[] AllowedFilters = ["active", "departed", "all"];

    private static async Task<IResult> HandleAsync(
        AppDbContext db,
        string yardId,
        string? cursor = null,
        int limit = 10,
        string? filter = "active")
    {
        if (!Guid.TryParse(yardId, out var parsedYardId))
        {
            return Results.BadRequest(new { error = "YardId must be a valid UUID." });
        }

        var yard = await db.Yards.FirstOrDefaultAsync(y => y.Id == parsedYardId);
        if (yard is null)
        {
            return Results.NotFound(new { error = "Yard not found" });
        }

        if (limit <= 0 || limit > 100)
        {
            return Results.BadRequest(new { error = "Limit must be between 1 and 100." });
        }

        Guid? cursorGuid = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!Guid.TryParse(cursor, out var parsed))
            {
                return Results.BadRequest(new { error = "Cursor must be a valid UUID." });
            }

            var exists = await db.Vehicles.AnyAsync(y => y.Id == parsed && y.YardId == parsedYardId);
            if (!exists)
            {
                return Results.BadRequest(new { error = "Cursor not found." });
            }

            cursorGuid = parsed;
        }

        var query = db.Vehicles
            .Include(v => v.Beacon)
            .Where(v => v.YardId == parsedYardId);

        var normalizedFilter = (filter ?? "active").Trim().ToLowerInvariant();
        if (!AllowedFilters.Contains(normalizedFilter))
        {
            return Results.BadRequest(new { error = "Filter must be one of: active, departed, all." });
        }

        query = normalizedFilter switch
        {
            "active" => query.Where(v => v.LeftAt == null),
            "departed" => query.Where(v => v.LeftAt != null),
            _ => query
        };

        if (cursorGuid.HasValue)
        {
            query = query.Where(y => y.Id.CompareTo(cursorGuid.Value) > 0);
        }

        query = query.OrderBy(y => y.Id).Take(limit + 1);

        var vehicles = await query.ToListAsync();

        var hasNext = vehicles.Count > limit;
        Guid? nextCursor = null;

        if (hasNext)
        {
            nextCursor = vehicles[^1].Id;
            vehicles = vehicles.Take(limit).ToList();
        }

        var responseItems = vehicles
            .Select(v => new ResponseItem(
                v.Id,
                v.Plate,
                v.Model.ToString(),
                v.Status.ToString(),
                v.EnteredAt,
                v.LeftAt,
                v.AssigneeId,
                v.Beacon is not null
                    ? new BeaconResponse(v.Beacon.Id, v.Beacon.UUID, v.Beacon.Major, v.Beacon.Minor)
                    : null
            ))
            .ToList();

        var response = new Response(
            responseItems,
            new PageInfo(nextCursor, hasNext),
            responseItems.Count
        );

        return Results.Ok(response);
    }
}

using AutoInsight.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Vehicles.Get;

public static class Endpoint
{
    public static RouteGroupBuilder MapVehicleGetEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{vehicleId}", HandleAsync)
            .WithSummary("Get a vehicle by id")
            .WithDescription(
                "Returns the detailed information of a vehicle that belongs to the specified yard, including the current assignee (when available) and the beacon paired with the vehicle." +
                "\n\n**Path Parameters:**\n" +
                "- `yardId` (UUID, required): Yard that owns the vehicle.\n" +
                "- `vehicleId` (UUID, required): Vehicle identifier." +
                "\n\n**Responses:**\n" +
                "- `200 OK`: Vehicle found and returned.\n" +
                "- `400 Bad Request`: Invalid yardId or vehicleId.\n" +
                "- `404 Not Found`: Yard or vehicle not found." +
                "\n\n**Example Response (200):**\n" +
                "```json\n" +
                "{\n" +
                "  \"id\": \"3fd7b234-11aa-44f5-9a0a-0c6d9ad54a6f\",\n" +
                "  \"plate\": \"ABC1D23\",\n" +
                "  \"model\": \"MottuSport110i\",\n" +
                "  \"status\": \"Waiting\",\n" +
                "  \"enteredAt\": \"2025-11-07T10:15:32Z\",\n" +
                "  \"leftAt\": null,\n" +
                "  \"assignee\": {\n" +
                "    \"id\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                "    \"name\": \"João Lima\",\n" +
                "    \"imageUrl\": null,\n" +
                "    \"role\": \"Member\",\n" +
                "    \"userId\": \"firebase-user-123\"\n" +
                "  },\n" +
                "  \"beacon\": {\n" +
                "    \"id\": \"f5f04b8d-6f13-42ec-800d-342298c5bfa7\",\n" +
                "    \"uuid\": \"c0a8ff11-42f5-4e19-96c4-c9b5f7b9d8e4\",\n" +
                "    \"major\": \"200\",\n" +
                "    \"minor\": \"15\"\n" +
                "  }\n" +
                "}\n" +
                "```"
            )
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(AppDbContext db, string yardId, string vehicleId)
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

        if (!Guid.TryParse(vehicleId, out var parsedVehicleId))
        {
            return Results.BadRequest(new { error = "'Vehicle Id' must be a valid UUID." });
        }

        var vehicle = await db.Vehicles
            .Include(v => v.Assignee)
            .Include(v => v.Beacon)
            .FirstOrDefaultAsync(y => y.Id == parsedVehicleId && y.YardId == parsedYardId);

        if (vehicle is null)
        {
            return Results.NotFound(new { error = "Vehicle not found" });
        }

        var response = new Response(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Model.ToString(),
            vehicle.Status.ToString(),
            vehicle.EnteredAt,
            vehicle.LeftAt,
            vehicle.Assignee is not null
                ? new AssigneeResponse(
                    vehicle.Assignee.Id,
                    vehicle.Assignee.Name,
                    vehicle.Assignee.ImageUrl,
                    vehicle.Assignee.Role.ToString(),
                    vehicle.Assignee.UserId)
                : null,
            vehicle.Beacon is not null
                ? new BeaconResponse(vehicle.Beacon.Id, vehicle.Beacon.UUID, vehicle.Beacon.Major, vehicle.Beacon.Minor)
                : null
        );

        return Results.Ok(response);
    }
}

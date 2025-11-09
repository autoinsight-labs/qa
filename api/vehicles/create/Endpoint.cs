using AutoInsight.Data;
using AutoInsight.Models;
using AutoInsight.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Vehicles.Create;

public static class Endpoint
{
    public static RouteGroupBuilder MapVehicleCreateEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithSummary("Create a new vehicle for a yard")
            .WithDescription(
                "Registers a new vehicle under the specified yard, always attaching a beacon (UUID/Major/Minor). When an assignee is provided the vehicle starts in the `Waiting` status, otherwise it defaults to `Scheduled`." +
                "\n\n**Path Parameters:**\n" +
                "- `yardId` (UUID, required): Yard that will own the vehicle." +
                "\n\n**Request Body:**\n" +
                "```json\n" +
                "{\n" +
                "  \"plate\": \"ABC1D23\",\n" +
                "  \"model\": \"MottuSport110i\",\n" +
                "  \"beacon\": {\n" +
                "    \"uuid\": \"c0a8ff11-42f5-4e19-96c4-c9b5f7b9d8e4\",\n" +
                "    \"major\": \"200\",\n" +
                "    \"minor\": \"15\"\n" +
                "  },\n" +
                "  \"assigneeId\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\"\n" +
                "}\n" +
                "```" +
                "\n\n**Responses:**\n" +
                "- `201 Created`: Vehicle successfully created (returns vehicle details).\n" +
                "- `400 Bad Request`: Invalid yardId, assigneeId or request payload.\n" +
                "- `404 Not Found`: Yard or assignee not found." +
                "\n\n**Example Response (201):**\n" +
                "```json\n" +
                "{\n" +
                "  \"id\": \"9f1f3a93-bf6d-4028-91cb-238aaf3b2368\",\n" +
                "  \"plate\": \"ABC1D23\",\n" +
                "  \"model\": \"MottuSport110i\",\n" +
                "  \"status\": \"Waiting\",\n" +
                "  \"enteredAt\": \"2025-11-07T10:15:32Z\",\n" +
                "  \"leftAt\": null,\n" +
                "  \"yardId\": \"6b1b36c2-8f63-4c2b-b3df-9c5d9cfefb83\",\n" +
                "  \"assigneeId\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                "  \"beacon\": { \"id\": \"f5f04b8d-6f13-42ec-800d-342298c5bfa7\", \"uuid\": \"c0a8ff11-42f5-4e19-96c4-c9b5f7b9d8e4\", \"major\": \"200\", \"minor\": \"15\" }\n" +
                "}\n" +
                "```"
            )
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Plate)
                .NotEmpty()
                .Matches(@"^([A-Z]{3}\d{4}|[A-Z]{3}\s?\d[A-Z]\d{2})$")
                .WithMessage("Plate must be a valid plate (AAA1234 or ABC1C34).");

            RuleFor(x => x.Model)
                .NotEmpty()
                .Must(BeAValidModel)
                .WithMessage("Model must be one of: MottuSport110i, Mottue, HondaPop110i, TVSSport110i.");

            RuleFor(x => x.Beacon)
                .NotNull()
                .SetValidator(new BeaconValidator());
        }

        private static bool BeAValidModel(string model) =>
            Enum.TryParse<VehicleModel>(model, true, out _);
    }

    private class BeaconValidator : AbstractValidator<BeaconRequest>
    {
        public BeaconValidator()
        {
            RuleFor(x => x.Uuid).NotEmpty();
            RuleFor(x => x.Major).NotEmpty();
            RuleFor(x => x.Minor).NotEmpty();
        }
    }

    private static async Task<IResult> HandleAsync(Request request, AppDbContext db, IYardCapacitySnapshotService snapshotService, string yardId)
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

        var validation = await new Validator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var normalizedUuid = request.Beacon.Uuid.Trim();
        var normalizedMajor = request.Beacon.Major.Trim();
        var normalizedMinor = request.Beacon.Minor.Trim();

        var beaconExists = await db.Beacons.AnyAsync(b => b.UUID == normalizedUuid);
        if (beaconExists)
        {
            return Results.BadRequest(new { error = "Beacon UUID already registered." });
        }

        var beaconPairExists = await db.Beacons.AnyAsync(b => b.Major == normalizedMajor && b.Minor == normalizedMinor);
        if (beaconPairExists)
        {
            return Results.BadRequest(new { error = "Beacon major/minor pair already registered." });
        }

        var vehicle = new Vehicle
        {
            Plate = request.Plate,
            Model = Enum.Parse<VehicleModel>(request.Model, true),
            Status = request.AssigneeId is null ? VehicleStatus.Scheduled : VehicleStatus.Waiting,
            YardId = parsedYardId,
            Yard = yard,
            Beacon = null!
        };

        var beacon = new Beacon
        {
            UUID = normalizedUuid,
            Major = normalizedMajor,
            Minor = normalizedMinor,
            VehicleId = vehicle.Id,
            Vehicle = vehicle
        };

        vehicle.Beacon = beacon;

        if (request.AssigneeId is not null)
        {
            if (!Guid.TryParse(request.AssigneeId, out var parsedAssigneeId))
            {
                return Results.BadRequest(new { error = "AssigneeId must be a valid UUID." });
            }

            var employee = await db.YardEmployees
                .FirstOrDefaultAsync(y => y.Id == parsedAssigneeId && y.YardId == parsedYardId);
            if (employee is null)
            {
                return Results.NotFound(new { error = "Employee not found" });
            }

            vehicle.AssigneeId = parsedAssigneeId;
            vehicle.Assignee = employee;
        }

        db.Vehicles.Add(vehicle);

        await db.SaveChangesAsync();
        await snapshotService.CaptureAsync(yard);

        var response = new Response(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Model.ToString(),
            vehicle.Status.ToString(),
            vehicle.EnteredAt,
            vehicle.LeftAt,
            vehicle.YardId,
            vehicle.AssigneeId,
            new BeaconResponse(vehicle.Beacon!.Id, vehicle.Beacon.UUID, vehicle.Beacon.Major, vehicle.Beacon.Minor)
        );

        return Results.Created($"/v2/yards/{yardId}/vehicles/{vehicle.Id}", response);
    }
}

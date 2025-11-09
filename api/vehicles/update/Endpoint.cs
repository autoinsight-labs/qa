using System.Collections.Generic;
using AutoInsight.Data;
using AutoInsight.Models;
using AutoInsight.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AutoInsight.Vehicles.Update;

public static class Endpoint
{
    public static RouteGroupBuilder MapVehicleUpdateEndpoint(this RouteGroupBuilder group)
    {
        group.MapPatch("/{vehicleId}", HandleAsync)
            .WithSummary("Update a vehicle status, assignee or beacon")
            .WithDescription(
                "Updates the status, assignee and/or beacon of a vehicle that belongs to the specified yard. Vehicles that are `Cancelled` or `Finished` cannot be updated." +
                "\n\n**Path Parameters:**\n" +
                "- `yardId` (UUID, required): Yard that owns the vehicle.\n" +
                "- `vehicleId` (UUID, required): Vehicle identifier." +
                "\n\n**Request Body (partial):**\n" +
                "```json\n" +
                "{\n" +
                "  \"status\": \"OnService\",\n" +
                "  \"assigneeId\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                "  \"beacon\": { \"uuid\": \"9c5d8732-cc5c-44a6-82a1-3f3b431dbd58\", \"major\": \"300\", \"minor\": \"12\" }\n" +
                "}\n" +
                "```" +
                "\n\n**Responses:**\n" +
                "- `200 OK`: Vehicle successfully updated.\n" +
                "- `400 Bad Request`: Invalid identifiers or request payload (including validation errors).\n" +
                "- `404 Not Found`: Yard, vehicle or assignee not found." +
                "\n\n**Example Response (200):**\n" +
                "```json\n" +
                "{\n" +
                "  \"id\": \"3fd7b234-11aa-44f5-9a0a-0c6d9ad54a6f\",\n" +
                "  \"plate\": \"ABC1D23\",\n" +
                "  \"model\": \"MottuSport110i\",\n" +
                "  \"status\": \"OnService\",\n" +
                "  \"enteredAt\": \"2025-11-07T10:15:32Z\",\n" +
                "  \"leftAt\": null,\n" +
                "  \"assigneeId\": \"7fbd32a2-1b78-4a2e-bf53-83f1c1fdd92b\",\n" +
                "  \"beacon\": { \"id\": \"f5f04b8d-6f13-42ec-800d-342298c5bfa7\", \"uuid\": \"9c5d8732-cc5c-44a6-82a1-3f3b431dbd58\", \"major\": \"300\", \"minor\": \"12\" }\n" +
                "}\n" +
                "```"
            )
            .Produces<Response>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            When(x => x.AssigneeId is not null, () =>
            {
                RuleFor(x => x.AssigneeId)
                    .NotEmpty()
                    .Must(id => Guid.TryParse(id, out _))
                    .WithMessage("'Assignee Id' is not a valid UUID");
            });

            When(x => x.Status is not null, () =>
            {
                RuleFor(x => x.Status)
                    .NotEmpty()
                    .Must(BeAValidStatus);
            });

            When(x => x.Beacon is not null, () =>
            {
                RuleFor(x => x.Beacon!).SetValidator(new BeaconValidator());
            });
        }

        private static bool BeAValidStatus(string? status) =>
            Enum.TryParse<VehicleStatus>(status, true, out _);
    }

    private class BeaconValidator : AbstractValidator<BeaconRequest>
    {
        public BeaconValidator()
        {
            When(x => x.Uuid is not null, () =>
            {
                RuleFor(x => x.Uuid!)
                    .NotEmpty();
            });

            When(x => x.Major is not null, () =>
            {
                RuleFor(x => x.Major!)
                    .NotEmpty();
            });

            When(x => x.Minor is not null, () =>
            {
                RuleFor(x => x.Minor!)
                    .NotEmpty();
            });
        }
    }

    private static async Task<IResult> HandleAsync(Request request, AppDbContext db, IYardCapacitySnapshotService snapshotService, string yardId, string vehicleId)
    {
        if (!Guid.TryParse(yardId, out var parsedYardId))
        {
            return Results.BadRequest(new { error = "'Yard Id' must be a valid UUID." });
        }

        var yard = await db.Yards.FirstOrDefaultAsync(v => v.Id == parsedYardId);
        if (yard is null)
        {
            return Results.NotFound(new { error = "Yard not found" });
        }

        if (!Guid.TryParse(vehicleId, out var parsedVehicleId))
        {
            return Results.BadRequest(new { error = "'Vehicle Id' must be a valid UUID." });
        }

        var vehicle = await db.Vehicles
            .Include(v => v.Beacon)
            .FirstOrDefaultAsync(v => v.Id == parsedVehicleId && v.YardId == parsedYardId);

        if (vehicle is null)
        {
            return Results.NotFound(new { error = "Vehicle not found" });
        }

        if (vehicle.Status is VehicleStatus.Cancelled or VehicleStatus.Finished)
        {
            return Results.BadRequest(new { error = $"Can't update a service that has been {vehicle.Status}." });
        }

        var validation = await new Validator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        if (vehicle.Beacon is null && request.Beacon is null)
        {
            return Results.BadRequest(new { error = "Beacon not found for vehicle. Provide the beacon payload to attach one." });
        }

        if (request.AssigneeId is not null)
        {
            var parsedAssigneeId = Guid.Parse(request.AssigneeId);
            var assignee = await db.YardEmployees
                .FirstOrDefaultAsync(v => v.Id == parsedAssigneeId && v.YardId == parsedYardId);

            if (assignee is null)
            {
                return Results.NotFound(new { error = "Assignee not found" });
            }

            vehicle.AssigneeId = parsedAssigneeId;
            vehicle.Assignee = assignee;
        }

        var shouldCaptureSnapshot = false;
        var vehicleDeparted = false;
        if (request.Status is not null)
        {
            var nextStatus = Enum.Parse<VehicleStatus>(request.Status, true);
            var previousStatus = vehicle.Status;

            if (nextStatus != previousStatus)
            {
                vehicle.Status = nextStatus;

                if (nextStatus is VehicleStatus.Cancelled or VehicleStatus.Finished)
                {
                    vehicle.LeftAt = DateTime.UtcNow;
                    shouldCaptureSnapshot = true;
                    vehicleDeparted = true;

                    if (vehicle.Beacon is not null)
                    {
                        db.Beacons.Remove(vehicle.Beacon);
                        vehicle.Beacon = null;
                    }
                }
            }
        }

        if (request.Beacon is not null && !vehicleDeparted)
        {
            var normalizedUuid = request.Beacon.Uuid?.Trim();
            var normalizedMajor = request.Beacon.Major?.Trim();
            var normalizedMinor = request.Beacon.Minor?.Trim();

            if (vehicle.Beacon is null)
            {
                if (string.IsNullOrWhiteSpace(normalizedUuid) ||
                    string.IsNullOrWhiteSpace(normalizedMajor) ||
                    string.IsNullOrWhiteSpace(normalizedMinor))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        { "Beacon", new[] { "'Uuid', 'Major' and 'Minor' must be provided when attaching a beacon to the vehicle." } }
                    });
                }

                var uuidInUse = await db.Beacons.AnyAsync(b => b.UUID == normalizedUuid);
                if (uuidInUse)
                {
                    return Results.BadRequest(new { error = "Beacon UUID already registered." });
                }

                var pairInUse = await db.Beacons.AnyAsync(b => b.Major == normalizedMajor && b.Minor == normalizedMinor);
                if (pairInUse)
                {
                    return Results.BadRequest(new { error = "Beacon major/minor pair already registered." });
                }

                vehicle.Beacon = new Beacon
                {
                    UUID = normalizedUuid!,
                    Major = normalizedMajor!,
                    Minor = normalizedMinor!,
                    VehicleId = vehicle.Id,
                    Vehicle = vehicle
                };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(normalizedUuid))
                {
                    var uuidInUse = await db.Beacons.AnyAsync(b => b.UUID == normalizedUuid && b.VehicleId != vehicle.Id);
                    if (uuidInUse)
                    {
                        return Results.BadRequest(new { error = "Beacon UUID already registered." });
                    }

                    vehicle.Beacon.UUID = normalizedUuid!;
                }

                if (!string.IsNullOrWhiteSpace(normalizedMajor))
                {
                    var majorConflict = await db.Beacons.AnyAsync(b =>
                        b.Major == normalizedMajor &&
                        b.Minor == (normalizedMinor ?? vehicle.Beacon.Minor) &&
                        b.VehicleId != vehicle.Id);

                    if (majorConflict)
                    {
                        return Results.BadRequest(new { error = "Beacon major/minor pair already registered." });
                    }

                    vehicle.Beacon.Major = normalizedMajor!;
                }

                if (!string.IsNullOrWhiteSpace(normalizedMinor))
                {
                    var minorConflict = await db.Beacons.AnyAsync(b =>
                        b.Major == (normalizedMajor ?? vehicle.Beacon.Major) &&
                        b.Minor == normalizedMinor &&
                        b.VehicleId != vehicle.Id);

                    if (minorConflict)
                    {
                        return Results.BadRequest(new { error = "Beacon major/minor pair already registered." });
                    }

                    vehicle.Beacon.Minor = normalizedMinor!;
                }
            }
        }

        await db.SaveChangesAsync();

        if (shouldCaptureSnapshot)
        {
            await snapshotService.CaptureAsync(yard);
        }

        var response = new Response(
            vehicle.Id,
            vehicle.Plate,
            vehicle.Model.ToString(),
            vehicle.Status.ToString(),
            vehicle.EnteredAt,
            vehicle.LeftAt,
            vehicle.AssigneeId,
            vehicle.Beacon is not null
                ? new BeaconResponse(vehicle.Beacon.Id, vehicle.Beacon.UUID, vehicle.Beacon.Major, vehicle.Beacon.Minor)
                : null
        );

        return Results.Ok(response);
    }
}

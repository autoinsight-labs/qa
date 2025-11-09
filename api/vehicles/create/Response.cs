namespace AutoInsight.Vehicles.Create;

public record Response(
    Guid Id,
    string Plate,
    string Model,
    string Status,
    DateTime EnteredAt,
    DateTime? LeftAt,
    Guid YardId,
    Guid? AssigneeId,
    BeaconResponse Beacon
);

public record BeaconResponse(Guid Id, string Uuid, string Major, string Minor);

namespace AutoInsight.Vehicles.Update;

public record Response(
    Guid Id,
    string Plate,
    string Model,
    string Status,
    DateTime EnteredAt,
    DateTime? LeftAt,
    Guid? AssigneeId,
    BeaconResponse? Beacon
);

public record BeaconResponse(Guid Id, string Uuid, string Major, string Minor);

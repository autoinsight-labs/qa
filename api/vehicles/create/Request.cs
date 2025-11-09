namespace AutoInsight.Vehicles.Create;

public record Request(
    string Plate,
    string Model,
    BeaconRequest Beacon,
    string? AssigneeId
);

public record BeaconRequest(string Uuid, string Major, string Minor);

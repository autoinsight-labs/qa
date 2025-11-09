namespace AutoInsight.Vehicles.Update;

public record Request(string? Status, string? AssigneeId, BeaconRequest? Beacon);

public record BeaconRequest(string? Uuid, string? Major, string? Minor);

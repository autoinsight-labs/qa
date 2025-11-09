namespace AutoInsight.Vehicles.Get;

public record Response(
    Guid Id,
    string Plate,
    string Model,
    string Status,
    DateTime EnteredAt,
    DateTime? LeftAt,
    AssigneeResponse? Assignee,
    BeaconResponse? Beacon
);

public record AssigneeResponse(Guid Id, string Name, string? ImageUrl, string Role, string UserId);

public record BeaconResponse(Guid Id, string Uuid, string Major, string Minor);

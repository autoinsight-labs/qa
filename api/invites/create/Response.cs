namespace AutoInsight.EmployeeInvites.Create;

public record Response(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    string InviterId,
    YardResponse Yard
);

public record YardResponse(Guid Id, string Name);

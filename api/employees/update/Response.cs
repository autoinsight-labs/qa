namespace AutoInsight.YardEmployees.Update;

public record Response(Guid Id, string Name, string? ImageUrl, string Role, string UserId);

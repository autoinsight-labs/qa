namespace AutoInsight.Auth;

public sealed record AuthenticatedUser(string UserId, string? Email)
{
    public static readonly string HttpContextItemKey = typeof(AuthenticatedUser).FullName!;
}
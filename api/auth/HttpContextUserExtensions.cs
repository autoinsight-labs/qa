using Microsoft.AspNetCore.Http;

namespace AutoInsight.Auth;

public static class HttpContextUserExtensions
{
    public static bool TryGetAuthenticatedUser(this HttpContext context, out AuthenticatedUser? user)
    {
        if (context.Items.TryGetValue(AuthenticatedUser.HttpContextItemKey, out var stored) && stored is AuthenticatedUser typed)
        {
            user = typed;
            return true;
        }

        user = null;
        return false;
    }

    internal static void SetAuthenticatedUser(this HttpContext context, AuthenticatedUser user)
    {
        context.Items[AuthenticatedUser.HttpContextItemKey] = user;
    }
}
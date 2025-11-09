using Microsoft.AspNetCore.Builder;

namespace AutoInsight.Auth;

public static class AuthenticationPipelineExtensions
{
    public static IApplicationBuilder UseAuthenticatedUserExtraction(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthenticatedUserMiddleware>();
    }
}
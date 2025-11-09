using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AutoInsight.Auth;

public sealed class AuthenticatedUserMiddleware
{
    private const string AuthorizationHeaderName = "Authorization";
    private const string BearerPrefix = "Bearer ";

    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticatedUserMiddleware> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AuthenticatedUserMiddleware(RequestDelegate next, ILogger<AuthenticatedUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.TryGetAuthenticatedUser(out _))
        {
            var header = context.Request.Headers[AuthorizationHeaderName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(header))
            {
                var token = header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
                    ? header.Substring(BearerPrefix.Length).Trim()
                    : header.Trim();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        var jwt = _tokenHandler.ReadJwtToken(token);

                        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "user_id");
                        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "email");

                        if (userIdClaim is not null && !string.IsNullOrWhiteSpace(userIdClaim.Value))
                        {
                            context.SetAuthenticatedUser(new AuthenticatedUser(userIdClaim.Value, emailClaim?.Value));
                        }
                        else
                        {
                            _logger.LogWarning("JWT token missing 'user_id' claim.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JWT token for authenticated user.");
                    }
                }
            }
        }

        await _next(context);
    }
}
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AutoInsight.Tests.Shared;

internal static class JwtTokenFactory
{
    private static readonly JwtSecurityTokenHandler Handler = new();

    public static string CreateToken(string userId, string? email = null)
    {
        var claims = new List<Claim>
        {
            new("user_id", userId)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email!));
        }

        return CreateToken(claims);
    }

    public static string CreateToken(IEnumerable<Claim> claims)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(claims: claims, notBefore: now.AddMinutes(-1), expires: now.AddMinutes(5));
        return Handler.WriteToken(token);
    }
}

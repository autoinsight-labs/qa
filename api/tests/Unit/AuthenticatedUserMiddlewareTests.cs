using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoInsight.Auth;
using AutoInsight.Tests.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AutoInsight.Tests.Unit;

public sealed class AuthenticatedUserMiddlewareTests
{
    public static IEnumerable<object?[]> AuthorizationHeaderCases()
    {
        yield return new object?[]
        {
            "No header",
            null,
            false,
            null,
            null
        };

        yield return new object?[]
        {
            "Valid bearer token",
            $"Bearer {JwtTokenFactory.CreateToken("user-123", "user@example.com")}",
            true,
            "user-123",
            "user@example.com"
        };

        yield return new object?[]
        {
            "Token without bearer prefix",
            JwtTokenFactory.CreateToken("user-456", "another@example.com"),
            true,
            "user-456",
            "another@example.com"
        };

        yield return new object?[]
        {
            "Token missing user id claim",
            $"Bearer {JwtTokenFactory.CreateToken(Array.Empty<Claim>())}",
            false,
            null,
            null
        };

        yield return new object?[]
        {
            "Malformed token",
            "Bearer this-is-not-a-jwt",
            false,
            null,
            null
        };
    }

    [Theory]
    [MemberData(nameof(AuthorizationHeaderCases))]
    public async Task InvokeAsync_ExtractsAuthenticatedUserWhenPossible(
        string name,
        string? header,
        bool expectedHasUser,
        string? expectedUserId,
        string? expectedEmail)
    {
        Assert.False(string.IsNullOrWhiteSpace(name));

        var context = new DefaultHttpContext();
        if (header is not null)
        {
            context.Request.Headers.Authorization = header;
        }

        var nextCalled = false;
        RequestDelegate next = ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new AuthenticatedUserMiddleware(next, NullLogger<AuthenticatedUserMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);

        var hasUser = context.TryGetAuthenticatedUser(out var user);
        Assert.Equal(expectedHasUser, hasUser);

        if (expectedHasUser)
        {
            Assert.NotNull(user);
            Assert.Equal(expectedUserId, user!.UserId);
            Assert.Equal(expectedEmail, user.Email);
        }
        else
        {
            Assert.Null(user);
        }
    }
}

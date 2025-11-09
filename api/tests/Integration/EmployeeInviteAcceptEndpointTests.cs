using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoInsight.Data;
using AutoInsight.Models;
using AutoInsight.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoInsight.Tests.Integration;

public sealed class EmployeeInviteAcceptEndpointTests
{
    public static IEnumerable<object[]> AcceptInviteSuccessCases()
    {
        yield return new object[] { new AcceptInviteScenario("invitee@example.com", "Maria Alves", null) };
        yield return new object[] { new AcceptInviteScenario("invitee@example.com", "João Pereira", "https://example.com/avatar.png") };
    }

    [Fact]
    public async Task AcceptInvite_ShouldReturnForbidden_WhenEmailDoesNotMatch()
    {
        using var factory = new CustomWebApplicationFactory();
        var inviteId = await SeedPendingInviteAsync(factory, "invitee@example.com");

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtTokenFactory.CreateToken("user-123", "another@example.com"));

        var response = await client.PostAsJsonAsync($"/v2/invites/{inviteId}/accept", new
        {
            name = "Unauthorized User"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AcceptInviteSuccessCases))]
    public async Task AcceptInvite_ShouldCreateEmployee_WhenEmailMatches(AcceptInviteScenario scenario)
    {
        using var factory = new CustomWebApplicationFactory();
        var inviteId = await SeedPendingInviteAsync(factory, scenario.Email);

        using var client = factory.CreateClient();
        var token = JwtTokenFactory.CreateToken("user-accepted", scenario.Email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync($"/v2/invites/{inviteId}/accept", new
        {
            name = scenario.Name,
            imageUrl = scenario.ImageUrl
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invite = await db.EmployeeInvites.SingleAsync(i => i.Id == inviteId);
        Assert.Equal(InviteStatus.Accepted, invite.Status);
        Assert.NotNull(invite.AcceptedAt);

        var employee = await db.YardEmployees.SingleAsync(e => e.UserId == "user-accepted");
        Assert.Equal(scenario.Name, employee.Name);
        Assert.Equal(scenario.ImageUrl, employee.ImageUrl);
        Assert.Equal(invite.YardId, employee.YardId);
    }

    private static async Task<Guid> SeedPendingInviteAsync(CustomWebApplicationFactory factory, string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var yard = new Yard
        {
            Name = "Seed Yard",
            OwnerId = "owner-seed",
            Capacity = 42
        };

        var invite = new EmployeeInvite
        {
            Yard = yard,
            YardId = yard.Id,
            Email = email,
            Role = EmployeeRole.Member,
            InviterId = "owner-seed"
        };

        db.Yards.Add(yard);
        db.EmployeeInvites.Add(invite);
        await db.SaveChangesAsync();

        return invite.Id;
    }

    public sealed record AcceptInviteScenario(string Email, string Name, string? ImageUrl);
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AutoInsight.Tests.Shared;
using Xunit;

namespace AutoInsight.Tests.Integration;

public sealed class YardEndpointsTests
{
    public static IEnumerable<object[]> CreateYardSuccessCases()
    {
        yield return new object[] { new CreateYardScenario("Main Yard", "Maria Souza", 120) };
        yield return new object[] { new CreateYardScenario("Downtown Hub", "João Lima", 80) };
    }

    [Fact]
    public async Task PostYard_ShouldReturnUnauthorized_WhenHeaderMissing()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v2/yards", new
        {
            name = "No Auth Yard",
            ownerName = "John Doe",
            capacity = 50
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(CreateYardSuccessCases))]
    public async Task PostYard_ShouldCreateYard_WhenAuthenticated(CreateYardScenario scenario)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var token = JwtTokenFactory.CreateToken("firebase-user-123", "owner@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/v2/yards", new
        {
            name = scenario.Name,
            ownerName = scenario.OwnerName,
            capacity = scenario.Capacity
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<YardResponse>();
        Assert.NotNull(payload);
        Assert.Equal(scenario.Name, payload!.Name);
        Assert.Equal("firebase-user-123", payload.OwnerId);
        Assert.Equal(scenario.Capacity, payload.Capacity);
        Assert.NotEqual(Guid.Empty, payload.Id);
    }

    public sealed record CreateYardScenario(string Name, string OwnerName, int Capacity);

    private sealed record YardResponse(Guid Id, string Name, string OwnerId, int Capacity);
}

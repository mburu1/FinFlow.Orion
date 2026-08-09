using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Api.IntegrationTests.Auth;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class AuthFlowTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ThenLogin_ReturnsAnAccessToken()
    {
        using var client = _factory.CreateClient();

        var token = await AuthTestHelper.RegisterAndLoginAsync(client);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var email = $"test-{Guid.NewGuid():N}@finflow-orion-tests.local";

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Correct-Password-123!",
            firstName = "Test",
            lastName = "User"
        });

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = "Wrong-Password-123!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

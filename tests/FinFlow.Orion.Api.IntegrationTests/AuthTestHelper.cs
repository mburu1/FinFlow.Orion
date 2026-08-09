using System.Net.Http.Json;
using System.Text.Json;

namespace FinFlow.Orion.Api.IntegrationTests;

internal static class AuthTestHelper
{
    /// <summary>
    /// Registers a fresh test user and logs in via the real Api endpoints, returning
    /// an access token that can be attached to subsequent authenticated requests.
    /// </summary>
    public static async Task<string> RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"test-{Guid.NewGuid():N}@finflow-orion-tests.local";
        const string password = "Test-Password-123!";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var stream = await loginResponse.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(stream);

        return document.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not contain an accessToken.");
    }
}

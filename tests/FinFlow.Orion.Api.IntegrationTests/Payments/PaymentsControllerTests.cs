using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinFlow.Orion.Contracts.Payments.Requests;
using FinFlow.Orion.Contracts.Payments.Responses;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Api.IntegrationTests.Payments;

[Collection(ApiIntegrationTestCollection.Name)]
public sealed class PaymentsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentsControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await AuthTestHelper.RegisterAndLoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static InitiatePaymentRequest CardPaymentRequest(string? idempotencyKey = null) => new()
    {
        Amount = 1000,
        CurrencyCode = "KES",
        Provider = "Card",
        Channel = "Web",
        IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString("N"),
        CustomerId = "cust-integration-test",
        Description = "Integration test payment"
    };

    [Fact]
    public async Task InitiatePayment_Card_CompletesSynchronouslyAsCaptured()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/payments", CardPaymentRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<InitiatePaymentResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Captured");
        body.Provider.Should().Be("Card");
    }

    [Fact]
    public async Task InitiatePayment_DuplicateIdempotencyKey_ReturnsConflict()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var idempotencyKey = Guid.NewGuid().ToString("N");

        var first = await client.PostAsJsonAsync("/api/v1/payments", CardPaymentRequest(idempotencyKey));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/v1/payments", CardPaymentRequest(idempotencyKey));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPaymentById_ReturnsTheCreatedPayment()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/api/v1/payments", CardPaymentRequest());
        var createdBody = await created.Content.ReadFromJsonAsync<InitiatePaymentResponse>();

        var response = await client.GetAsync($"/api/v1/payments/{createdBody!.PaymentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentById_UnknownId_ReturnsNotFound()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReversePayment_OnACapturedPayment_Succeeds()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/api/v1/payments", CardPaymentRequest());
        var createdBody = await created.Content.ReadFromJsonAsync<InitiatePaymentResponse>();

        var reverseResponse = await client.PostAsJsonAsync(
            $"/api/v1/payments/{createdBody!.PaymentId}/reverse",
            new ReversePaymentRequest
            {
                PaymentId = createdBody.PaymentId,
                Reason = "integration test refund",
                IdempotencyKey = Guid.NewGuid().ToString("N")
            });

        reverseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reloaded = await client.GetAsync($"/api/v1/payments/{createdBody.PaymentId}");
        var reloadedBody = await reloaded.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        reloadedBody.GetProperty("status").GetString().Should().Be("Reversed");
    }
}

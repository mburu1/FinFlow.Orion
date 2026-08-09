using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Infrastructure.Webhooks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinFlow.Orion.Infrastructure.Tests.Webhooks;

[Collection(InfrastructureTestCollection.Name)]
public sealed class MongoWebhookServiceTests
{
    private readonly InfrastructureTestFixture _fixture;

    public MongoWebhookServiceTests(InfrastructureTestFixture fixture) => _fixture = fixture;

    private MongoWebhookService CreateService(string collectionName) => new(
        Options.Create(new MongoWebhookConfiguration
        {
            ConnectionString = _fixture.MongoConnectionString,
            DatabaseName = "FinFlowOrionTests",
            CollectionName = collectionName
        }),
        NullLogger<MongoWebhookService>.Instance);

    [Fact]
    public async Task StoreRawPayloadAsync_ThenGetRawPayloadAsync_RoundTripsTheDocument()
    {
        var service = CreateService($"webhooks-{Guid.NewGuid():N}");
        var webhookEventId = Guid.NewGuid();

        await service.StoreRawPayloadAsync(webhookEventId, PaymentProvider.MPesa, "{\"raw\":\"payload\"}");

        var reloaded = await service.GetRawPayloadAsync(webhookEventId);

        reloaded.Should().NotBeNull();
        reloaded!.WebhookEventId.Should().Be(webhookEventId);
        reloaded.Provider.Should().Be("MPesa");
        reloaded.RawPayload.Should().Be("{\"raw\":\"payload\"}");
        reloaded.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UpdatesIsProcessedAndProcessedAt()
    {
        var service = CreateService($"webhooks-{Guid.NewGuid():N}");
        var webhookEventId = Guid.NewGuid();
        await service.StoreRawPayloadAsync(webhookEventId, PaymentProvider.Card, "{}");

        await service.MarkAsProcessedAsync(webhookEventId);

        var reloaded = await service.GetRawPayloadAsync(webhookEventId);
        reloaded!.IsProcessed.Should().BeTrue();
        reloaded.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUnprocessedByProviderAsync_OnlyReturnsUnprocessedDocumentsForThatProvider()
    {
        var service = CreateService($"webhooks-{Guid.NewGuid():N}");

        var unprocessedId = Guid.NewGuid();
        var processedId = Guid.NewGuid();
        await service.StoreRawPayloadAsync(unprocessedId, PaymentProvider.BankTransfer, "{}");
        await service.StoreRawPayloadAsync(processedId, PaymentProvider.BankTransfer, "{}");
        await service.MarkAsProcessedAsync(processedId);

        var unprocessed = await service.GetUnprocessedByProviderAsync(PaymentProvider.BankTransfer);

        unprocessed.Should().ContainSingle(d => d.WebhookEventId == unprocessedId);
        unprocessed.Should().NotContain(d => d.WebhookEventId == processedId);
    }

    [Fact]
    public async Task GetRawPayloadAsync_UnknownId_ReturnsNull()
    {
        var service = CreateService($"webhooks-{Guid.NewGuid():N}");

        var result = await service.GetRawPayloadAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}

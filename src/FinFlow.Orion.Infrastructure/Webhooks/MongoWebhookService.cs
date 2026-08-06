using FinFlow.Orion.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace FinFlow.Orion.Infrastructure.Webhooks;

// ── MongoDB document (internal representation) ───────────────────────────────

internal sealed class WebhookRawMongoDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid WebhookEventId { get; init; }

    [BsonElement("provider")]
    public string Provider { get; init; } = string.Empty;

    [BsonElement("rawPayload")]
    public string RawPayload { get; init; } = string.Empty;

    [BsonElement("isProcessed")]
    public bool IsProcessed { get; set; }

    [BsonElement("receivedAt")]
    public DateTime ReceivedAt { get; init; }

    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; set; }
}

// ── Configuration ────────────────────────────────────────────────────────────

public sealed class MongoWebhookConfiguration
{
    public const string SectionName = "MongoDB";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "FinFlowOrion";
    public string CollectionName { get; init; } = "WebhookRawPayloads";
}

// ── Service implementation ────────────────────────────────────────────────────

public sealed class MongoWebhookService : IMongoWebhookService
{
    private readonly IMongoCollection<WebhookRawMongoDocument> _collection;
    private readonly ILogger<MongoWebhookService> _logger;

    public MongoWebhookService(
        IOptions<MongoWebhookConfiguration> config,
        ILogger<MongoWebhookService> logger)
    {
        _logger = logger;

        var settings = config.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<WebhookRawMongoDocument>(settings.CollectionName);

        // Ensure indexes on startup
        EnsureIndexes();
    }

    public async Task StoreRawPayloadAsync(
        Guid webhookEventId,
        PaymentProvider provider,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        var document = new WebhookRawMongoDocument
        {
            WebhookEventId = webhookEventId,
            Provider = provider.ToString(),
            RawPayload = rawPayload,
            IsProcessed = false,
            ReceivedAt = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "[MongoDB] Stored raw webhook payload — EventId: {Id} | Provider: {Provider}",
            webhookEventId, provider);
    }

    public async Task<WebhookRawDocument?> GetRawPayloadAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<WebhookRawMongoDocument>.Filter
            .Eq(d => d.WebhookEventId, webhookEventId);

        var document = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null) return null;

        return MapToDto(document);
    }

    public async Task<IReadOnlyList<WebhookRawDocument>> GetUnprocessedByProviderAsync(
        PaymentProvider provider,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<WebhookRawMongoDocument>.Filter.And(
            Builders<WebhookRawMongoDocument>.Filter.Eq(d => d.Provider, provider.ToString()),
            Builders<WebhookRawMongoDocument>.Filter.Eq(d => d.IsProcessed, false));

        var documents = await _collection
            .Find(filter)
            .SortBy(d => d.ReceivedAt)
            .Limit(batchSize)
            .ToListAsync(cancellationToken);

        return documents.Select(MapToDto).ToList();
    }

    public async Task MarkAsProcessedAsync(
        Guid webhookEventId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<WebhookRawMongoDocument>.Filter
            .Eq(d => d.WebhookEventId, webhookEventId);

        var update = Builders<WebhookRawMongoDocument>.Update
            .Set(d => d.IsProcessed, true)
            .Set(d => d.ProcessedAt, DateTime.UtcNow);

        var result = await _collection.UpdateOneAsync(
            filter, update, cancellationToken: cancellationToken);

        if (result.ModifiedCount == 0)
            _logger.LogWarning(
                "[MongoDB] No document found to mark processed — EventId: {Id}", webhookEventId);
        else
            _logger.LogInformation(
                "[MongoDB] Marked processed — EventId: {Id}", webhookEventId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static WebhookRawDocument MapToDto(WebhookRawMongoDocument doc) => new()
    {
        WebhookEventId = doc.WebhookEventId,
        Provider = doc.Provider,
        RawPayload = doc.RawPayload,
        IsProcessed = doc.IsProcessed,
        ReceivedAt = doc.ReceivedAt,
        ProcessedAt = doc.ProcessedAt
    };

    private void EnsureIndexes()
    {
        var providerIndex = new CreateIndexModel<WebhookRawMongoDocument>(
            Builders<WebhookRawMongoDocument>.IndexKeys.Ascending(d => d.Provider));

        var isProcessedIndex = new CreateIndexModel<WebhookRawMongoDocument>(
            Builders<WebhookRawMongoDocument>.IndexKeys.Ascending(d => d.IsProcessed));

        var receivedAtIndex = new CreateIndexModel<WebhookRawMongoDocument>(
            Builders<WebhookRawMongoDocument>.IndexKeys.Descending(d => d.ReceivedAt));

        var compoundIndex = new CreateIndexModel<WebhookRawMongoDocument>(
            Builders<WebhookRawMongoDocument>.IndexKeys
                .Ascending(d => d.Provider)
                .Ascending(d => d.IsProcessed)
                .Descending(d => d.ReceivedAt));

        _collection.Indexes.CreateMany(
        [
            providerIndex,
            isProcessedIndex,
            receivedAtIndex,
            compoundIndex
        ]);
    }
}
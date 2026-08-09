using System.Text.Json;
using FinFlow.Orion.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Workers.Services;

public interface IWorkerOutboxPublisher
{
    Task PublishAsync(
        string type,
        string payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Worker-side outbox publisher. Resolves the stored payload's CLR type via
/// IntegrationEventMap.TypeRegistry, deserializes it, and dispatches it onto the
/// MassTransit bus. Kept separate from the API-side outbox writer to avoid
/// circular dependencies.
/// </summary>
public sealed class WorkerOutboxPublisher : IWorkerOutboxPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<WorkerOutboxPublisher> _logger;

    public WorkerOutboxPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<WorkerOutboxPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync(
        string type,
        string payload,
        CancellationToken cancellationToken = default)
    {
        if (!IntegrationEventMap.TypeRegistry.TryGetValue(type, out var eventType))
        {
            _logger.LogWarning(
                "[WorkerOutboxPublisher] Unknown outbox message type {Type} — skipping publish. " +
                "It will still be marked processed so it doesn't block the batch.",
                type);
            return;
        }

        var @event = JsonSerializer.Deserialize(payload, eventType)
            ?? throw new InvalidOperationException($"Failed to deserialize outbox payload as {eventType.Name}.");

        await _publishEndpoint.Publish(@event, eventType, cancellationToken);

        _logger.LogInformation(
            "[WorkerOutboxPublisher] Published {Type}", type);
    }
}

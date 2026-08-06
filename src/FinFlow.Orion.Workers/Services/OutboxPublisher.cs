using FinFlow.Orion.Infrastructure.Persistence.Outbox;
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
/// Worker-side outbox publisher.
/// Deserializes the stored payload and dispatches it to MassTransit.
/// Kept separate from the API-side OutboxPublisher to avoid circular dependencies.
/// </summary>
public sealed class WorkerOutboxPublisher : IWorkerOutboxPublisher
{
    private readonly ILogger<WorkerOutboxPublisher> _logger;

    public WorkerOutboxPublisher(ILogger<WorkerOutboxPublisher> logger)
        => _logger = logger;

    public Task PublishAsync(
        string type,
        string payload,
        CancellationToken cancellationToken = default)
    {
        // TODO: Resolve the actual Type from the assembly and publish via MassTransit
        // Example:
        //   var eventType = Type.GetType(type);
        //   var @event    = JsonSerializer.Deserialize(payload, eventType!);
        //   await _publishEndpoint.Publish(@event!, eventType!, cancellationToken);

        _logger.LogInformation(
            "[WorkerOutboxPublisher] Publishing — Type: {Type}", type);

        return Task.CompletedTask;
    }
}
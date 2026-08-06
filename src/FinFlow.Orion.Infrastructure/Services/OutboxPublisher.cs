using FinFlow.Orion.Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Infrastructure.Services;

public interface IOutboxPublisher
{
    Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default);
}

public sealed class OutboxPublisher : IOutboxPublisher
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IOutboxService outboxService,
        ILogger<OutboxPublisher> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    public async Task PublishAsync(
        string type,
        string payload,
        CancellationToken cancellationToken = default)
    {
        var message = OutboxMessage.Create(
            type: type,
            payload: payload,
            aggregateId: Guid.Empty.ToString(),
            aggregateType: "Unknown");

        await _outboxService.MarkAsProcessedAsync(message.Id, cancellationToken);

        _logger.LogInformation(
            "[OutboxPublisher] Published message — Type: {Type} | Id: {Id}",
            type, message.Id);
    }
}
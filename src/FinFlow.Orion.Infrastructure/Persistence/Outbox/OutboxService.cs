using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Infrastructure.Persistence.Outbox;

public interface IOutboxService
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}

public sealed class OutboxService : IOutboxService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OutboxService> _logger;

    public OutboxService(
        ApplicationDbContext dbContext,
        ILogger<OutboxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync([messageId], cancellationToken);
        if (message is null)
        {
            _logger.LogWarning("Outbox message {Id} not found for marking as processed.", messageId);
            return;
        }

        message.MarkAsCompleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FindAsync([messageId], cancellationToken);
        if (message is null)
        {
            _logger.LogWarning("Outbox message {Id} not found for marking as failed.", messageId);
            return;
        }

        message.MarkAsFailed(error);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
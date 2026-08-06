using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class WebhookRepository : IWebhookRepository
{
    private readonly ApplicationDbContext _dbContext;

    public WebhookRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WebhookEvents
            .Include(w => w.Deliveries)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookEvent>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WebhookEvents
            .Where(w => !w.IsProcessed)
            .OrderBy(w => w.ReceivedAt)
            .Take(batchSize)
            .Include(w => w.Deliveries)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.WebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public async Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.WebhookEvents.Update(webhookEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
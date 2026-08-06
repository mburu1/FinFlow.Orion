using FinFlow.Orion.Domain.Entities.Webhooks;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IWebhookRepository
{
    Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEvent>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
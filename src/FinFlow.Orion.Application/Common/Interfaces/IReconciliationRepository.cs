using FinFlow.Orion.Domain.Entities.Reconciliation;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IReconciliationRepository
{
    Task<ReconciliationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Discrepancy?> GetDiscrepancyByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ReconciliationReport report, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReconciliationReport report, CancellationToken cancellationToken = default);
}
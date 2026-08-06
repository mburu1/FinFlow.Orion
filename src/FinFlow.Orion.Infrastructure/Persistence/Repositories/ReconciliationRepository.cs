using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Reconciliation;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class ReconciliationRepository : IReconciliationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReconciliationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReconciliationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ReconciliationReports
            .Include(r => r.Items)
            .Include(r => r.Discrepancies)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Discrepancy?> GetDiscrepancyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Discrepancies
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddAsync(ReconciliationReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.ReconciliationReports.AddAsync(report, cancellationToken);
    }

    public async Task UpdateAsync(ReconciliationReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.ReconciliationReports.Update(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
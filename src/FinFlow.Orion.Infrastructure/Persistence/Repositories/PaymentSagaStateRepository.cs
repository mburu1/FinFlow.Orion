using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Sagas;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class PaymentSagaStateRepository : IPaymentSagaStateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentSagaStateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentSagaState?> GetActiveByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentSagaStates
            .Where(s => s.PaymentId == paymentId && !s.IsCompleted)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentSagaState state, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentSagaStates.AddAsync(state, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PaymentSagaState state, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentSagaStates.Update(state);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

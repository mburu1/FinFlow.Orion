using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Orion.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Attempts)
            .Include(p => p.ProviderResponse)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Attempts)
            .FirstOrDefaultAsync(p => p.Reference.Reference == reference, cancellationToken);
    }

    public async Task<Payment?> GetByProviderTransactionIdAsync(string providerTransactionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Include(p => p.Attempts)
            .FirstOrDefaultAsync(p => p.ProviderResponse!.ProviderTransactionId == providerTransactionId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Payments
            .Include(p => p.Attempts)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _dbContext.Payments.Update(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
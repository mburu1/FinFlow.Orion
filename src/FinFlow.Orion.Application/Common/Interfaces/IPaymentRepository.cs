using FinFlow.Orion.Domain.Entities.Payments;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}
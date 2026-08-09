using FinFlow.Orion.Application.Sagas;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IPaymentSagaStateRepository
{
    Task<PaymentSagaState?> GetActiveByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentSagaState state, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentSagaState state, CancellationToken cancellationToken = default);
}

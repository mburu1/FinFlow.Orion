namespace FinFlow.Orion.Application.Sagas;

public interface IPaymentSagaOrchestrator
{
    Task StartAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task HandleFailureAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default);
    Task CompensateAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
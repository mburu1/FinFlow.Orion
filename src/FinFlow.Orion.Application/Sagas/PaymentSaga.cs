using FinFlow.Orion.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Sagas;

public sealed class PaymentSaga : IPaymentSagaOrchestrator
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentSaga> _logger;

    // Provider fallback chain: MPesa → Card → BankTransfer
    private static readonly string[] FallbackChain = ["MPesa", "Card", "BankTransfer"];

    public PaymentSaga(
        IPaymentRepository paymentRepository,
        ILogger<PaymentSaga> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task StartAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            _logger.LogError("[Saga] Payment {Id} not found. Cannot start saga.", paymentId);
            return;
        }

        var state = new PaymentSagaState
        {
            PaymentId = paymentId,
            CurrentStep = "PaymentInitiated",
            StartedAt = DateTime.UtcNow
        };

        state.CompletedSteps.Add("PaymentInitiated");

        _logger.LogInformation("[Saga] Started for payment {Id} via {Provider}",
            paymentId, payment.Provider);
    }

    public async Task HandleFailureAsync(
        Guid paymentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null) return;

        var currentProviderIndex = Array.IndexOf(FallbackChain, payment.Provider.ToString());
        var nextProvider = currentProviderIndex < FallbackChain.Length - 1
            ? FallbackChain[currentProviderIndex + 1]
            : null;

        if (nextProvider is not null)
        {
            _logger.LogInformation(
                "[Saga] Payment {Id} failed via {Current}. Routing to fallback: {Next}",
                paymentId, payment.Provider, nextProvider);
        }
        else
        {
            _logger.LogWarning(
                "[Saga] Payment {Id} exhausted all providers. Compensating.",
                paymentId);

            await CompensateAsync(paymentId, cancellationToken);
        }
    }

    public async Task CompensateAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null) return;

        _logger.LogWarning("[Saga] Compensating payment {Reference}. Marking as failed.",
            payment.Reference.Reference);
    }
}
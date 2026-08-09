using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Sagas;

/// <summary>
/// Orchestrates the M-Pesa → Card → BankTransfer provider fallback chain for a
/// failed payment. Persists its progress via IPaymentSagaStateRepository so it
/// survives process restarts, and drives retries by sending RetryPaymentCommand
/// through MediatR — never by mutating the payment directly.
/// </summary>
public sealed class PaymentSaga : IPaymentSagaOrchestrator
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentSagaStateRepository _sagaStateRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentSaga> _logger;

    // Provider fallback chain: MPesa → Card → BankTransfer
    private static readonly string[] FallbackChain = ["MPesa", "Card", "BankTransfer"];

    public PaymentSaga(
        IPaymentRepository paymentRepository,
        IPaymentSagaStateRepository sagaStateRepository,
        IMediator mediator,
        ILogger<PaymentSaga> logger)
    {
        _paymentRepository = paymentRepository;
        _sagaStateRepository = sagaStateRepository;
        _mediator = mediator;
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

        var existing = await _sagaStateRepository.GetActiveByPaymentIdAsync(paymentId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogDebug("[Saga] Active saga already exists for payment {Id} — skipping start.", paymentId);
            return;
        }

        var state = new PaymentSagaState
        {
            PaymentId = paymentId,
            CurrentStep = "PaymentInitiated",
            StartedAt = DateTime.UtcNow
        };

        state.CompletedSteps.Add("PaymentInitiated");

        await _sagaStateRepository.AddAsync(state, cancellationToken);

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

        var state = await _sagaStateRepository.GetActiveByPaymentIdAsync(paymentId, cancellationToken);
        if (state is null)
        {
            state = new PaymentSagaState
            {
                PaymentId = paymentId,
                CurrentStep = "PaymentInitiated",
                StartedAt = DateTime.UtcNow
            };
            state.CompletedSteps.Add("PaymentInitiated");
            await _sagaStateRepository.AddAsync(state, cancellationToken);
        }

        state.RetryCount++;
        state.LastFailureReason = reason;

        var currentProviderIndex = Array.IndexOf(FallbackChain, payment.Provider.ToString());
        var nextProvider = currentProviderIndex >= 0 && currentProviderIndex < FallbackChain.Length - 1
            ? FallbackChain[currentProviderIndex + 1]
            : null;

        if (nextProvider is not null && state.CanRetry)
        {
            state.FallbackProvider = nextProvider;
            state.CurrentStep = $"FallbackTo:{nextProvider}";
            state.CompletedSteps.Add(state.CurrentStep);
            await _sagaStateRepository.UpdateAsync(state, cancellationToken);

            _logger.LogInformation(
                "[Saga] Payment {Id} failed via {Current}. Routing to fallback: {Next}",
                paymentId, payment.Provider, nextProvider);

            await _mediator.Send(
                new RetryPaymentCommand(
                    PaymentId: paymentId,
                    IdempotencyKey: $"saga-retry-{paymentId:N}-{state.RetryCount}-{Guid.NewGuid():N}",
                    OverrideProvider: nextProvider),
                cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "[Saga] Payment {Id} exhausted all providers or retries. Compensating.",
                paymentId);

            state.IsCompensating = true;
            await _sagaStateRepository.UpdateAsync(state, cancellationToken);

            await CompensateInternalAsync(paymentId, state, cancellationToken);
        }
    }

    public async Task CompensateAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var state = await _sagaStateRepository.GetActiveByPaymentIdAsync(paymentId, cancellationToken);
        await CompensateInternalAsync(paymentId, state, cancellationToken);
    }

    private async Task CompensateInternalAsync(
        Guid paymentId,
        PaymentSagaState? state,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null) return;

        _logger.LogWarning(
            "[Saga] Compensating payment {Reference}. Terminal status: {Status}.",
            payment.Reference.Reference, payment.Status);

        if (state is null) return;

        state.IsCompleted = true;
        state.CompletedAt = DateTime.UtcNow;
        await _sagaStateRepository.UpdateAsync(state, cancellationToken);
    }
}

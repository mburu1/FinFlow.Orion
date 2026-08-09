using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Contracts.Payments.Responses;
using FinFlow.Orion.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Payments.Commands.RetryPayment;

public sealed class RetryPaymentCommandHandler
    : IRequestHandler<RetryPaymentCommand, InitiatePaymentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentProviderDispatcher _dispatcher;
    private readonly ILogger<RetryPaymentCommandHandler> _logger;

    public RetryPaymentCommandHandler(
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotencyService,
        IPaymentRepository paymentRepository,
        IPaymentProviderDispatcher dispatcher,
        ILogger<RetryPaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _idempotencyService = idempotencyService;
        _paymentRepository = paymentRepository;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task<InitiatePaymentResponse> Handle(
        RetryPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var cached = await _idempotencyService.GetAsync(request.IdempotencyKey, cancellationToken);
        if (cached is not null)
            throw new Domain.Exceptions.IdempotencyViolationException(request.IdempotencyKey);

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payments.Payment), request.PaymentId);

        if (payment.Status != PaymentStatus.Failed)
            throw new Domain.Exceptions.InvalidPaymentException(
                $"Cannot retry a payment in {payment.Status} status — only a Failed payment can be retried.");

        var resolvedProvider = payment.Provider;
        if (request.OverrideProvider is not null)
        {
            if (!Enum.TryParse<PaymentProvider>(request.OverrideProvider, true, out resolvedProvider))
                throw new NotFoundException($"Payment provider '{request.OverrideProvider}' is not supported.");

            _logger.LogInformation("[Retry] Routing payment {Id} to fallback provider {Provider}",
                payment.Id, resolvedProvider);
        }

        payment.ResetForRetry(resolvedProvider);

        var attemptNumber = payment.Attempts.Count + 1;
        await PaymentDispatchProcessor.DispatchAndTransitionAsync(
            payment,
            _dispatcher,
            attemptNumber,
            _logger,
            bankTransferDetails: null,
            cancellationToken);

        await _idempotencyService.StoreAsync(
            request.IdempotencyKey,
            payment.Id.ToString(),
            TimeSpan.FromHours(24),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Retry] Retried payment {Reference} via {Provider} — Status: {Status}",
            payment.Reference.Reference, resolvedProvider, payment.Status);

        return new InitiatePaymentResponse
        {
            PaymentId = payment.Id,
            Reference = payment.Reference.Reference,
            Status = payment.Status.ToString(),
            Amount = payment.Amount.Amount,
            CurrencyCode = payment.Amount.CurrencyCode,
            Provider = payment.Provider.ToString(),
            CreatedAt = payment.CreatedAt
        };
    }
}

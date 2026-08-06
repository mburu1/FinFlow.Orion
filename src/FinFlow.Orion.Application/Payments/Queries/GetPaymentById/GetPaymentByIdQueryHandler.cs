using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using MediatR;

namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentById;

public sealed class GetPaymentByIdQueryHandler
    : IRequestHandler<GetPaymentByIdQuery, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
        => _paymentRepository = paymentRepository;

    public async Task<PaymentDto> Handle(
        GetPaymentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Payments.Payment), request.PaymentId);

        return new PaymentDto
        {
            PaymentId = payment.Id,
            Reference = payment.Reference.Reference,
            Amount = payment.Amount.Amount,
            CurrencyCode = payment.Amount.CurrencyCode,
            Status = payment.Status.ToString(),
            Provider = payment.Provider.ToString(),
            Channel = payment.Channel.ToString(),
            CustomerId = payment.CustomerId,
            PhoneNumber = payment.PhoneNumber?.ToString(),
            Description = payment.Description,
            ProviderTransactionId = payment.ProviderResponse?.ProviderTransactionId,
            FailureReason = payment.ProviderResponse?.Message,
            AttemptCount = payment.Attempts.Count,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
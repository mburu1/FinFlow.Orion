using FinFlow.Orion.Contracts.Payments.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Payments.Commands.RetryPayment;

public sealed record RetryPaymentCommand(
    Guid PaymentId,
    string IdempotencyKey,
    string? OverrideProvider = null
) : IRequest<InitiatePaymentResponse>;
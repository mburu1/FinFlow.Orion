using MediatR;

namespace FinFlow.Orion.Application.Payments.Commands.ReversePayment;

public sealed record ReversePaymentCommand(
    Guid PaymentId,
    string Reason,
    string RequestedBy,
    string IdempotencyKey
) : IRequest<bool>;
using FinFlow.Orion.Contracts.Payments.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Payments.Commands.InitiatePayment;

public sealed record InitiatePaymentCommand(
    decimal Amount,
    string CurrencyCode,
    string Provider,
    string Channel,
    string IdempotencyKey,
    string? CustomerId,
    string? PhoneNumber,
    string? Description,
    Dictionary<string, string>? Metadata = null
) : IRequest<InitiatePaymentResponse>;
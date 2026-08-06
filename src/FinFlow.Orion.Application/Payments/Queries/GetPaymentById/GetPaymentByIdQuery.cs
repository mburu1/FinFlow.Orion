using MediatR;

namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentById;

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDto>;
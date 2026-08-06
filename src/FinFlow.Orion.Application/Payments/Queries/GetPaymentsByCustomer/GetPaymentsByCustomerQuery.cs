using FinFlow.Orion.Contracts.Common;
using MediatR;

namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentsByCustomer;

public sealed record GetPaymentsByCustomerQuery(
    string CustomerId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResponse<PaymentSummaryDto>>;
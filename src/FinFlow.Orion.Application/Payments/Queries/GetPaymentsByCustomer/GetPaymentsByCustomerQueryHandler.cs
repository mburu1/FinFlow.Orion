using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Contracts.Common;
using MediatR;

namespace FinFlow.Orion.Application.Payments.Queries.GetPaymentsByCustomer;

public sealed class GetPaymentsByCustomerQueryHandler
    : IRequestHandler<GetPaymentsByCustomerQuery, PagedResponse<PaymentSummaryDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsByCustomerQueryHandler(IPaymentRepository paymentRepository)
        => _paymentRepository = paymentRepository;

    public async Task<PagedResponse<PaymentSummaryDto>> Handle(
        GetPaymentsByCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var (payments, totalCount) = await _paymentRepository
            .GetByCustomerIdAsync(request.CustomerId, request.Page, request.PageSize, cancellationToken);

        var dtos = payments.Select(p => new PaymentSummaryDto
        {
            PaymentId = p.Id,
            Reference = p.Reference.Reference,
            Amount = p.Amount.Amount,
            CurrencyCode = p.Amount.CurrencyCode,
            Status = p.Status.ToString(),
            Provider = p.Provider.ToString(),
            CreatedAt = p.CreatedAt
        }).ToList();

        return PagedResponse<PaymentSummaryDto>.Create(dtos, request.Page, request.PageSize, totalCount);
    }
}
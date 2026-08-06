using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Queries.GetDiscrepancies;

public sealed class GetDiscrepanciesQueryHandler
    : IRequestHandler<GetDiscrepanciesQuery, PagedResponse<DiscrepancyResponse>>
{
    private readonly IReconciliationRepository _reconciliationRepository;

    public GetDiscrepanciesQueryHandler(IReconciliationRepository reconciliationRepository)
        => _reconciliationRepository = reconciliationRepository;

    public async Task<PagedResponse<DiscrepancyResponse>> Handle(
        GetDiscrepanciesQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _reconciliationRepository.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.Reconciliation.ReconciliationReport), request.ReportId);

        var discrepancies = report.Discrepancies
            .Where(d => request.UnresolvedOnly == true ? !d.IsResolved : true)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DiscrepancyResponse
            {
                DiscrepancyId = d.Id,
                ReportId = d.ReportId,
                PaymentReference = d.PaymentReference,
                DiscrepancyType = d.DiscrepancyType,
                InternalAmount = d.InternalAmount.Amount,
                ProviderAmount = d.ProviderAmount.Amount,
                DifferenceAmount = d.DifferenceAmount.Amount,
                CurrencyCode = d.InternalAmount.CurrencyCode,
                IsResolved = d.IsResolved,
                ResolvedBy = d.ResolvedBy,
                ResolvedAt = d.ResolvedAt,
                Notes = d.Notes,
                DetectedAt = d.DetectedAt
            })
            .ToList();

        return PagedResponse<DiscrepancyResponse>.Create(
            discrepancies,
            request.Page,
            request.PageSize,
            report.Discrepancies.Count);
    }
}
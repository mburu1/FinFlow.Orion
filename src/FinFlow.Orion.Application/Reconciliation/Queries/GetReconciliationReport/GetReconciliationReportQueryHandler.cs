using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Queries.GetReconciliationReport;

public sealed class GetReconciliationReportQueryHandler
    : IRequestHandler<GetReconciliationReportQuery, ReconciliationReportResponse>
{
    private readonly IReconciliationRepository _reconciliationRepository;

    public GetReconciliationReportQueryHandler(
        IReconciliationRepository reconciliationRepository)
        => _reconciliationRepository = reconciliationRepository;

    public async Task<ReconciliationReportResponse> Handle(
        GetReconciliationReportQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _reconciliationRepository.GetByIdAsync(request.ReportId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.Reconciliation.ReconciliationReport), request.ReportId);

        return new ReconciliationReportResponse
        {
            ReportId = report.Id,
            ReportReference = report.ReportReference,
            Provider = report.Provider.ToString(),
            ReconDate = report.ReconDate,
            Status = report.Status.ToString(),
            TotalTransactions = report.TotalTransactions,
            MatchedCount = report.MatchedCount,
            UnmatchedCount = report.UnmatchedCount,
            TotalMatchedAmount = report.TotalMatchedAmount.Amount,
            TotalDiscrepancyAmount = report.TotalDiscrepancyAmount.Amount,
            CurrencyCode = report.TotalMatchedAmount.CurrencyCode,
            CreatedAt = report.CreatedAt,
            CompletedAt = report.CompletedAt
        };
    }
}
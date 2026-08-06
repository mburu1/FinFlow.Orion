using FinFlow.Orion.Contracts.Reconciliation.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Queries.GetReconciliationReport;

public sealed record GetReconciliationReportQuery(Guid ReportId)
    : IRequest<ReconciliationReportResponse>;
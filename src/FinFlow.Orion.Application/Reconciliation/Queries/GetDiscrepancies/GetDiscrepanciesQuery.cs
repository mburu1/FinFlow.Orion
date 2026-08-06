using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Queries.GetDiscrepancies;

public sealed record GetDiscrepanciesQuery(
    Guid ReportId,
    bool? UnresolvedOnly = true,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResponse<DiscrepancyResponse>>;
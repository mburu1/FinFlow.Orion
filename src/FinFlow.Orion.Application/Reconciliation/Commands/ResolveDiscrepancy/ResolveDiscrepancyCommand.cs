using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Commands.ResolveDiscrepancy;

public sealed record ResolveDiscrepancyCommand(
    Guid DiscrepancyId,
    string ResolvedBy,
    string Notes
) : IRequest<bool>;
using MediatR;

namespace FinFlow.Orion.Application.Reconciliation.Commands.TriggerReconciliation;

public sealed record TriggerReconciliationCommand(
    string Provider,
    DateOnly ReconDate,
    string TriggeredBy
) : IRequest<Guid>;
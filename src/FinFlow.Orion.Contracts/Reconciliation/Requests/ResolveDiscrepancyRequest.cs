namespace FinFlow.Orion.Contracts.Reconciliation.Requests;

public sealed class ResolveDiscrepancyRequest
{
    public Guid DiscrepancyId { get; init; }
    public string ResolvedBy { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}
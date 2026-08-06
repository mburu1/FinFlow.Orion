namespace FinFlow.Orion.Contracts.Reconciliation.Requests;

public sealed class TriggerReconciliationRequest
{
    public string Provider { get; init; } = string.Empty;
    public DateOnly ReconDate { get; init; }
    public string TriggeredBy { get; init; } = string.Empty;    // "manual" or "scheduler"
}
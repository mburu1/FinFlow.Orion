namespace FinFlow.Orion.Contracts.Reconciliation.Events;

public sealed class ReconciliationCompletedIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; init; }
    public string ReportReference { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public int MatchedCount { get; init; }
    public int UnmatchedCount { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
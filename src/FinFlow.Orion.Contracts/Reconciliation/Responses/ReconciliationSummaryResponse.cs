namespace FinFlow.Orion.Contracts.Reconciliation.Responses;

public sealed class ReconciliationSummaryResponse
{
    public Guid ReportId { get; init; }
    public string ReportReference { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public DateOnly ReconDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalTransactions { get; init; }
    public int MatchedCount { get; init; }
    public int UnmatchedCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
namespace FinFlow.Orion.Contracts.Reconciliation.Responses;

public sealed class ReconciliationReportResponse
{
    public Guid ReportId { get; init; }
    public string ReportReference { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public DateOnly ReconDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalTransactions { get; init; }
    public int MatchedCount { get; init; }
    public int UnmatchedCount { get; init; }
    public decimal TotalMatchedAmount { get; init; }
    public decimal TotalDiscrepancyAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
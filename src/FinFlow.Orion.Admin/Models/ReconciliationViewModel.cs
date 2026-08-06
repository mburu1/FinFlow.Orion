namespace FinFlow.Orion.Admin.Models;

public sealed class ReconciliationViewModel
{
    public Guid ReportId { get; set; }
    public string ReportReference { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateOnly ReconDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalTransactions { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public decimal TotalMatchedAmount { get; set; }
    public decimal TotalDiscrepancyAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
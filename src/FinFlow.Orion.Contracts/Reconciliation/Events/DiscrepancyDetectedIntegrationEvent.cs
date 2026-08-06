namespace FinFlow.Orion.Contracts.Reconciliation.Events;

public sealed class DiscrepancyDetectedIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid ReportId { get; init; }
    public string PaymentReference { get; init; } = string.Empty;
    public decimal DifferenceAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
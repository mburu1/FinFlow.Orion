namespace FinFlow.Orion.Contracts.Reconciliation.Responses;

public sealed class DiscrepancyResponse
{
    public Guid DiscrepancyId { get; init; }
    public Guid ReportId { get; init; }
    public string PaymentReference { get; init; } = string.Empty;
    public string DiscrepancyType { get; init; } = string.Empty;
    public decimal InternalAmount { get; init; }
    public decimal ProviderAmount { get; init; }
    public decimal DifferenceAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime DetectedAt { get; init; }

    // ✅ Returns a cloned instance with IsResolved = true
    public DiscrepancyResponse WithResolved(string? resolvedBy = null) => new()
    {
        DiscrepancyId = DiscrepancyId,
        ReportId = ReportId,
        PaymentReference = PaymentReference,
        DiscrepancyType = DiscrepancyType,
        InternalAmount = InternalAmount,
        ProviderAmount = ProviderAmount,
        DifferenceAmount = DifferenceAmount,
        CurrencyCode = CurrencyCode,
        IsResolved = true,
        ResolvedBy = resolvedBy,
        ResolvedAt = DateTime.UtcNow,
        Notes = Notes,
        DetectedAt = DetectedAt
    };
}
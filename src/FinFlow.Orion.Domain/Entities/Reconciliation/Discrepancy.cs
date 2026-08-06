using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Reconciliation;

public sealed class Discrepancy : Entity
{
    public Guid ReportId { get; private set; }
    public string PaymentReference { get; private set; } = null!;
    public string DiscrepancyType { get; private set; } = null!;  // AmountMismatch, StatusMismatch, Missing
    public Money InternalAmount { get; private set; } = null!;
    public Money ProviderAmount { get; private set; } = null!;
    public Money DifferenceAmount { get; private set; } = null!;
    public string? Notes { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public DateTime DetectedAt { get; private set; }

    private Discrepancy() { } // EF Core

    public static Discrepancy Create(
        Guid reportId,
        string paymentReference,
        string discrepancyType,
        Money internalAmount,
        Money providerAmount,
        string? notes = null)
    {
        var diff = internalAmount.Amount >= providerAmount.Amount
            ? internalAmount.Subtract(providerAmount)
            : providerAmount.Subtract(internalAmount);

        return new Discrepancy
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            PaymentReference = paymentReference,
            DiscrepancyType = discrepancyType,
            InternalAmount = internalAmount,
            ProviderAmount = providerAmount,
            DifferenceAmount = diff,
            Notes = notes,
            IsResolved = false,
            DetectedAt = DateTime.UtcNow
        };
    }

    public void Resolve(string resolvedBy, string? notes = null)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        Notes = notes;
    }
}
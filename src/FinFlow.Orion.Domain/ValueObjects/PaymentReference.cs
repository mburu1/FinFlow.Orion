using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.ValueObjects;

public class PaymentReference : ValueObject
{
    public string Reference { get; } = null!;

    private PaymentReference() { }

    public PaymentReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length < 8)
            throw new ArgumentException("Invalid payment reference.", nameof(reference));

        Reference = reference.ToUpperInvariant();
    }

    public static PaymentReference Generate()
        => new(Guid.NewGuid().ToString("N")[..12]);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Reference;
    }
}
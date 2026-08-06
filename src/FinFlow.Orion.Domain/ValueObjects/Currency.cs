using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.ValueObjects;

public class Currency : ValueObject
{
    public string Code { get; } = null!;
    public string Name { get; } = null!;
    public int DecimalPlaces { get; }

    private Currency() { }

    public Currency(string code, string name, int decimalPlaces = 2)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Invalid currency code.", nameof(code));

        Code = code.ToUpperInvariant();
        Name = name;
        DecimalPlaces = decimalPlaces;
    }

    public static Currency KES => new("KES", "Kenyan Shilling");
    public static Currency USD => new("USD", "US Dollar");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}
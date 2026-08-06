using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    public string Number { get; } = null!;
    public string CountryCode { get; } = null!;

    private PhoneNumber() { }

    public PhoneNumber(string number, string countryCode = "254")
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Phone number cannot be empty.", nameof(number));

        Number = CleanNumber(number);
        CountryCode = countryCode;
    }

    private static string CleanNumber(string number)
    {
        var cleaned = new string(number.Where(char.IsDigit).ToArray());
        return cleaned.StartsWith("254") ? cleaned : $"254{cleaned.TrimStart('0')}";
    }

    public override string ToString() => $"+{CountryCode} {Number.Substring(3)}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Number;
        yield return CountryCode;
    }
}
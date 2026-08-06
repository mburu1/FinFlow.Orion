using FinFlow.Orion.Domain.Primitives;
using System.Globalization;

namespace FinFlow.Orion.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    private Money() { } // EF Core

    public Money(decimal amount, string currencyCode)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new ArgumentException("Invalid currency code.", nameof(currencyCode));

        Amount = amount;
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public static Money Zero(string currency = "KES") => new(0, currency);

    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException("Cannot add money with different currencies.");

        return new Money(Amount + other.Amount, CurrencyCode);
    }

    public Money Subtract(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");

        return new Money(Amount - other.Amount, CurrencyCode);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
    }

    public override string ToString()
        => $"{Amount.ToString("F2", CultureInfo.CurrentCulture)} {CurrencyCode}";
}
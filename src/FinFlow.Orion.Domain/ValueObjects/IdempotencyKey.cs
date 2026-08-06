using FinFlow.Orion.Domain.Primitives;
using System.Security.Cryptography;
using System.Text;

namespace FinFlow.Orion.Domain.ValueObjects;

public class IdempotencyKey : ValueObject
{
    public string Value { get; }

    private IdempotencyKey() { }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 16)
            throw new ArgumentException("Invalid idempotency key.", nameof(value));

        Value = value;
    }

    public static IdempotencyKey Generate(string requestId, string userId)
    {
        var input = $"{requestId}:{userId}:{DateTime.UtcNow:O}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new IdempotencyKey(Convert.ToBase64String(hash));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.ValueObjects;

public class ProviderResponse : ValueObject
{
    public string ProviderTransactionId { get; } = null!;
    public string Status { get; } = null!;
    public string? Message { get; }
    public DateTime ReceivedAt { get; }

    private ProviderResponse() { }

    public ProviderResponse(string providerTransactionId, string status, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            throw new ArgumentException("Provider transaction ID is required.");

        ProviderTransactionId = providerTransactionId;
        Status = status;
        Message = message;
        ReceivedAt = DateTime.UtcNow;
    }

    public bool IsSuccessful => Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ProviderTransactionId;
        yield return Status;
        yield return Message;
    }
}
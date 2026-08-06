using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Primitives;

namespace FinFlow.Orion.Domain.Entities.Payments;

public sealed class PaymentProviderConfig : Entity
{
    public string Name { get; private set; } = null!;
    public PaymentProvider ProviderType { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }          // Lower = higher priority
    public int MaxRetries { get; private set; }
    public TimeSpan RetryDelay { get; private set; }
    public string BaseUrl { get; private set; } = null!;
    public string? WebhookSecret { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private PaymentProviderConfig() { } // EF Core

    public static PaymentProviderConfig Create(
        string name,
        PaymentProvider providerType,
        string baseUrl,
        int priority = 1,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        string? webhookSecret = null)
    {
        return new PaymentProviderConfig
        {
            Id = Guid.NewGuid(),
            Name = name,
            ProviderType = providerType,
            BaseUrl = baseUrl,
            Priority = priority,
            MaxRetries = maxRetries,
            RetryDelay = retryDelay ?? TimeSpan.FromSeconds(2),
            WebhookSecret = webhookSecret,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void UpdatePriority(int priority) { Priority = priority; UpdatedAt = DateTime.UtcNow; }
}
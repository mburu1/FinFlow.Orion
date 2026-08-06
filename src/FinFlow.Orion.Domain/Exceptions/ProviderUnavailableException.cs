using FinFlow.Orion.Domain.Enums;

namespace FinFlow.Orion.Domain.Exceptions;

public class ProviderUnavailableException : DomainException
{
    public PaymentProvider Provider { get; }

    public ProviderUnavailableException(PaymentProvider provider, string message)
        : base($"Payment provider {provider} is unavailable: {message}")
    {
        Provider = provider;
    }
}
using FinFlow.Orion.Domain.Abstractions;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Events.Payments;
using FinFlow.Orion.Domain.Exceptions;
using FinFlow.Orion.Domain.Primitives;
using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Entities.Payments;

public sealed class Payment : AggregateRoot, IAuditableEntity
{
    public PaymentReference Reference { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public PaymentChannel Channel { get; private set; }
    public IdempotencyKey IdempotencyKey { get; private set; } = null!;
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Description { get; private set; }
    public string? CustomerId { get; private set; }
    public ProviderResponse? ProviderResponse { get; private set; }

    private readonly List<PaymentAttempt> _attempts = [];
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts.AsReadOnly();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    private Payment() { } // EF Core

    public static Payment Create(
        Money amount,
        PaymentProvider provider,
        PaymentChannel channel,
        IdempotencyKey idempotencyKey,
        string? customerId = null,
        PhoneNumber? phoneNumber = null,
        string? description = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Reference = PaymentReference.Generate(),
            Amount = amount,
            Provider = provider,
            Channel = channel,
            IdempotencyKey = idempotencyKey,
            Status = PaymentStatus.Pending,
            CustomerId = customerId,
            PhoneNumber = phoneNumber,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        payment.AddDomainEvent(new PaymentInitiatedEvent(
            payment.Id,
            payment.Reference.Reference,
            amount,
            provider,
            idempotencyKey.Value));

        return payment;
    }

    public void MarkAsAuthorized(ProviderResponse response)
    {
        EnsureStatus(PaymentStatus.Pending);
        Status = PaymentStatus.Authorized;
        ProviderResponse = response;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsCaptured(ProviderResponse response)
    {
        EnsureStatus(PaymentStatus.Authorized);
        Status = PaymentStatus.Captured;
        ProviderResponse = response;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentCompletedEvent(
            Id,
            Reference.Reference,
            Amount,
            Provider,
            response.ProviderTransactionId));
    }

    public void MarkAsFailed(ProviderResponse response)
    {
        if (Status is PaymentStatus.Captured or PaymentStatus.Refunded)
            throw new InvalidPaymentException($"Cannot fail a payment in {Status} status.");

        Status = PaymentStatus.Failed;
        ProviderResponse = response;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedEvent(
            Id,
            Reference.Reference,
            Amount,
            Provider,
            response.Message ?? "Unknown failure"));
    }

    public void Reverse(string reason)
    {
        EnsureStatus(PaymentStatus.Captured);
        Status = PaymentStatus.Reversed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentReversedEvent(
            Id,
            Reference.Reference,
            Amount,
            Provider,
            reason));
    }

    public void AddAttempt(PaymentAttempt attempt)
        => _attempts.Add(attempt);

    /// <summary>
    /// Resets a failed payment back to Pending under a (possibly different) provider,
    /// so it can be re-dispatched — used by the payment saga's fallback chain and
    /// manual retries.
    /// </summary>
    public void ResetForRetry(PaymentProvider newProvider)
    {
        EnsureStatus(PaymentStatus.Failed);
        Provider = newProvider;
        Status = PaymentStatus.Pending;
        ProviderResponse = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(PaymentStatus expected)
    {
        if (Status != expected)
            throw new InvalidPaymentException(
                $"Expected payment status {expected} but found {Status}.");
    }
}
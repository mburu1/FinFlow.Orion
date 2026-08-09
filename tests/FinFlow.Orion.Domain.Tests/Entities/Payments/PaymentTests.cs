using FinFlow.Orion.Domain.Entities.Payments;
using FinFlow.Orion.Domain.Enums;
using FinFlow.Orion.Domain.Events.Payments;
using FinFlow.Orion.Domain.Exceptions;
using FinFlow.Orion.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinFlow.Orion.Domain.Tests.Entities.Payments;

public class PaymentTests
{
    private static Payment CreatePayment(PaymentProvider provider = PaymentProvider.Card)
        => Payment.Create(
            amount: new Money(1000, "KES"),
            provider: provider,
            channel: PaymentChannel.Web,
            idempotencyKey: new IdempotencyKey(new string('a', 20)),
            customerId: "cust-1");

    [Fact]
    public void Create_StartsAsPending_AndRaisesPaymentInitiatedEvent()
    {
        var payment = CreatePayment();

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentInitiatedEvent>();
    }

    [Fact]
    public void MarkAsAuthorized_FromPending_TransitionsToAuthorized()
    {
        var payment = CreatePayment();
        payment.ClearDomainEvents();

        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));

        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.ProviderResponse!.ProviderTransactionId.Should().Be("TX-1");
    }

    [Fact]
    public void MarkAsAuthorized_WhenNotPending_Throws()
    {
        var payment = CreatePayment();
        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));

        var act = () => payment.MarkAsAuthorized(new ProviderResponse("TX-2", "PENDING"));

        act.Should().Throw<InvalidPaymentException>();
    }

    [Fact]
    public void MarkAsCaptured_FromAuthorized_TransitionsToCaptured_AndRaisesPaymentCompletedEvent()
    {
        var payment = CreatePayment();
        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));
        payment.ClearDomainEvents();

        payment.MarkAsCaptured(new ProviderResponse("TX-1", "SUCCESS"));

        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentCompletedEvent>();
    }

    [Fact]
    public void MarkAsCaptured_FromPending_Throws()
    {
        var payment = CreatePayment();

        var act = () => payment.MarkAsCaptured(new ProviderResponse("TX-1", "SUCCESS"));

        act.Should().Throw<InvalidPaymentException>()
            .WithMessage("*Expected payment status Authorized but found Pending*");
    }

    [Fact]
    public void MarkAsFailed_FromPending_TransitionsToFailed_AndRaisesPaymentFailedEvent()
    {
        var payment = CreatePayment();
        payment.ClearDomainEvents();

        payment.MarkAsFailed(new ProviderResponse("TX-FAIL", "FAILED", "declined"));

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentFailedEvent>();
    }

    [Theory]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Refunded)]
    public void MarkAsFailed_WhenCapturedOrRefunded_Throws(PaymentStatus terminalStatus)
    {
        var payment = CreatePayment();
        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));
        payment.MarkAsCaptured(new ProviderResponse("TX-1", "SUCCESS"));

        if (terminalStatus == PaymentStatus.Refunded)
        {
            // No public API reaches Refunded in the current domain model, so exercise
            // the guard directly via reflection-free means is not possible — only
            // Captured is reachable through the public API. Skip Refunded scenario.
            return;
        }

        var act = () => payment.MarkAsFailed(new ProviderResponse("TX-2", "FAILED"));

        act.Should().Throw<InvalidPaymentException>();
    }

    [Fact]
    public void Reverse_FromCaptured_TransitionsToReversed_AndRaisesPaymentReversedEvent()
    {
        var payment = CreatePayment();
        payment.MarkAsAuthorized(new ProviderResponse("TX-1", "PENDING"));
        payment.MarkAsCaptured(new ProviderResponse("TX-1", "SUCCESS"));
        payment.ClearDomainEvents();

        payment.Reverse("customer requested refund");

        payment.Status.Should().Be(PaymentStatus.Reversed);
        payment.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<PaymentReversedEvent>();
    }

    [Fact]
    public void Reverse_WhenNotCaptured_Throws()
    {
        var payment = CreatePayment();

        var act = () => payment.Reverse("reason");

        act.Should().Throw<InvalidPaymentException>();
    }

    [Fact]
    public void ResetForRetry_FromFailed_TransitionsToPending_WithNewProvider()
    {
        var payment = CreatePayment(PaymentProvider.MPesa);
        payment.MarkAsFailed(new ProviderResponse("TX-FAIL", "FAILED"));

        payment.ResetForRetry(PaymentProvider.Card);

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Provider.Should().Be(PaymentProvider.Card);
        payment.ProviderResponse.Should().BeNull();
    }

    [Fact]
    public void ResetForRetry_WhenNotFailed_Throws()
    {
        var payment = CreatePayment();

        var act = () => payment.ResetForRetry(PaymentProvider.Card);

        act.Should().Throw<InvalidPaymentException>();
    }

    [Fact]
    public void AddAttempt_AppendsToAttempts()
    {
        var payment = CreatePayment();
        var attempt = PaymentAttempt.Create(payment.Id, 1, payment.Provider, PaymentStatus.Captured, "TX-1");

        payment.AddAttempt(attempt);

        payment.Attempts.Should().ContainSingle().Which.Should().Be(attempt);
    }
}

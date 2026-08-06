using FinFlow.Orion.Domain.ValueObjects;

namespace FinFlow.Orion.Domain.Exceptions;

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(Money requested, Money available)
        : base($"Insufficient funds. Requested: {requested}, Available: {available}") { }
}
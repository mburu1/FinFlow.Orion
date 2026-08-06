namespace FinFlow.Orion.Domain.Exceptions;

public class InvalidPaymentException : DomainException
{
    public InvalidPaymentException(string message) : base(message) { }
}
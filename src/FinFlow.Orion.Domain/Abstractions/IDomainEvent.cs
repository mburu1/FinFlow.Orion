namespace FinFlow.Orion.Domain.Abstractions;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}
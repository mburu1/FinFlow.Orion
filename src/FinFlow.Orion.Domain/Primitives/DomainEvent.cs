using FinFlow.Orion.Domain.Abstractions;

namespace FinFlow.Orion.Domain.Primitives;

public abstract class DomainEvent : IDomainEvent
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
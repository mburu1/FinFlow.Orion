using FinFlow.Orion.Domain.Abstractions;
using MediatR;

namespace FinFlow.Orion.Domain.Primitives;

public abstract class DomainEvent : IDomainEvent, INotification
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
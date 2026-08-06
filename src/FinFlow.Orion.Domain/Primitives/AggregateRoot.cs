using FinFlow.Orion.Domain.Abstractions;

namespace FinFlow.Orion.Domain.Primitives;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    // Marker for aggregate roots (e.g., Payment, ReconciliationReport)
}
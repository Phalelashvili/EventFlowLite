using EventFlowLite.Abstractions.Command;

namespace EventFlowLite.Abstractions.Event;

public interface IDomainEvent<out TAggregate, out TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    TAggregate Aggregate { get; }
    TId AggregateId { get; }
    DateTime Timestamp { get; }
    Type AggregateEventType { get; }
}

// maybe use DomainEvent class directly?
public interface IDomainEvent<out TAggregate, out TId, out TAggregateEvent> : IDomainEvent<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
    where TAggregateEvent : IAggregateEvent<TAggregate, TId>
{
    TAggregateEvent AggregateEvent { get; }
    CommandParams CommandParams { get; }
}
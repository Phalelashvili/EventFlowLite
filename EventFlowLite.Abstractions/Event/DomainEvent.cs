using EventFlowLite.Abstractions.Command;

namespace EventFlowLite.Abstractions.Event;

public class DomainEvent<TAggregate, TId, TEvent> : IDomainEvent<TAggregate, TId, TEvent>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
    where TEvent : IAggregateEvent<TAggregate, TId>
{
    // NOTE: created using activator atm. refer to DomainEventFactory
    public DomainEvent(TAggregate aggregate, TEvent aggregateEvent, CommandParams commandParams, DateTime timestamp)
    {
        Aggregate = aggregate;
        AggregateEvent = aggregateEvent;
        CommandParams = commandParams;
        Timestamp = timestamp;
    }

    public TAggregate Aggregate { get; }
    public TId AggregateId => Aggregate.Id;
    public DateTime Timestamp { get; }
    public Type AggregateEventType => AggregateEvent.GetType();
    public TEvent AggregateEvent { get; }
    public CommandParams CommandParams { get; }
}
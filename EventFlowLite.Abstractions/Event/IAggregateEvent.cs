namespace EventFlowLite.Abstractions.Event;

public interface IAggregateEvent<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    DateTime Timestamp { get; }
}
namespace EventFlowLite.Abstractions.Event;

public interface IDomainEventHandler<in TAggregate, in TId, in TEvent>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
    where TEvent : IAggregateEvent<TAggregate, TId>
{
    Task HandleAsync(IDomainEvent<TAggregate, TId, TEvent> domainEvent, CancellationToken cancellationToken);
}
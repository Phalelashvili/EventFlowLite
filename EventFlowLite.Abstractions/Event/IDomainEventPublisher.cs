namespace EventFlowLite.Abstractions.Event;

public interface IDomainEventPublisher
{
    Task PublishAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent,
        CancellationToken cancellationToken = default)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable;
}
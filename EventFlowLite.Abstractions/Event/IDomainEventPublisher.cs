namespace EventFlowLite.Abstractions.Event;

public interface IDomainEventPublisher
{
    Task<bool> PublishAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent,
        CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable;
}
using EventFlowLite.Abstractions.Event;

namespace EventFlowLite.Abstractions.Repository;

public interface IAggregateRepository<TAggregate, in TId> : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    Task<IEnumerable<DomainEventEntity>> GetDomainEventsAsync(TId id);
}
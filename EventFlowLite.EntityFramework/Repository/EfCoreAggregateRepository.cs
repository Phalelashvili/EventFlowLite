using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Repository;
using EventFlowLite.EntityFramework.Factories;
using Microsoft.EntityFrameworkCore;

namespace EventFlowLite.EntityFramework.Repository;

public class EfCoreAggregateRepository<TContext, TAggregate, TId> : EfCoreRepository<TContext, TAggregate, TId>,
    IAggregateRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TAggregate, TId>
    where TId : IComparable
    where TContext : DbContext
{
    public EfCoreAggregateRepository(TContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<DomainEventEntity>> GetDomainEventsAsync(TId id)
    {
        var aggregateName = DomainEventEntityFactory.CreateAggregateName<TAggregate>();
        return await DbContext.Set<DomainEventEntity>()
            .Where(domainEvent =>
                domainEvent.AggregateName == aggregateName &&
                domainEvent.AggregateId == id.ToString())
            .ToListAsync();
    }
}
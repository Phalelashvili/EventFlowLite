using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.EntityFramework.Command.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventFlowLite.EntityFramework.Command;

public class DomainEventCommitter<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    private readonly DbContext _dbContext;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TAggregate _aggregate;
    private readonly IAggregateCommandBase<TAggregate, TId> _command;
    private readonly ILogger _logger;

    public DomainEventCommitter(DbContext dbContext, IDomainEventPublisher domainEventPublisher,
        TAggregate aggregate, IAggregateCommandBase<TAggregate, TId> command,
        ILogger logger)
    {
        _dbContext = dbContext;
        _domainEventPublisher = domainEventPublisher;
        _aggregate = aggregate;
        _command = command;
        _logger = logger;
    }

    /// <summary>
    /// tries to publish domain event and mark it as published in database
    /// </summary>
    public async Task TryPublishDomainEventsAsync(
        IDictionary<IAggregateEvent<TAggregate, TId>, DomainEventEntity> eventToEntityMapping)
    {
        foreach (var (aggregateEvent, eventEntity) in eventToEntityMapping)
        {
            var succeeded = await TryPublishDomainEventAndMarkEntityAsync(aggregateEvent, eventEntity);

            if (succeeded == false)
                // there's nothing to update, Published state is already false in database
                continue;

            await TryUpdateDomainEventEntityAsync(eventEntity);
        }
    }

    /// <summary>
    /// publishes event and marks Domain Event as published in database.
    /// if failed, it is supposed to stay marked as Published = false, so we know something went wrong
    /// </summary>
    private async Task<bool> TryPublishDomainEventAndMarkEntityAsync(
        IAggregateEvent<TAggregate, TId> aggregateEvent, DomainEventEntity eventEntity)
    {
        try
        {
            var domainEvent = DomainEventFactory.Create(_aggregate, _command, aggregateEvent);
            await _domainEventPublisher.PublishAsync(domainEvent, CancellationToken.None);
            eventEntity.MarkAsPublished();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Failed publishing domain event {EntityId}", eventEntity.Id);
            return false;
        }
    }

    /// <summary>
    /// updates Domain Event entity in database. if failed, Published state will remain false, indicating that something
    /// went wrong, BUT, event was already published, it's the commit that failed. in this case Idempotency should fix
    /// the issue of this event being re-published
    /// </summary>
    private async Task TryUpdateDomainEventEntityAsync(DomainEventEntity eventEntity)
    {
        try
        {
            _dbContext.Set<DomainEventEntity>().Update(eventEntity);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Failed updating publish state of domain event {EntityId}", eventEntity.Id);
        }
    }
}
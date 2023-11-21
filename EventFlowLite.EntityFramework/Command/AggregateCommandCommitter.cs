using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Extensions;
using EventFlowLite.EntityFramework.Command.Extensions;
using EventFlowLite.EntityFramework.Factories;
using EventFlowLite.EntityFramework.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EventFlowLite.EntityFramework.Command;

public class AggregateCommandCommitter<TAggregate, TId>
    where TAggregate : AggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    private readonly DbContext _dbContext;
    private readonly EfCoreAggregateRepository<DbContext, TAggregate, TId> _repository;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly TAggregate _aggregate;
    private readonly IAggregateCommandBase<TAggregate, TId> _command;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public AggregateCommandCommitter(DbContext dbContext, IDomainEventPublisher domainEventPublisher,
        TAggregate aggregate, IAggregateCommandBase<TAggregate, TId> command,
        ILoggerFactory loggerFactory)
    {
        _dbContext = dbContext;
        _repository = new EfCoreAggregateRepository<DbContext, TAggregate, TId>(_dbContext);
        _domainEventPublisher = domainEventPublisher;
        _aggregate = aggregate;
        _command = command;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("AggregateCommandCommitter");
    }

    /// <summary>
    /// saves aggregate events and snapshot of aggregate. publishes domain events 
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (FlushEventsAndLog(out var aggregateEvents) is false)
            return;

        Dictionary<IAggregateEvent<TAggregate, TId>, DomainEventEntity> eventToEntityMapping;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SaveAddedEntriesIfAnyAsync(cancellationToken);
            // NOTE: we need save newly created aggregates so we can get the id. refer to ICommandBus comments
            eventToEntityMapping = CreateEventToEntityMapping(aggregateEvents);
            await SaveDomainEventEntitiesAsync(cancellationToken, eventToEntityMapping);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // in case rolling back transaction fails, don't let original exception get lost
            await TryRollbackTransactionAsync(transaction);
            throw;
        }

        // in case publisher fails -- we want to know which events were not processed
        // so they can be re-published (don't have mechanism for that atm, lol)
        await TryPublishDomainEventsAsync(eventToEntityMapping, cancellationToken);
    }

    private async Task SaveDomainEventEntitiesAsync(CancellationToken cancellationToken,
        Dictionary<IAggregateEvent<TAggregate, TId>, DomainEventEntity> eventToEntityMapping)
    {
        await _dbContext.Set<DomainEventEntity>()
            .AddRangeAsync(eventToEntityMapping.Values, cancellationToken);

        await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// pre-saves changes to assign ids to aggregates that aren't created yet.
    /// </summary>
    private async Task SaveAddedEntriesIfAnyAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.EntitiesWereAdded())
            await SaveChangesAsync(cancellationToken);
    }

    private async Task TryRollbackTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "FAILED ROLLING BACK TRANSACTION");
        }
    }

    /// <summary>
    /// maps aggregate event to correlated DomainEventEntity, so that it can be tracked and updated in database
    /// </summary>
    private Dictionary<IAggregateEvent<TAggregate, TId>, DomainEventEntity> CreateEventToEntityMapping(
        IEnumerable<IAggregateEvent<TAggregate, TId>> aggregateEvents)
    {
        DomainEventEntity ValueFactory(IAggregateEvent<TAggregate, TId> aggregateEvent)
        {
            return DomainEventEntityFactory.Create(_aggregate, _command, aggregateEvent);
        }

        var dict = aggregateEvents.ToDictionary(e => e, ValueFactory);
        return dict;
    }

    private bool FlushEventsAndLog(out IAggregateEvent<TAggregate, TId>[] aggregateEvents)
    {
        aggregateEvents = _aggregate.FlushEvents().ToArray();

        if (aggregateEvents.Length == 0)
        {
            _logger.LogWarning("command {CommandType} did not produce any aggregate events",
                _command.GetType().PrettyPrint());
            return false;
        }

        return true;
    }

    private async Task TryPublishDomainEventsAsync(
        Dictionary<IAggregateEvent<TAggregate, TId>, DomainEventEntity> eventToEntityMapping,
        CancellationToken cancellationToken)
    {
        var domainEventCommitter = new DomainEventCommitter<TAggregate, TId>(
            _dbContext,
            _domainEventPublisher,
            _aggregate,
            _command,
            _loggerFactory);

        await domainEventCommitter.TryPublishDomainEventsAsync(eventToEntityMapping, cancellationToken);
    }
    
    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // changes are saved through IRepository solely to re-use concurrency code in its SaveChangesAsync method.
        // other solution would be to override save method in DbContext, (all of them, or make new base class).
        // but i think this is cleaner.
        // or to expose db context & transactions in IRepository.
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
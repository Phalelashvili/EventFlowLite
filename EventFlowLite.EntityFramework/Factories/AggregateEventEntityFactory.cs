using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Extensions;
using Newtonsoft.Json;

namespace EventFlowLite.EntityFramework.Factories;

public static class DomainEventEntityFactory
{
    public static DomainEventEntity Create<TAggregate, TId>(IAggregateRoot<TAggregate, TId> aggregate,
        IAggregateCommandBase<TAggregate, TId> command, IAggregateEvent<TAggregate, TId> aggregateEvent)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var agrName = CreateAggregateName<TAggregate>();
        var agrId = aggregate.Id.ToString();
        var agrVersion = aggregate.AggregateVersion;

        var commandName = command.GetType().PrettyPrint();
        if (command.Params is null)
            throw new ArgumentException(nameof(command.Params));
        var commandParamsEntity = (CommandParamsEntity)command.Params;

        var eventName = aggregateEvent.GetType().PrettyPrint();
        var eventData = JsonConvert.SerializeObject(aggregateEvent);

        var timestamp = aggregateEvent.Timestamp;

        return new DomainEventEntity(agrName, agrId!, agrVersion,
            commandName, commandParamsEntity,
            eventName, eventData, timestamp);
    }

    public static IEnumerable<DomainEventEntity> CreateMany<TAggregate, TId>(IAggregateRoot<TAggregate, TId> aggregate,
        IAggregateCommandBase<TAggregate, TId> command, IEnumerable<IAggregateEvent<TAggregate, TId>> aggregateEvents)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        return aggregateEvents.Select(e => Create(aggregate, command, e));
    }

    public static string CreateAggregateName<TAggregate>()
    {
        return typeof(TAggregate).PrettyPrint();
    }
}
using System.Collections.Concurrent;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Event;

namespace EventFlowLite.EntityFramework.Command.Factories;

public static class DomainEventFactory
{
    // TODO?: https://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/
    private static readonly ConcurrentDictionary<Type, Type> DomainEventTypeCache = new();

    public static IDomainEvent<TAggregate, TId, TEvent> Create<TAggregate, TId, TEvent>(TAggregate aggregate,
        IAggregateCommandBase<TAggregate, TId> command, TEvent aggregateEvent)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
        where TEvent : IAggregateEvent<TAggregate, TId>
    {
        // TODO: this solution sucks ass
        // we create new type from aggregateEvent because TEvent : IAggregateEvent<TAggregate, TId> constraint will not work with
        // handler contract, which requires concrete event type
        var domainEventType = DomainEventTypeCache.GetOrAdd(aggregateEvent.GetType(), aggregateEventType => typeof(
            DomainEvent<,,>).MakeGenericType(typeof(TAggregate), typeof(TId), aggregateEventType));

        return (IDomainEvent<TAggregate, TId, TEvent>)Activator.CreateInstance(
            domainEventType,
            aggregate,
            aggregateEvent,
            command.Params,
            aggregateEvent.Timestamp);
    }

    public static IEnumerable<IDomainEvent<TAggregate, TId>> CreateMany<TAggregate, TId, TEvent>(TAggregate aggregate,
        IAggregateCommandBase<TAggregate, TId> command, IEnumerable<TEvent> aggregateEvents)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
        where TEvent : IAggregateEvent<TAggregate, TId>
    {
        return aggregateEvents.Select(e => Create(aggregate, command, e));
    }
}
using EventFlowLite.Abstractions.Extensions;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Utils;

namespace EventFlowLite.Abstractions;

public interface IAggregateRoot : IDatabaseEntityBase
{
}

public interface IAggregateRoot<TAggregate, TId> : IAggregateRoot, IDatabaseEntity<TId>
    where TId : IComparable
    where TAggregate : IAggregateRoot<TAggregate, TId>
{
    IEnumerable<IAggregateEvent<TAggregate, TId>> FlushEvents();

    // yes, IEntity already has ConcurrencyVersion, which is auto-incremented on saving changes,
    // but i'd have to refactor things and actually make maintenance harder in order to remove it from AR
    int AggregateVersion { get; }
}

public abstract class AggregateRoot<TAggregate, TId> : DatabaseEntity<TId>, IAggregateRoot<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TAggregate, TId> // include class constraint in interface?
    where TId : IComparable
{
    private static readonly IReadOnlyDictionary<Type, Action<TAggregate, IAggregateEvent<TAggregate, TId>>>
        ApplyMethods = ApplyAggregateEventUtil.GetApplyMethods<TAggregate, TId>();

    private readonly List<IAggregateEvent<TAggregate, TId>> _events = new();
    public override TId Id { get; protected set; }

    public int AggregateVersion { get; private set; }

    public IEnumerable<IAggregateEvent<TAggregate, TId>> FlushEvents()
    {
        var events = new List<IAggregateEvent<TAggregate, TId>>(_events);
        _events.Clear();
        return events;
    }

    protected void Emit(IAggregateEvent<TAggregate, TId> aggregateEvent)
    {
        var eventType = aggregateEvent.GetType();
        if (ApplyMethods.TryGetValue(eventType, out var applyMethod))
            applyMethod((this as TAggregate)!, aggregateEvent);
        else
            throw new NotImplementedException($"Apply method not implemented for event '{eventType.PrettyPrint()}'");

        AggregateVersion++;
        _events.Add(aggregateEvent);
    }

    public override string ToString()
    {
        return GetType().PrettyPrint();
    }
}
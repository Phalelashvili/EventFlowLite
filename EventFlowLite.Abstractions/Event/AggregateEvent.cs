using System.Text.Json.Serialization;

namespace EventFlowLite.Abstractions.Event;

public class AggregateEvent<TAggregate, TId> : IAggregateEvent<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    // use current dt when initially constructing event obj
    public AggregateEvent() : this(DateTime.UtcNow)
    {
    }

    // use existing dt (set when using empty ctor) when deserializing
    [JsonConstructor]
    public AggregateEvent(DateTime timestamp)
    {
        Timestamp = timestamp;
    }

    public DateTime Timestamp { get; }
}
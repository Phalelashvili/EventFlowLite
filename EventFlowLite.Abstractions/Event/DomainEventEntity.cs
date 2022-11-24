using System.Diagnostics;

namespace EventFlowLite.Abstractions.Event;

[DebuggerDisplay("DomainEventEntity for {AggregateName}:{AggregateId} {CommandParams.CommandId}")]
public class DomainEventEntity
{
    public DomainEventEntity()
    {
    }

    public DomainEventEntity(string aggregateName, string aggregateId,
        int aggregateVersion, string commandName, CommandParamsEntity commandParams,
        string eventName, string eventData, DateTime timestamp)
    {
        AggregateName = aggregateName;
        AggregateId = aggregateId;
        AggregateVersion = aggregateVersion;
        CommandName = commandName;
        CommandParams = commandParams ?? throw new ArgumentException($"{nameof(commandName)} is null");
        EventName = eventName;
        EventData = eventData;
        Timestamp = timestamp;
    }

    public long Id { get; protected set; }

    public string AggregateName { get; private set; }
    public string AggregateId { get; private set; }
    public int AggregateVersion { get; private set; }

    public string CommandName { get; private set; }
    public CommandParamsEntity CommandParams { get; private set; }

    public string EventName { get; private set; }
    public string EventData { get; private set; }

    public DateTime Timestamp { get; private set; }

    public bool Published { get; private set; }

    public void MarkAsPublished()
    {
        Published = true;
    }
}
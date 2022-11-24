namespace EventFlowLite.Abstractions.ServiceBus.CommandBus.Exceptions;

public class CommandAlreadyHandledException : Exception
{
    public CommandAlreadyHandledException(string aggregateName, string? aggregateId,
        string commandName, string commandId)
        : base(
            $"Command {commandName} {commandId} for aggregate {aggregateName} {aggregateId} has already been handled.")
    {
        AggregateName = aggregateName;
        AggregateId = aggregateId;
        CommandName = commandName;
        CommandId = commandId;
    }

    public string AggregateName { get; }
    public string? AggregateId { get; }
    public string CommandName { get; }
    public string CommandId { get; }
}
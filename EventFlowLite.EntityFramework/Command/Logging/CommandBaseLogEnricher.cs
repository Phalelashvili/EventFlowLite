using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using Serilog.Core;
using Serilog.Events;

namespace EventFlowLite.EntityFramework.Command.Logging;

public class CommandBaseLogEnricher<TAggregate, TId> : ILogEventEnricher
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    private readonly IAggregateCommandBase<TAggregate, TId> _command;

    public CommandBaseLogEnricher(IAggregateCommandBase<TAggregate, TId> command)
    {
        _command = command;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CommandId", _command.Params.CommandId));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CorrelationId", _command.Params.CorrelationId));
    }
}
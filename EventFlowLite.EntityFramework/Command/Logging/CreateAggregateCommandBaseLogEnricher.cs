using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using Serilog.Core;
using Serilog.Events;

namespace EventFlowLite.EntityFramework.Command.Logging;

public class ModifyAggregateCommandLogEnricher<TAggregate, TId> : CommandBaseLogEnricher<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    private readonly ICommand<TAggregate, TId> _command;

    public ModifyAggregateCommandLogEnricher(ICommand<TAggregate, TId> command) : base(command)
    {
        _command = command;
    }

    public new void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        base.Enrich(logEvent, propertyFactory);
        var aggregateId = $"{typeof(TAggregate).Name}:{_command.Id}";
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("AggregateId", aggregateId));
    }
}
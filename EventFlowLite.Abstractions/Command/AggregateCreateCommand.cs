namespace EventFlowLite.Abstractions.Command;

public class AggregateCreateCommand<TAggregate, TId> : ICreateAggregateCommand<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>, new()
    where TId : IComparable
{
    public AggregateCreateCommand(CommandParams commandParams)
    {
        Params = commandParams;
    }

    public CommandParams Params { get; }
}
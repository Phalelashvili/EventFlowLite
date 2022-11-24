namespace EventFlowLite.Abstractions.Command;

public class AggregateCommand<TAggregate, TId> : ICommand<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    public AggregateCommand(TId id, CommandParams @params)
    {
        Id = id;
        Params = @params;
    }

    public TId Id { get; }
    public CommandParams Params { get; }
}
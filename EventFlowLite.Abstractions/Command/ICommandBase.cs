namespace EventFlowLite.Abstractions.Command;

public interface IAggregateCommand
{
}

public interface IAggregateCommandBase<in TAggregate, out TId> : IAggregateCommand
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    CommandParams Params { get; }
}
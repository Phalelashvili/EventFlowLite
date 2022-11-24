namespace EventFlowLite.Abstractions.Command;

public interface ICommand<in TAggregate, out TId> : IAggregateCommandBase<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    TId Id { get; }
}
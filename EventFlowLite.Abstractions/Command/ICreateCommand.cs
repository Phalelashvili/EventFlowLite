namespace EventFlowLite.Abstractions.Command;

public interface ICreateAggregateCommand<in TAggregate, out TId> : IAggregateCommandBase<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>, new()
    where TId : IComparable
{
}
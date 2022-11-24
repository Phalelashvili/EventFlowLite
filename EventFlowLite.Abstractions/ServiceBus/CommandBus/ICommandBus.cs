using EventFlowLite.Abstractions.Command;

namespace EventFlowLite.Abstractions.ServiceBus.CommandBus;

public interface ICommandBus
{
    // separation of ICreateAggregateCommand and ICommand is required for aggregate id.
    // ICreateAggregateCommand does not have an id,
    // so we can't query the database trying to find it. (and create if it doesn't exist)
    // solution to this would be to include id in ICreateAggregateCommand as well,
    // but unless we use UUID, we'd need to retrieve sequence from database;
    // i think this would make the project even more complicated

    Task<TAggregate> PublishAsync<TAggregate, TId>(ICreateAggregateCommand<TAggregate, TId> command,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<TAggregate, TId>, new()
        where TId : IComparable;

    Task<TAggregate> PublishAsync<TAggregate, TId>(ICommand<TAggregate, TId> command,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<TAggregate, TId>
        where TId : IComparable;
}
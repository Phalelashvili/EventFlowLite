namespace EventFlowLite.Abstractions.Command;

public interface ICreateCommandHandler<in TAggregate, in TId, in TCommand>
    where TCommand : ICreateAggregateCommand<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>, new()
    where TId : IComparable
{
    Task HandleAsync(TAggregate aggregate, TCommand command, CancellationToken cancellationToken);
}
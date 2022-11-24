namespace EventFlowLite.Abstractions.Command;

public interface ICommandHandler<in TAggregate, in TId, in TCommand>
    where TCommand : ICommand<TAggregate, TId>
    where TAggregate : IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    Task HandleAsync(TAggregate aggregate, TCommand command, CancellationToken cancellationToken);
}
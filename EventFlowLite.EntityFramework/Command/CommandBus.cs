using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Extensions;
using EventFlowLite.Abstractions.ServiceBus.CommandBus;
using EventFlowLite.Abstractions.ServiceBus.CommandBus.Exceptions;
using EventFlowLite.EntityFramework.Command.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace EventFlowLite.EntityFramework.Command;

[PrimaryConstructor]
public partial class CommandBus : ICommandBus
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly ILogger<CommandBus> _logger;
    private readonly IServiceProvider _serviceProvider;

    #region Create Aggregate
    
    /// <summary>
    /// publishes command that creates the aggregate
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AggregateDoesNotExistError"></exception>
    /// <exception cref="AggregateVersionMismatchError"></exception>
    public async Task<TAggregate> PublishAsync<TAggregate, TId>(ICreateAggregateCommand<TAggregate, TId> command,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<TAggregate, TId>, new()
        where TId : IComparable
    {
        LogContext.Push(new CommandBaseLogEnricher<TAggregate, TId>(command));
        
        await EnsureIdempotencyAsync(command);
        
        var agr = new TAggregate();

        var handlerType = CommandHandlerTypeMaker.MakeCreateCommandHandlerTypeAndCache<TAggregate, TId>(command.GetType());
        await HandleAsync<TAggregate, TId>(handlerType, agr, command, cancellationToken);
        
        await _dbContext.Set<TAggregate>().AddAsync(agr, cancellationToken);

        await CommitAsync(agr, command, cancellationToken);

        return agr;
    }

    /// <summary>
    /// ensures that command is idempotent based on aggregate name, command name and command id
    /// </summary>
    /// <exception cref="CommandIdIsEmptyException"></exception>
    /// <exception cref="CommandAlreadyHandledException"></exception>
    private async Task EnsureIdempotencyAsync<TAggregate, TId>(IAggregateCommandBase<TAggregate, TId> command)
        where TAggregate : AggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (string.IsNullOrEmpty(command.Params?.CommandId))
            throw new CommandIdIsEmptyException();
        
        var aggregateName = typeof(TAggregate).PrettyPrint();
        var commandName = command.GetType().PrettyPrint();
        var commandId = command.Params.CommandId;

        var eventExistsWithCommandParams = await _dbContext.Set<DomainEventEntity>()
            .AnyAsync(e => 
                e.AggregateName == aggregateName && 
                e.CommandName == commandName && 
                e.CommandParams.CommandId == commandId);
        if (eventExistsWithCommandParams)
            throw new CommandAlreadyHandledException(aggregateName, null, commandName, commandId);
    }

    #endregion

    #region Modify Aggregate State
    
    /// <summary>
    /// publishes command that modifies state of the aggregate
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AggregateDoesNotExistError"></exception>
    /// <exception cref="AggregateVersionMismatchError"></exception>
    public async Task<TAggregate> PublishAsync<TAggregate, TId>(ICommand<TAggregate, TId> command,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        LogContext.Push(new ModifyAggregateCommandLogEnricher<TAggregate, TId>(command));

        // NOTE: aggregate must be loaded before ensuring idempotency, so that in case same command gets handled
        // concurrently, concurrency error will be thrown. TODO: figure out a way to do this with database constraints?
        var agr = await FindVersionedAggregateOrThrowAsync(command);

        await EnsureIdempotencyAsync(command);
        
        var handlerType = CommandHandlerTypeMaker.MakeCommandHandlerTypeAndCache<TAggregate, TId>(command.GetType());
        await HandleAsync<TAggregate, TId>(handlerType, agr, command, cancellationToken);

        await CommitAsync(agr, command, cancellationToken);

        return agr;
    }

    /// <summary>
    /// ensures that command is idempotent based on aggregate name, command name, aggregate id and command id
    /// </summary>
    /// <exception cref="CommandIdIsEmptyException"></exception>
    /// <exception cref="CommandAlreadyHandledException"></exception>
    private async Task EnsureIdempotencyAsync<TAggregate, TId>(ICommand<TAggregate, TId> command)
        where TAggregate : class, IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (string.IsNullOrEmpty(command.Params?.CommandId))
            throw new CommandIdIsEmptyException();

        var aggregateName = typeof(TAggregate).PrettyPrint();
        var commandName = command.GetType().PrettyPrint();
        var aggregateId = command.Id.ToString();
        var commandId = command.Params.CommandId;
        
        var eventExistsWithCommandId = await _dbContext.Set<DomainEventEntity>()
            .AnyAsync(e => 
                e.AggregateName == aggregateName &&
                e.AggregateId == aggregateId && 
                e.CommandName == commandName &&
                e.CommandParams.CommandId == commandId);
        if (eventExistsWithCommandId)
            throw new CommandAlreadyHandledException(aggregateName, aggregateId, commandName, commandId);
    }

    private async Task<TAggregate> FindVersionedAggregateOrThrowAsync<TAggregate, TId>(ICommand<TAggregate, TId> command)
        where TAggregate : class, IAggregateRoot<TAggregate, TId> where TId : IComparable
    {
        var agr = await _dbContext.Set<TAggregate>().FindAsync(command.Id);
        if (agr is null)
            throw new AggregateDoesNotExistError(typeof(TAggregate), command.Id);
        if (command.Params.ExpectedVersion.HasValue && command.Params.ExpectedVersion.Value != agr.AggregateVersion)
            throw new AggregateVersionMismatchError(command.Params.ExpectedVersion.Value, agr.AggregateVersion);
        // NOTE: if aggregate version changes while we're modifying it,
        // while saving changes -- ConcurrencyVersion check will exception 
        return agr;
    }

    #endregion

    #region Common

    /// <summary>
    /// resolves handler from DI and invokes handler for command
    /// </summary>
    /// <exception cref="CommandHandlerNotRegisteredException"></exception>
    /// <exception cref="TooManyCommandHandlersRegisteredException"></exception>
    private async Task HandleAsync<TAggregate, TId>(Type handlerType, TAggregate agr, IAggregateCommand command,
        CancellationToken cancellationToken)
        where TAggregate : class, IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var handlerInstance = GetHandlerInstanceOrThrow(handlerType, command);

        var handleMethod = CommandHandleMethodLocator.GetAndCacheHandleMethod(handlerType);

        var invoker = new CommandHandlerInvoker<TAggregate, TId>(handlerInstance, handleMethod, agr);
        await invoker.TryInvokeOrThrowInnerAsync(command, cancellationToken);
    }

    private object GetHandlerInstanceOrThrow(Type handlerType, IAggregateCommand command)
    {
        var handlers = _serviceProvider.GetServices(handlerType).ToArray();
        if (handlers.Length == 0)
            throw new CommandHandlerNotRegisteredException(command.GetType());
        if (handlers.Length > 1)
            throw new TooManyCommandHandlersRegisteredException(command.GetType());
        var handlerInstance = handlers.Single();
        return handlerInstance!;
    }

    private async Task CommitAsync<TAggregate, TId>(
        TAggregate agr, IAggregateCommandBase<TAggregate, TId> command, CancellationToken cancellationToken)
        where TAggregate : AggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var committer = new AggregateCommandCommitter<TAggregate, TId>(
            _dbContext,
            _domainEventPublisher,
            agr,
            command,
            _logger);
        await committer.CommitAsync(cancellationToken);
    }
    
    #endregion
}
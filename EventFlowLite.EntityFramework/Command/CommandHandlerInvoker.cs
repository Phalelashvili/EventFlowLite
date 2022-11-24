using System.Reflection;
using System.Runtime.ExceptionServices;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.Abstractions.Extensions;
using Throw;

namespace EventFlowLite.EntityFramework.Command;

public class CommandHandlerInvoker<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TAggregate, TId>
    where TId : IComparable
{
    private readonly object _handlerInstance;
    private readonly MethodInfo _handleMethod;
    private readonly TAggregate _aggregate;

    public CommandHandlerInvoker(object handlerInstance, MethodInfo handleMethod, TAggregate aggregate)
    {
        _handlerInstance = handlerInstance.ThrowIfNull().Value;
        _handleMethod = handleMethod.ThrowIfNull().Value;
        _aggregate = aggregate.ThrowIfNull().Value;
    }

    public async Task TryInvokeOrThrowInnerAsync(IAggregateCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await InvokeHandleMethodAsync(command, cancellationToken);
        }
        catch (TargetInvocationException e)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
        }
    }

    private async Task InvokeHandleMethodAsync(IAggregateCommand command, CancellationToken cancellationToken)
    {
        // HandleAsync(TAggregate aggregate, TCommand command, CancellationToken cancellationToken);
        var handleParams = new object[] { _aggregate, command, cancellationToken };
        var returnedObject = _handleMethod.Invoke(_handlerInstance, handleParams);
        if (returnedObject is Task task)
        {
            await task;
        }
        else
        {
            var actualReturnTypeName = returnedObject?.GetType().PrettyPrint() ?? "void";
            throw new InvalidOperationException($"handle method returned {actualReturnTypeName} instead of Task");
        }
    }
}
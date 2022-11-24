using System.Reflection;
using System.Runtime.ExceptionServices;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.EntityFramework.Query;

public class QueryHandlerInvoker
{
    private readonly object _handlerInstance;
    private readonly MethodInfo _handleMethod;

    public QueryHandlerInvoker(object handlerInstance, MethodInfo handleMethod)
    {
        _handlerInstance = handlerInstance;
        _handleMethod = handleMethod;
    }

    public async Task<TResult> TryInvokeOrThrowInnerAsync<TResult>(IQuery<TResult> query,
        CancellationToken cancellationToken)
    {
        try
        {
            return await InvokeHandleMethodAsync(query, cancellationToken);
        }
        catch (TargetInvocationException e)
        {
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw; // there is more code that can fail in Throw() method above. this should make compiler happy
        }
    }

    private async Task<TResult> InvokeHandleMethodAsync<TResult>(IQuery<TResult> query,
        CancellationToken cancellationToken)
    {
        // Handle(TQuery query, CancellationToken cancellationToken);
        var returnedObject = _handleMethod.Invoke(_handlerInstance, new object[] { query, cancellationToken });

        if (returnedObject is Task<TResult> task)
        {
            return await task;
        }
        else
        {
            var actualReturnTypeName = returnedObject?.GetType().PrettyPrint() ?? "void";
            var expectedReturnTypeName = typeof(Task<TResult>).PrettyPrint();
            throw new InvalidOperationException(
                $"handle method returned {actualReturnTypeName} instead of {expectedReturnTypeName}");
        }
    }
}
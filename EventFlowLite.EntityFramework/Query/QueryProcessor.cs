using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.ServiceBus.QueryProcessor;
using EventFlowLite.Abstractions.ServiceBus.QueryProcessor.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlowLite.EntityFramework.Query;

[PrimaryConstructor]
public partial class QueryProcessor : IQueryProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<TResult> ProcessAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var queryType = query.GetType();

        var handlerType = QueryHandlerTypeMaker.MakeGenericAndCache<TResult>(queryType);
        var handlerInstance = GetHandlerInstanceOrThrow(handlerType, queryType);

        var handleMethod = QueryHandleMethodLocator.GetAndCacheHandleMethod(handlerType);

        var invoker = new QueryHandlerInvoker(handlerInstance, handleMethod);
        var result = await invoker.TryInvokeOrThrowInnerAsync(query, cancellationToken);

        return result;
    }

    private object GetHandlerInstanceOrThrow(Type handlerType, Type queryType)
    {
        var handlers = _serviceProvider.GetServices(handlerType).ToArray();
        if (handlers.Length == 0)
            throw new QueryHandlerNotRegisteredException(queryType);
        if (handlers.Length > 1)
            throw new TooManyQueryHandlersRegisteredException(queryType);
        return handlers.Single()!;
    }
}
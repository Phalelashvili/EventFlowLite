using System.Collections.Concurrent;
using System.Reflection;
using EventFlowLite.Abstractions;
using EventFlowLite.EntityFramework.Extensions;

namespace EventFlowLite.EntityFramework.Query;

public static class QueryHandleMethodLocator
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> HandlerMethodInfoCache = new();
    // TODO?: https://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/
    // private static readonly ConcurrentDictionary<Type, Delegate> HandleMethodDelegateCache = new();

    public static MethodInfo GetAndCacheHandleMethod(Type handlerType)
    {
        return HandlerMethodInfoCache.GetOrAdd(handlerType, GetHandleMethod);
    }

    private static MethodInfo GetHandleMethod(Type handlerType)
    {
        if (IsTypeQueryHandler(handlerType) == false)
            throw new ArgumentOutOfRangeException(nameof(handlerType));

        return GetSingleHandleMethodOrThrow(handlerType);
    }

    private static MethodInfo GetSingleHandleMethodOrThrow(IReflect handlerType)
    {
        var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        // can't see a reason why IQueryHandler would have more than 1 method,
        // this looks ok now. let it crash if there's a need to
        var handleMethod = methods.SingleOrDefault();
        if (handleMethod is null)
            throw new Exception("could not find handle method.");

        return handleMethod;
    }

    private static bool IsTypeQueryHandler(Type handlerType)
    {
        return handlerType.IsAssignableToGenericInterface(typeof(IQueryHandler<,>));
    }

    // private static Func<IQueryHandler<IQuery<TResult>, TResult>, IQuery<TResult>, Task<TResult>> 
    //     GetHandleDelegate<TResult>(IQuery<TResult> query, Type handlerType)
    // {
    //     Delegate Factory(Type key)
    //     {
    //         if (handlerType.IsAssignableToGenericInterface(typeof(IQueryHandler<,>)) == false)
    //             throw new ArgumentOutOfRangeException(nameof(handlerType));
    //
    //         var methods = key.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    //         // can't see a reason why IQueryHandler would have more than 1 method,
    //         // this looks ok now. let it crash if there's a need to
    //         var handleMethodInfo = methods.SingleOrDefault();
    //         if (handleMethodInfo is null)
    //             throw new Exception("could not find handle method.");
    //
    //         return Delegate.CreateDelegate(
    //                 typeof(Func<IQueryHandler<IQuery<TResult>, TResult>, IQuery<TResult>, Task<TResult>>),
    //                 handleMethodInfo);
    //     }
    //
    //     var boxedDelegate = HandleMethodDelegateCache.GetOrAdd(handlerType, Factory);
    //     return (Func<IQueryHandler<IQuery<TResult>, TResult>, IQuery<TResult>, Task<TResult>>) boxedDelegate;
    // }
}
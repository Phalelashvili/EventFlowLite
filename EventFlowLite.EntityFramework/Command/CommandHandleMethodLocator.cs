using System.Collections.Concurrent;
using System.Reflection;
using EventFlowLite.Abstractions.Command;
using EventFlowLite.EntityFramework.Extensions;

namespace EventFlowLite.EntityFramework.Command;

public static class CommandHandleMethodLocator
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
        if (IsTypeCommandHandler(handlerType) == false)
            throw new ArgumentOutOfRangeException(nameof(handlerType));

        return GetSingleHandleMethodOrThrow(handlerType);
    }

    private static MethodInfo GetSingleHandleMethodOrThrow(IReflect handlerType)
    {
        var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        // can't see a reason why ICommandHandler would have more than 1 method,
        // this looks ok now. let it crash if there's a need to
        var handleMethod = methods.SingleOrDefault();
        if (handleMethod is null)
            throw new Exception("could not find handle method.");
        return handleMethod;
    }

    private static bool IsTypeCommandHandler(Type handlerType)
    {
        return handlerType.IsAssignableToGenericInterface(typeof(ICreateCommandHandler<,,>)) ||
               handlerType.IsAssignableToGenericInterface(typeof(ICommandHandler<,,>));
    }
}
using System.Collections.Concurrent;
using EventFlowLite.Abstractions.Command;

namespace EventFlowLite.EntityFramework.Command;

public static class CommandHandlerTypeMaker
{
    private static readonly ConcurrentDictionary<Type, Type> Cache = new();

    public static Type MakeCreateCommandHandlerTypeAndCache<TAggregate, TId>(Type commandType)
    {
        return Cache.GetOrAdd(commandType, MakeGenericCreateCommandHandlerType<TAggregate, TId>);
    }

    public static Type MakeCommandHandlerTypeAndCache<TAggregate, TId>(Type commandType)
    {
        return Cache.GetOrAdd(commandType, MakeGenericCommandHandlerType<TAggregate, TId>);
    }

    private static Type MakeGenericCommandHandlerType<TAggregate, TId>(Type commandType)
    {
        return typeof(ICommandHandler<,,>).MakeGenericType(typeof(TAggregate), typeof(TId), commandType);
    }

    private static Type MakeGenericCreateCommandHandlerType<TAggregate, TId>(Type commandType)
    {
        return typeof(ICreateCommandHandler<,,>).MakeGenericType(typeof(TAggregate), typeof(TId), commandType);
    }
}
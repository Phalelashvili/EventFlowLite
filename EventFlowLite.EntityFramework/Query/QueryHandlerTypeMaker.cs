using System.Collections.Concurrent;
using EventFlowLite.Abstractions;

namespace EventFlowLite.EntityFramework.Query;

public static class QueryHandlerTypeMaker
{
    private static readonly ConcurrentDictionary<Type, Type> Cache = new();

    public static Type MakeGenericAndCache<TResult>(Type queryType)
    {
        return Cache.GetOrAdd(queryType, MakeGenericHandlerType<TResult>);
    }

    private static Type MakeGenericHandlerType<TResult>(Type key)
    {
        return typeof(IQueryHandler<,>).MakeGenericType(key, typeof(TResult));
    }
}
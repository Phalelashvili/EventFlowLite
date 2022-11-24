﻿namespace EventFlowLite.Abstractions;

public interface IQuery
{
}

public interface IQuery<out T> : IQuery
{
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult?> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
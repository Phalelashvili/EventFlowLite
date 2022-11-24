namespace EventFlowLite.Abstractions.ServiceBus.QueryProcessor;

public interface IQueryProcessor
{
    Task<TResult> ProcessAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}
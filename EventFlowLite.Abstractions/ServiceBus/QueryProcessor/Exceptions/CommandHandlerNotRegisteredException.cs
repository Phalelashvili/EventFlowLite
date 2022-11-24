using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.Abstractions.ServiceBus.QueryProcessor.Exceptions;

public class QueryHandlerNotRegisteredException : Exception
{
    public QueryHandlerNotRegisteredException(Type queryType)
        : base($"handler for query '{queryType.PrettyPrint()}' was not registered")
    {
    }
}
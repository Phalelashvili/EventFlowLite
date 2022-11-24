using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.Abstractions.ServiceBus.QueryProcessor.Exceptions;

public class TooManyQueryHandlersRegisteredException : Exception
{
    public TooManyQueryHandlersRegisteredException(Type commandType)
        : base($"Too many handler are registered for query '{commandType.PrettyPrint()}'")
    {
    }
}
using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.Abstractions.ServiceBus.CommandBus.Exceptions;

public class CommandHandlerNotRegisteredException : Exception
{
    public CommandHandlerNotRegisteredException(Type commandType)
        : base($"handler for command '{commandType.PrettyPrint()}' was not registered")
    {
    }
}
using EventFlowLite.Abstractions.Extensions;

namespace EventFlowLite.Abstractions.ServiceBus.CommandBus.Exceptions;

public class TooManyCommandHandlersRegisteredException : Exception
{
    public TooManyCommandHandlersRegisteredException(Type commandType)
        : base($"Too many handler are registered for command '{commandType.PrettyPrint()}'")
    {
    }
}
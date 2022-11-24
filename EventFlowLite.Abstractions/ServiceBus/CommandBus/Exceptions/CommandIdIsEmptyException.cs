namespace EventFlowLite.Abstractions.ServiceBus.CommandBus.Exceptions;

public class CommandIdIsEmptyException : Exception
{
    public CommandIdIsEmptyException() : base("command id is empty")
    {
    }
}
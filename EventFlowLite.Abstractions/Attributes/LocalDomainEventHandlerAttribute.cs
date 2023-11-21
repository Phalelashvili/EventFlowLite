namespace EventFlowLite.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LocalDomainEventHandlerAttribute : Attribute
{
    public TimeSpan Timeout { get; }
    
    public LocalDomainEventHandlerAttribute(TimeSpan timeout)
    {
        Timeout = timeout;
    }
}
using System.Reflection;

namespace EventFlowLite.EntityFramework.DomainEventPublisher;

public class DomainEventHandlerDescriptor
{
    public DomainEventHandlerDescriptor(Type handlerType, MethodInfo handleMethodInfo)
    {
        HandlerType = handlerType;
        HandleMethodInfo = handleMethodInfo;
    }

    public Type HandlerType { get; }
    public MethodInfo HandleMethodInfo { get; }
}
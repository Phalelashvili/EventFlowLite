using System.Reflection;

namespace EventFlowLite.EntityFramework.DomainEventPublisher;

public class DomainEventHandlerDescriptor
{
    public DomainEventHandlerDescriptor(Type handlerType, MethodInfo handleMethodInfo, bool isStrictlyBackgroundHandler,
        TimeSpan? localHandlingTimeout)
    {
        HandlerType = handlerType;
        HandleMethodInfo = handleMethodInfo;
        IsStrictlyBackgroundHandler = isStrictlyBackgroundHandler;
        LocalHandlingTimeout = localHandlingTimeout ?? TimeSpan.FromSeconds(15);
    }

    public Type HandlerType { get; }
    public MethodInfo HandleMethodInfo { get; }
    
    /// <summary>
    /// determines whether job should strictly handled in hangfire or can be tried to be handled locally
    /// </summary>
    public bool IsStrictlyBackgroundHandler { get; }
    
    /// <summary>
    /// used to cancel handling event locally if it takes too long
    /// </summary>
    public TimeSpan LocalHandlingTimeout { get; }
}
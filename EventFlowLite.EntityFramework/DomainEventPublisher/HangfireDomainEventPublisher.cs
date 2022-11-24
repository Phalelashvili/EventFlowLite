using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Extensions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlowLite.EntityFramework.DomainEventPublisher;

// TODO: could use some cleanup
[PrimaryConstructor]
public partial class HangfireDomainEventPublisher : IDomainEventPublisher
{
    private static readonly ConcurrentDictionary<Type, DomainEventHandlerDescriptor[]> HandlerDescriptorCache = new();

    // TODO?: https://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/
    private static readonly ConcurrentDictionary<Type, MethodInfo> EnqueueMethodCache = new();

    private static readonly string HandleMethodName =
        typeof(IDomainEventHandler<,,>).GetMethods().SingleOrDefault()?.Name ??
        throw new Exception($"could not determine handler method of {typeof(IDomainEventHandler<,,>).PrettyPrint()}");

    private readonly IEnumerable<ServiceDescriptor> _serviceCollection;

    public Task PublishAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent, CancellationToken _ = default)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        var handlerDescriptors = HandlerDescriptorCache.GetOrAdd(domainEvent.AggregateEventType,
            CreateHandlerDescriptor<TAggregate, TId>);
        foreach (var descriptor in handlerDescriptors)
        {
            // this is horrible. TODO: find a better way?
            var expression = CreateHandlerExpression(descriptor, domainEvent);

            var genericEnqueueMethod = EnqueueMethodCache.GetOrAdd(descriptor.HandlerType, MakeGenericEnqueueMethod);
            try
            {
                genericEnqueueMethod.Invoke(null, new object?[] { expression });
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }

        return Task.CompletedTask;
    }

    private static MethodInfo MakeGenericEnqueueMethod(Type type)
    {
        return
            typeof(BackgroundJob)
                .GetMethods()
                .Last(m => m.Name == nameof(BackgroundJob.Enqueue) && m.IsGenericMethod)
                .MakeGenericMethod(type);
    }

    private static LambdaExpression CreateHandlerExpression<TAggregate, TId>(DomainEventHandlerDescriptor descriptor,
        IDomainEvent<TAggregate, TId> domainEvent)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var handlerParamExpr = Expression.Parameter(descriptor.HandlerType, "handler");

        var callArgExprs = new Expression[]
        {
            Expression.Constant(domainEvent),
            Expression.Constant(CancellationToken.None)
        };
        var callExpr = Expression.Call(handlerParamExpr, descriptor.HandleMethodInfo, callArgExprs);

        var lambda = Expression.Lambda(callExpr, handlerParamExpr);
        return lambda;
    }

    private DomainEventHandlerDescriptor[] CreateHandlerDescriptor<TAggregate, TId>(Type aggregateEventType)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var domainEventType =
            typeof(IDomainEvent<,,>).MakeGenericType(typeof(TAggregate), typeof(TId), aggregateEventType);
        var handlerInterfaceType =
            typeof(IDomainEventHandler<,,>).MakeGenericType(typeof(TAggregate), typeof(TId), aggregateEventType);

        var handlerServiceDescriptors = _serviceCollection
            .Where(serviceDescriptor => serviceDescriptor.ServiceType == handlerInterfaceType).ToArray();

        var descriptors = new List<DomainEventHandlerDescriptor>(handlerServiceDescriptors.Length);
        foreach (var handlerServiceDescriptor in handlerServiceDescriptors)
        {
            var handlerType = handlerServiceDescriptor.ImplementationType;
            if (handlerType is null)
                throw new Exception($"'{handlerServiceDescriptor.ServiceType}' is implemented by null value");
            
            var methodParams = new[] { domainEventType, typeof(CancellationToken) };
            var handleMethodInfo = handlerType.GetMethod(HandleMethodName, methodParams)
                                   ?? throw new Exception($"could not get method {handlerType}.{HandleMethodName}");

            var descriptor = new DomainEventHandlerDescriptor(handlerType, handleMethodInfo);
            descriptors.Add(descriptor);
        }

        return descriptors.ToArray();
    }
}
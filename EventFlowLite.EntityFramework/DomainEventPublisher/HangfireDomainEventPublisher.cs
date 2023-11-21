using System.Collections.Concurrent;
using System.Reflection;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Attributes;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.Abstractions.Extensions;
using EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace EventFlowLite.EntityFramework.DomainEventPublisher;

internal sealed partial class HangfireDomainEventPublisher : IDomainEventPublisher
{
    private static readonly ConcurrentDictionary<Type, DomainEventHandlerDescriptor[]> HandlerDescriptorCache = new();
    
    private static readonly string HandleMethodName =
        typeof(IDomainEventHandler<,,>).GetMethods().SingleOrDefault()?.Name ??
        throw new Exception($"could not determine handler method of {typeof(IDomainEventHandler<,,>).PrettyPrint()}");

    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ServiceDescriptor> _serviceCollection;
    private readonly ILogger<HangfireDomainEventPublisher> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _config;
    private readonly IFeatureFlags _featureFlags;
    private readonly DomainEventPublisherContext _publisherContext;

    private readonly CircuitBreakerPolicy _hangfireCircuit;

    public HangfireDomainEventPublisher(IServiceProvider serviceProvider,
        IEnumerable<ServiceDescriptor> serviceCollection, ILogger<HangfireDomainEventPublisher> logger,
        IConfiguration config, IFeatureFlags featureFlags, DomainEventPublisherContext publisherContext,
        HangfireCircuitProvider hangfireCircuitProvider, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _serviceCollection = serviceCollection;
        _logger = logger;
        _config = config;
        _featureFlags = featureFlags;
        _publisherContext = publisherContext;
        _loggerFactory = loggerFactory;

        _hangfireCircuit = hangfireCircuitProvider.GetCircuitBreaker();
    }

    public async Task<bool> PublishAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent,
        CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        _publisherContext.IncrementNestLevel();
        var maxNestLevel = _config.GetValue<int?>("Framework:DomainEventPublisher:MaxNestLevel") ?? 4;

        var handlerDescriptors = HandlerDescriptorCache.GetOrAdd(domainEvent.AggregateEventType,
            CreateHandlerDescriptor<TAggregate, TId>);

        var publishedAll = true;

        var executor = new RecipeExecutor(_serviceProvider, _hangfireCircuit, _loggerFactory);

        foreach (var descriptor in handlerDescriptors)
        {
            ExecutionRecipe? recipe;
            if (_hangfireCircuit.CircuitState is CircuitState.Open)
            {
                if (_featureFlags.LocalDomainEventHandlingEnabled is false)
                {
                    _logger.LogCritical(
                        "can not process domain event. Hangfire circuit is open and local domain event handling is disabled.");
                    return false;
                }

                // if circuit is open, we don't really have a choice but to process everything locally.
                // even handlers that are strictly background handlers. maybe introduce some level of severity?
                // so that long running tasks that are not breaking core domain can be skipped over
                recipe = ExecutionRecipeFactory
                    .Create(DomainEventPublishingStrategy.Local);
            }
            else // Isolate() is not called manually, so any other state than Open suits this block
            {
                if (descriptor.IsStrictlyBackgroundHandler || _featureFlags.LocalDomainEventHandlingEnabled is false ||
                    _publisherContext.NestLevel >= maxNestLevel)
                {
                    recipe = ExecutionRecipeFactory
                        .Create(DomainEventPublishingStrategy.RedisHangfire);

                    // atm local domain event handling is kinda experimental, so keep it safe with feature flag
                    if (_featureFlags.LocalDomainEventHandlingEnabled)
                        recipe
                            .FallbackTo(ExecutionRecipeFactory
                                .Create(DomainEventPublishingStrategy.Local));
                }
                else
                {
                    // try handling locally WITH a timeout. if it fails (or times out), fall back enqueuing to hangfire,
                    // if hangfire also fails, fall back to local but WITHOUT a timeout
                    // NOTE: event will be processed locally twice if hangfire fails, but that should be fine,
                    // if idempotency is implemented correctly
                    recipe = ExecutionRecipeFactory
                        .Create(DomainEventPublishingStrategy.Local)
                        .WithTimeout(descriptor.LocalHandlingTimeout)
                        .FallbackTo(ExecutionRecipeFactory
                            .Create(DomainEventPublishingStrategy.RedisHangfire)
                            .FallbackTo(ExecutionRecipeFactory
                                .Create(DomainEventPublishingStrategy.Local)));
                }
            }

            if (recipe is null)
                throw new NullReferenceException("execution recipe can not be null");
            
            var published = await executor.ExecuteAsync(recipe, domainEvent, descriptor, cancellationToken);
            if (published is false)
                publishedAll = false;
        }

        return publishedAll;
    }
    
    private DomainEventHandlerDescriptor[] CreateHandlerDescriptor<TAggregate, TId>(Type aggregateEventType)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var defaultLocalHandlingTimeout =
            _config.GetValue<TimeSpan?>("Framework:DomainEventPublisher:DefaultLocalHandlingTimeout") ??
            TimeSpan.FromSeconds(15);

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

            var backgroundAttr = handleMethodInfo.GetCustomAttribute<BackgroundDomainEventHandlerAttribute>();
            var localAttr = handleMethodInfo.GetCustomAttribute<LocalDomainEventHandlerAttribute>();

            var isStrictlyBackgroundHandler = backgroundAttr is not null;
            var localHandlingTimeout = localAttr?.Timeout ?? defaultLocalHandlingTimeout;

            var descriptor = new DomainEventHandlerDescriptor(handlerType, handleMethodInfo,
                isStrictlyBackgroundHandler, localHandlingTimeout);
            descriptors.Add(descriptor);
        }

        return descriptors.ToArray();
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Event;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;

internal sealed class RecipeExecutor
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> EnqueueMethodCache = new();

    private readonly List<ExecutionRecipe> _recipes = new();

    private readonly IServiceProvider _serviceProvider;
    // TODO?: https://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/
    private readonly CircuitBreakerPolicy _hangfireCircuit;
    private readonly ILogger _logger;

    public RecipeExecutor(IServiceProvider serviceProvider, CircuitBreakerPolicy hangfireCircuit,
        ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _hangfireCircuit = hangfireCircuit;
        _logger = loggerFactory.CreateLogger<RecipeExecutor>();
    }

    public async Task<bool> ExecuteAsync<TAggregate, TId>(ExecutionRecipe recipe,
        IDomainEvent<TAggregate, TId> domainEvent,
        DomainEventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        var published = await ExecuteRecipeWithoutFallbackAsync(recipe, domainEvent, descriptor, cancellationToken);
        if (published)
            return true;
        if (recipe.FallbackRecipe is null)
            return false;
        return await ExecuteAsync(recipe.FallbackRecipe, domainEvent, descriptor, cancellationToken);
    }
    
    private async Task<bool> ExecuteRecipeWithoutFallbackAsync<TAggregate, TId>(ExecutionRecipe recipe,
        IDomainEvent<TAggregate, TId> domainEvent, DomainEventHandlerDescriptor descriptor,
        CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (recipe.Strategy is DomainEventPublishingStrategy.Local)
        {
            var published = await ExecuteLocalRecipeAsync(recipe, domainEvent, descriptor, cancellationToken);
            return published;
        }

        if (recipe.Strategy is DomainEventPublishingStrategy.RedisHangfire)
        {
            var enqueued = TryEnqueueInHangfire(domainEvent, descriptor);
            return enqueued;
        }

        return false;
    }

    private async Task<bool> ExecuteLocalRecipeAsync<TAggregate, TId>(ExecutionRecipe recipe,
        IDomainEvent<TAggregate, TId> domainEvent, DomainEventHandlerDescriptor descriptor,
        CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        CancellationTokenSource linkedTimeoutCts = null;

        if (recipe.Timeout.HasValue && Debugger.IsAttached is false)
        {
            var timeoutCts = new CancellationTokenSource();
            linkedTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            linkedTimeoutCts.CancelAfter(recipe.Timeout.Value);
        }

        return await TryHandlingLocallyAsync(domainEvent, descriptor, linkedTimeoutCts?.Token ?? cancellationToken);
    }
    
    private async Task<bool> TryHandlingLocallyAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent,
        DomainEventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId> where TId : IComparable
    {
        try
        {
            await HandleLocallyAsync(domainEvent, descriptor, cancellationToken);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "could not handle domain event locally");
            return false;
        }
    }

    private async Task HandleLocallyAsync<TAggregate, TId>(IDomainEvent<TAggregate, TId> domainEvent,
        DomainEventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        where TAggregate : IAggregateRoot<TAggregate, TId> where TId : IComparable
    {
        var invocationParams = new object?[] { domainEvent, cancellationToken };
        var handlerInstance = _serviceProvider.GetRequiredService(descriptor.HandlerType);

        var task = (Task)descriptor.HandleMethodInfo.Invoke(handlerInstance, invocationParams);
        await task;
    }

    private bool TryEnqueueInHangfire<TAggregate, TId>(
        IDomainEvent<TAggregate, TId> domainEvent, DomainEventHandlerDescriptor descriptor)
        where TAggregate : IAggregateRoot<TAggregate, TId>
        where TId : IComparable
    {
        if (_hangfireCircuit.CircuitState is CircuitState.Open) // no need to check for Isolated. not done manually
            return false;

        // this is horrible. TODO: find a better way?
        var expression = CreateHandlerExpression(descriptor, domainEvent);

        var genericEnqueueMethod = EnqueueMethodCache.GetOrAdd(descriptor.HandlerType, MakeGenericEnqueueMethod);
        try
        {
            _hangfireCircuit.Execute(() => genericEnqueueMethod.Invoke(null, new object?[] { expression }));
            return true;
        }
        catch (BrokenCircuitException)
        {
            return false;
        }
        catch (TargetInvocationException e)
        {
            // throwing instead of returning false, because this is a critical error, someone messed up reflection
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            throw; // unreachable. just to make compiler happy
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not enqueue domain event in hangfire");
            return false;
        }
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
}
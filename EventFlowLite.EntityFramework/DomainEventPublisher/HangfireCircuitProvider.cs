using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace EventFlowLite.EntityFramework.DomainEventPublisher;

public class HangfireCircuitProvider
{
    private readonly CircuitBreakerPolicy _circuit;
    private readonly ILogger _logger;

    public HangfireCircuitProvider(ILogger<HangfireCircuitProvider> logger)
    {
        _logger = logger;
        
        _circuit = Policy
            .Handle<Exception>()
            .CircuitBreaker(2, TimeSpan.FromMinutes(1),
                (e, _) => _logger.LogError(e, "Hangfire circuit is open."),
                () => _logger.LogError("Hangfire circuit is closed."));
    }

    public CircuitBreakerPolicy GetCircuitBreaker()
    {
        return _circuit;
    }
}
using System.Reflection;

namespace Acontplus.Infrastructure.HealthChecks;

/// <summary>
///     Health check for circuit breaker service.
/// </summary>
/// <param name="circuitBreakerService">The circuit breaker service to evaluate.</param>
public class CircuitBreakerHealthCheck(ICircuitBreakerService circuitBreakerService) : IHealthCheck
{
    private readonly ICircuitBreakerService _circuitBreakerService = circuitBreakerService;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            var defaultState = _circuitBreakerService.GetCircuitBreakerState();
            var apiState = _circuitBreakerService.GetCircuitBreakerState("api");
            var databaseState = _circuitBreakerService.GetCircuitBreakerState("database");
            var externalState = _circuitBreakerService.GetCircuitBreakerState("external");
            var authState = _circuitBreakerService.GetCircuitBreakerState("auth");

            var data = new Dictionary<string, object>
            {
                [HealthCheckMetadataKeys.DefaultCircuit] = defaultState.ToString(),
                [HealthCheckMetadataKeys.ApiCircuit] = apiState.ToString(),
                [HealthCheckMetadataKeys.DatabaseCircuit] = databaseState.ToString(),
                [HealthCheckMetadataKeys.ExternalCircuit] = externalState.ToString(),
                [HealthCheckMetadataKeys.AuthCircuit] = authState.ToString(),
                [HealthCheckMetadataKeys.LastCheckTime] = DateTime.UtcNow,
                ["application"] = appName
            };

            // Check if any critical circuits are open
            var criticalCircuitsOpen =
                new[] { databaseState, authState }.Any(state => state == CircuitBreakerState.Open);
            var anyCircuitOpen = new[] { defaultState, apiState, databaseState, externalState, authState }
                .Any(state => state == CircuitBreakerState.Open);

            if (criticalCircuitsOpen)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{appName} - Critical circuit breakers are open", data: data));
            }

            if (anyCircuitOpen)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"{appName} - Some circuit breakers are open", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"{appName} - All circuit breakers are operational", data));
        }
        catch (Exception ex)
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            return Task.FromResult(HealthCheckResult.Unhealthy($"{appName} - Circuit breaker service failed", ex));
        }
    }
}

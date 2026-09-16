using System.Reflection;

namespace Acontplus.Infrastructure.HealthChecks;

/// <summary>
///     Health check for cache service.
/// </summary>
/// <param name="cacheService">The cache service to evaluate.</param>
public class CacheHealthCheck(ICacheService cacheService) : IHealthCheck
{
    private readonly ICacheService _cacheService = cacheService;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache functionality with a comprehensive test
            var testKey = $"health-check-{Guid.NewGuid():N}";
            var testValue = $"test-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}";
            var testExpiration = TimeSpan.FromSeconds(30);

            // Test Set operation
            await _cacheService.SetAsync(testKey, testValue, testExpiration, cancellationToken);

            // Test Get operation
            var retrieved = await _cacheService.GetAsync<string>(testKey, cancellationToken);

            // Test Remove operation
            await _cacheService.RemoveAsync(testKey, cancellationToken);

            // Verify removal
            var removedValue = await _cacheService.GetAsync<string>(testKey, cancellationToken);

            if (retrieved == testValue && removedValue == null)
            {
                var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
                var data = new Dictionary<string, object>
                {
                    ["testKey"] = testKey,
                    ["retrievedCorrectly"] = true,
                    ["removedCorrectly"] = true,
                    ["lastTestTime"] = DateTime.UtcNow,
                    ["application"] = appName
                };
                return HealthCheckResult.Healthy($"{appName} - Cache service is fully operational", data);
            }

            var appNameDegraded = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            return HealthCheckResult.Degraded(
                $"{appNameDegraded} - Cache service test partially failed - some operations may not be working correctly");
        }
        catch (Exception ex)
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
            return HealthCheckResult.Unhealthy($"{appName} - Cache service failed", ex);
        }
    }
}

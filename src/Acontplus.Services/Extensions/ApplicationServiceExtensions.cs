using Acontplus.Services.Policies;

namespace Acontplus.Services.Extensions;

/// <summary>
/// Extension methods for registering complete application stack.
/// For infrastructure services (caching, resilience), use Acontplus.Infrastructure.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all application services (context, security, device detection) WITHOUT infrastructure.
    /// For caching and resilience, use services.AddInfrastructureServices() from Acontplus.Infrastructure.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register core services
        services.AddHttpContextAccessor();

        // Register service implementations
        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<ISecurityHeaderService, SecurityHeaderService>();
        services.AddScoped<IDeviceDetectionService, DeviceDetectionService>();

        // Register action filters
        services.AddScoped<ValidationActionFilter>();
        services.AddScoped<RequestLoggingActionFilter>();
        services.AddScoped<SecurityHeaderActionFilter>();

        // Configure request context
        services.Configure<RequestContextConfiguration>(
            configuration.GetSection("RequestContext"));

        return services;
    }

    /// <summary>
    /// Registers authorization policies for multi-tenant and device-aware scenarios.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services,
        List<string>? allowedClientIds = null)
    {
        // Register authorization handlers
        services.AddClientIdAuthorization(allowedClientIds);
        services.AddTenantIsolationAuthorization();
        services.AddDeviceTypeAuthorization();

        // Configure authorization policies
        services.AddAuthorization(options =>
        {
            options.AddClientIdPolicies(allowedClientIds);
            options.AddTenantIsolationPolicies();
            options.AddDeviceTypePolicies();
        });

        return services;
    }

    /// <summary>
    /// Configures application middleware pipeline WITHOUT infrastructure middleware.
    /// For rate limiting, use app.UseInfrastructureMiddleware() from Acontplus.Infrastructure.
    /// </summary>
    public static IApplicationBuilder UseApplicationMiddleware(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        // Security headers (early in pipeline)
        app.UseSecurityHeaders(environment);

        // CSP nonce generation
        app.UseMiddleware<CspNonceMiddleware>();

        // Request context and tracking
        app.UseMiddleware<RequestContextMiddleware>();

        // Global exception handling (late in pipeline, before MVC)
        app.UseAcontplusExceptionHandling(options =>
        {
            options.IncludeRequestDetails = true;
            options.LogRequestBody = environment.IsDevelopment();
            options.IncludeDebugDetailsInResponse = environment.IsDevelopment();
        });

        return app;
    }

    /// <summary>
    /// Configures MVC with application filters and JSON serialization options.
    /// </summary>
    public static IMvcBuilder AddApplicationMvc(
        this IServiceCollection services,
        bool enableGlobalFilters = true)
    {
        var mvcBuilder = services.AddControllers(options =>
        {
            if (enableGlobalFilters)
            {
                // Add global filters in order of execution
                options.Filters.Add<SecurityHeaderActionFilter>();
                options.Filters.Add<RequestLoggingActionFilter>();
                options.Filters.Add<ValidationActionFilter>();
            }
        });

        // Configure JSON options
        JsonConfigurationService.ConfigureAspNetCore(services);

        return mvcBuilder;
    }

    /// <summary>
    /// Adds health checks for application services only.
    /// For infrastructure health checks, use services.AddInfrastructureHealthChecks().
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add custom health checks for application services
        services.AddHealthChecks()
            .AddCheck<RequestContextHealthCheck>("request-context")
            .AddCheck<SecurityHeaderHealthCheck>("security-headers")
            .AddCheck<DeviceDetectionHealthCheck>("device-detection");

        return services;
    }

    /// <summary>
    /// Configures API explorer for documentation tools.
    /// </summary>
    public static IServiceCollection AddApiExplorer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        return services;
    }
}

// Health check implementations for application services

/// <summary>
/// Health check that verifies the availability and operation of the request context service.
/// </summary>
/// <param name="requestContextService">The request context service.</param>
public class RequestContextHealthCheck(IRequestContextService requestContextService) : IHealthCheck
{
    private readonly IRequestContextService _requestContextService = requestContextService;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test core functionality of request context service
            var contextData = _requestContextService.GetRequestContext();

            // Verify essential context data is available
            var hasRequestId = !string.IsNullOrEmpty(contextData.GetValueOrDefault("requestId")?.ToString());
            var hasCorrelationId = !string.IsNullOrEmpty(contextData.GetValueOrDefault("correlationId")?.ToString());
            var hasTimestamp = contextData.ContainsKey("timestamp");

            var healthData = contextData.Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!);

            healthData["hasRequestId"] = hasRequestId;
            healthData["hasCorrelationId"] = hasCorrelationId;
            healthData["hasTimestamp"] = hasTimestamp;
            healthData["lastCheckTime"] = DateTime.UtcNow;

            return hasRequestId && hasCorrelationId && hasTimestamp
                ? Task.FromResult(HealthCheckResult.Healthy("Request context service is fully operational", healthData))
                : Task.FromResult(HealthCheckResult.Degraded("Request context service is partially operational", data: healthData));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("HTTP context is not available"))
        {
            // This is expected when health check runs outside of HTTP context
            var data = new Dictionary<string, object>
            {
                ["status"] = "No HTTP context available (expected during startup)",
                ["lastCheckTime"] = DateTime.UtcNow
            };
            return Task.FromResult(HealthCheckResult.Healthy("Request context service is available", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Request context service failed", ex));
        }
    }
}

/// <summary>
/// Health check that verifies the availability of the security header service.
/// </summary>
/// <param name="securityHeaderService">The security header service.</param>
public class SecurityHeaderHealthCheck(ISecurityHeaderService securityHeaderService) : IHealthCheck
{
    private readonly ISecurityHeaderService _securityHeaderService = securityHeaderService;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var headers = _securityHeaderService.GetRecommendedHeaders(false);
            return Task.FromResult(HealthCheckResult.Healthy("Security header service is operational",
                new Dictionary<string, object> { ["headerCount"] = headers.Count }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Security header service failed", ex));
        }
    }
}

/// <summary>
/// Health check that verifies the functionality and accuracy of the device detection service.
/// </summary>
/// <param name="deviceDetectionService">The device detection service.</param>
public class DeviceDetectionHealthCheck(IDeviceDetectionService deviceDetectionService) : IHealthCheck
{
    private readonly IDeviceDetectionService _deviceDetectionService = deviceDetectionService;

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test with multiple user agents to verify detection accuracy
            var testCases = new[]
            {
                ("Desktop Chrome", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", DeviceType.Desktop),
                ("Mobile Safari", "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1", DeviceType.Mobile),
                ("iPad", "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1", DeviceType.Tablet)
            };

            var results = new Dictionary<string, object>();
            var allTestsPassed = true;

            foreach (var (name, userAgent, expectedType) in testCases)
            {
                var capabilities = _deviceDetectionService.GetDeviceCapabilities(userAgent);
                var testPassed = capabilities.Type == expectedType;
                allTestsPassed &= testPassed;

                results[$"test_{name.Replace(" ", "_").ToLower()}"] = new
                {
                    Expected = expectedType.ToString(),
                    Actual = capabilities.Type.ToString(),
                    Passed = testPassed,
                    capabilities.Browser,
                    OS = capabilities.OperatingSystem
                };
            }

            results["lastCheckTime"] = DateTime.UtcNow;
            results["allTestsPassed"] = allTestsPassed;

            return allTestsPassed
                ? Task.FromResult(HealthCheckResult.Healthy("Device detection service is fully operational", results))
                : Task.FromResult(HealthCheckResult.Degraded("Device detection service has some detection issues", data: results));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Device detection service failed", ex));
        }
    }
}

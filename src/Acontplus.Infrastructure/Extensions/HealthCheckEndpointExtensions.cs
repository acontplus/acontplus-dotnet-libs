namespace Acontplus.Infrastructure.Extensions;

/// <summary>
///     Extension methods for mapping health check endpoints with consistent formatting.
/// </summary>
public static class HealthCheckEndpointExtensions
{
    /// <summary>
    ///     Maps health check endpoints with standardized JSON response formatting including application name.
    ///     Maps standard endpoints (<c>/health</c>, <c>/health/ready</c>, <c>/health/live</c>, <c>/health/cache</c>, <c>/health/resilience</c>)
    ///     and registers <c>/alive</c> as an alias for liveness probes in .NET Aspire and Kubernetes.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <param name="basePath">The base path for health check endpoints (default: "/health").</param>
    public static void MapHealthCheckEndpoints(this WebApplication app, string basePath = "/health")
    {
        var appName = app.Environment.ApplicationName;

        MapTaggedHealthCheck(app, basePath, null, appName);
        MapTaggedHealthCheck(app, $"{basePath}/ready", "ready", appName);
        MapTaggedHealthCheck(app, $"{basePath}/live", "live", appName);
        MapTaggedHealthCheck(app, $"{basePath}/cache", "cache", appName);
        MapTaggedHealthCheck(app, $"{basePath}/resilience", "resilience", appName);

        // .NET Aspire and Kubernetes standard liveness probe alias
        if (basePath.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            MapTaggedHealthCheck(app, "/alive", "live", appName);
        }
    }

    private static void MapTaggedHealthCheck(WebApplication app, string path, string? tag, string appName)
    {
        var options = new HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        };

        if (tag != null)
        {
            options.Predicate = check => check.Tags.Contains(tag);
        }

        app.MapHealthChecks(path, options);
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report, string appName)
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            apiName = appName,
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration
        });

        return context.Response.WriteAsync(result, context.RequestAborted);
    }
}

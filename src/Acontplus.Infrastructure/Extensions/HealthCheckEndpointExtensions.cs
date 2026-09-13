namespace Acontplus.Infrastructure.Extensions;

/// <summary>
///     Extension methods for mapping health check endpoints with consistent formatting.
/// </summary>
public static class HealthCheckEndpointExtensions
{
    /// <summary>
    ///     Maps health check endpoints with standardized JSON response formatting including application name.
    /// </summary>
    /// <param name="app">The web application builder.</param>
    /// <param name="basePath">The base path for health check endpoints (default: "/health").</param>
    public static void MapHealthCheckEndpoints(this WebApplication app, string basePath = "/health")
    {
        var appName = app.Environment.ApplicationName;

        // Main health endpoint - shows all checks
        app.MapHealthChecks(basePath, new HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        });

        // Ready endpoint - shows only ready-tagged checks
        app.MapHealthChecks($"{basePath}/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        });

        // Live endpoint - shows only live-tagged checks
        app.MapHealthChecks($"{basePath}/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        });

        // Cache endpoint - shows only cache-tagged checks
        app.MapHealthChecks($"{basePath}/cache", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("cache"),
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        });

        // Resilience endpoint - shows only resilience-tagged checks
        app.MapHealthChecks($"{basePath}/resilience", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("resilience"),
            ResponseWriter = (context, report) => WriteHealthCheckResponse(context, report, appName)
        });
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

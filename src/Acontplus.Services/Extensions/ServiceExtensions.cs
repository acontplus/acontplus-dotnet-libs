namespace Acontplus.Services.Extensions;

/// <summary>
/// Extension methods for registering application-level services.
/// NOTE: For infrastructure services (caching, resilience, HTTP clients), use Acontplus.Infrastructure package.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add device detection service.
    /// </summary>
    public static IServiceCollection AddDeviceDetection(this IServiceCollection services)
    {
        services.AddScoped<IDeviceDetectionService, DeviceDetectionService>();
        return services;
    }

    /// <summary>
    /// Add request context service.
    /// </summary>
    public static IServiceCollection AddRequestContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RequestContextConfiguration>(configuration.GetSection("RequestContext"));
        services.AddScoped<IRequestContextService, RequestContextService>();
        return services;
    }

    /// <summary>
    /// Add security header service.
    /// </summary>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
    {
        services.AddScoped<ISecurityHeaderService, SecurityHeaderService>();
        return services;
    }

    /// <summary>
    /// Add lookup service with caching support.
    /// Requires IUnitOfWork and ICacheService to be registered.
    /// </summary>
    public static IServiceCollection AddLookupService(this IServiceCollection services)
    {
        services.AddScoped<ILookupService, LookupService>();
        return services;
    }
}



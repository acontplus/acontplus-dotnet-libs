using Acontplus.S3Application.Configuration;
using Acontplus.S3Application.Interfaces;
using Acontplus.S3Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.S3Application.Extensions;

/// <summary>
/// Extension methods for registering S3 storage services in the dependency injection container.
/// </summary>
public static class S3ServiceCollectionExtensions
{
    /// <summary>
    /// Registers S3 storage services with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS3Storage(this IServiceCollection services)
    {
        services.AddSingleton<IS3StorageService, S3StorageService>();
        services.Configure<S3StorageOptions>(options => { }); // Use defaults

        return services;
    }

    /// <summary>
    /// Registers S3 storage services with configuration from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS3Storage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3StorageOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.AddSingleton<IS3StorageService, S3StorageService>();

        return services;
    }

    /// <summary>
    /// Registers S3 storage services with explicit configuration action.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure S3 storage options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddS3Storage(
        this IServiceCollection services,
        Action<S3StorageOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IS3StorageService, S3StorageService>();

        return services;
    }
}

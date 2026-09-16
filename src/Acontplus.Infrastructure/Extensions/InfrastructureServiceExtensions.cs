namespace Acontplus.Infrastructure.Extensions;

/// <summary>
///     Service collection extensions for registering infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    private const string ResponseCompressionSectionName = "ResponseCompression";

    private static readonly string[] DefaultCompressionMimeTypes =
    [
        "application/json",
        "application/xml",
        "text/plain",
        "text/css",
        "application/javascript",
        "text/javascript",
        "application/json-patch+json"
    ];

    private static readonly string[] CacheHealthTags = ["ready", "cache"];
    private static readonly string[] CircuitBreakerHealthTags = ["ready", "resilience"];
    private static readonly string[] SelfHealthTags = ["live", "ready"];

    /// <summary>
    ///     Adds all infrastructure services (caching, resilience, HTTP client factory, health checks, response compression).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool addHealthChecks = false,
        bool addResponseCompression = false)
    {
        // Add caching services
        services.AddCachingServices(configuration);

        // Add resilience services
        services.AddResilienceServices(configuration);

        // Add HTTP client factory with resilience
        services.AddResilientHttpClients(configuration);

        // Optionally add health checks
        if (addHealthChecks)
        {
            services.AddInfrastructureHealthChecks();
        }

        // Optionally add response compression
        if (addResponseCompression)
        {
            services.AddResponseCompression(configuration);
        }

        return services;
    }

    /// <summary>
    ///     Adds caching services (in-memory or distributed with Redis).
    /// </summary>
    public static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheConfig = configuration.GetSection("Caching").Get<CacheConfiguration>()
                          ?? new CacheConfiguration();

        services.Configure<CacheConfiguration>(configuration.GetSection("Caching"));

        var redisConnectionString = cacheConfig.RedisConnectionString;
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            redisConnectionString = configuration.GetConnectionString("cache")
                                    ?? configuration.GetConnectionString("redis");
        }

        var isExplicitlyDisabled = string.Equals(configuration.GetSection("Caching:UseDistributedCache").Value, "false", StringComparison.OrdinalIgnoreCase);
        var useDistributed = cacheConfig.UseDistributedCache || (!string.IsNullOrEmpty(redisConnectionString) && !isExplicitlyDisabled);

        if (useDistributed && !string.IsNullOrEmpty(redisConnectionString))
        {
            // Register distributed cache (Redis)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = cacheConfig.RedisInstanceName ?? "acontplus:";
            });

            services.AddSingleton<ICacheService, DistributedCacheService>();
        }
        else
        {
            // Register in-memory cache
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = cacheConfig.MemoryCacheSizeLimit;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(cacheConfig.ExpirationScanFrequencyMinutes);
            });

            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    ///     Adds resilience services (circuit breaker, retry policies).
    /// </summary>
    public static IServiceCollection AddResilienceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceConfiguration>(configuration.GetSection("Resilience"));

        services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();
        services.AddSingleton<RetryPolicyService>();

        return services;
    }

    /// <summary>
    ///     Adds HTTP client factory with resilience patterns.
    /// </summary>
    public static IServiceCollection AddResilientHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var resilienceConfig = configuration.GetSection("Resilience").Get<ResilienceConfiguration>()
                               ?? new ResilienceConfiguration();

        // Register HTTP client factory
        services.AddHttpClient();

        // Register default HTTP client with resilience
        services.AddHttpClient("default")
            .AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(resilienceConfig.CircuitBreaker.SamplingDurationSeconds);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(resilienceConfig.Timeout.DefaultTimeoutSeconds);
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(resilienceConfig.Timeout.DefaultTimeoutSeconds * 2);
            });

        // Register API HTTP client with more lenient settings
        services.AddHttpClient("api")
            .AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(resilienceConfig.CircuitBreaker.SamplingDurationSeconds);
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(resilienceConfig.Timeout.HttpClientTimeoutSeconds);
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(resilienceConfig.Timeout.HttpClientTimeoutSeconds * 2);
            });

        // Register external HTTP client with strict settings
        services.AddHttpClient("external")
            .AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            });

        // Register long-running HTTP client
        services.AddHttpClient("long-running")
            .AddStandardResilienceHandler(options =>
            {
                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(resilienceConfig.CircuitBreaker.SamplingDurationSeconds);
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(resilienceConfig.Timeout.LongRunningTimeoutSeconds);
                options.TotalRequestTimeout.Timeout =
                    TimeSpan.FromSeconds(resilienceConfig.Timeout.LongRunningTimeoutSeconds * 2);
            });

        services.AddSingleton<ResilientHttpClientFactory>();

        return services;
    }

    /// <summary>
    ///     Adds health checks for infrastructure services (only if the services are registered).
    /// </summary>
    public static IServiceCollection AddInfrastructureHealthChecks(
        this IServiceCollection services)
    {
        var healthChecksBuilder = services.AddHealthChecks();
        var anyCheckAdded = false;

        // Add cache health check only if ICacheService is registered
        if (services.Any(d => d.ServiceType == typeof(ICacheService)))
        {
            healthChecksBuilder.AddCheck<CacheHealthCheck>(
                "cache",
                tags: CacheHealthTags);
            anyCheckAdded = true;
        }

        // Add circuit breaker health check only if ICircuitBreakerService is registered
        if (services.Any(d => d.ServiceType == typeof(ICircuitBreakerService)))
        {
            healthChecksBuilder.AddCheck<CircuitBreakerHealthCheck>(
                "circuit-breaker",
                tags: CircuitBreakerHealthTags);
            anyCheckAdded = true;
        }

        // If no specific infrastructure checks were added, add a simple self check
        // This ensures the health endpoint always returns something valid
        if (!anyCheckAdded)
        {
            healthChecksBuilder.AddCheck<SelfHealthCheck>(
                "self",
                tags: SelfHealthTags);
        }

        return services;
    }

    /// <summary>
    ///     Adds response compression services with configurable options.
    /// </summary>
    public static IServiceCollection AddResponseCompression(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var compressionConfig = configuration.GetSection(ResponseCompressionSectionName).Get<ResponseCompressionConfiguration>()
                                ?? new ResponseCompressionConfiguration();

        services.Configure<ResponseCompressionConfiguration>(configuration.GetSection(ResponseCompressionSectionName));

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = compressionConfig.EnableForHttps;

            // Set default MIME types if none specified
            options.MimeTypes = compressionConfig.MimeTypes.Count == 0
                ? ResponseCompressionDefaults.MimeTypes.Concat(DefaultCompressionMimeTypes).ToList()
                : compressionConfig.MimeTypes;

            // Add compression providers in order of preference (Brotli first for better compression)
            if (compressionConfig.EnableBrotli)
            {
                var brotliLevel = compressionConfig.BrotliLevel switch
                {
                    "Fastest" => CompressionLevel.Fastest,
                    "NoCompression" => CompressionLevel.NoCompression,
                    _ => CompressionLevel.Optimal
                };
                options.Providers.Add(new BrotliCompressionProvider(Options.Create(new BrotliCompressionProviderOptions
                { Level = brotliLevel })));
            }

            if (compressionConfig.EnableGzip)
            {
                var gzipLevel = compressionConfig.GzipLevel switch
                {
                    "Fastest" => CompressionLevel.Fastest,
                    "NoCompression" => CompressionLevel.NoCompression,
                    _ => CompressionLevel.Optimal
                };
                options.Providers.Add(new GzipCompressionProvider(Options.Create(new GzipCompressionProviderOptions
                { Level = gzipLevel })));
            }
        });

        return services;
    }
}

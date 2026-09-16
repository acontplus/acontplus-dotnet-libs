namespace Acontplus.Infrastructure.Http;

/// <summary>
///     Factory for creating HTTP clients with resilience patterns (circuit breaker, retry, timeout).
/// </summary>
public class ResilientHttpClientFactory(
    IHttpClientFactory httpClientFactory,
    ILogger<ResilientHttpClientFactory> logger,
    IOptions<ResilienceConfiguration> config)
{
    private readonly ResilienceConfiguration _config = config.Value;

    /// <summary>
    ///     Creates an HTTP client with standard resilience policies.
    /// </summary>
    public HttpClient CreateClient(string name = "default")
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Creating resilient HTTP client '{ClientName}'", name);
        }

        return httpClientFactory.CreateClient(name);
    }

    /// <summary>
    ///     Creates an HTTP client with custom timeout.
    /// </summary>
    public HttpClient CreateClientWithTimeout(string name, TimeSpan timeout)
    {
        var client = httpClientFactory.CreateClient(name);
        client.Timeout = timeout;
        return client;
    }

    /// <summary>
    ///     Creates an HTTP client for API calls with appropriate resilience settings.
    /// </summary>
    public HttpClient CreateApiClient() => CreateClient("api");

    /// <summary>
    ///     Creates an HTTP client for external service calls with strict resilience settings.
    /// </summary>
    public HttpClient CreateExternalClient() => CreateClient("external");

    /// <summary>
    ///     Creates an HTTP client for long-running operations.
    /// </summary>
    public HttpClient CreateLongRunningClient()
    {
        return CreateClientWithTimeout(
            "long-running",
            TimeSpan.FromSeconds(_config.Timeout.LongRunningTimeoutSeconds));
    }
}

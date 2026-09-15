using Acontplus.Persistence.Common.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Acontplus.Persistence.Common.Resilience;

/// <summary>
/// Provides factory methods for creating shared resilience policies across persistence repositories.
/// </summary>
public static class PersistenceResilienceHelper
{
    /// <summary>
    /// Creates an asynchronous retry policy based on persistence resilience options.
    /// </summary>
    /// <param name="resilienceOptions">The configured resilience options.</param>
    /// <param name="isTransientException">Predicate function to determine if an exception is transient.</param>
    /// <param name="logger">Logger for emitting warning logs during retries.</param>
    /// <param name="providerName">The database provider name.</param>
    /// <param name="repositoryKind">Label for the repository kind, e.g. "ADO" or "Dapper".</param>
    /// <returns>An <see cref="AsyncRetryPolicy"/> configured according to the resilience options.</returns>
    public static AsyncRetryPolicy CreateRetryPolicy(
        PersistenceResilienceOptions resilienceOptions,
        Func<Exception, bool> isTransientException,
        ILogger logger,
        string providerName,
        string repositoryKind = "ADO")
    {
        ArgumentNullException.ThrowIfNull(resilienceOptions);
        ArgumentNullException.ThrowIfNull(isTransientException);
        ArgumentNullException.ThrowIfNull(logger);

        if (!resilienceOptions.RetryPolicy.Enabled)
        {
            return Policy
                .Handle<Exception>(_ => false)
                .RetryAsync(0);
        }

        var maxRetries = resilienceOptions.RetryPolicy.MaxRetries;
        var baseDelay = TimeSpan.FromSeconds(resilienceOptions.RetryPolicy.BaseDelaySeconds);
        var maxDelay = TimeSpan.FromSeconds(resilienceOptions.RetryPolicy.MaxDelaySeconds);
        var exponentialBackoff = resilienceOptions.RetryPolicy.ExponentialBackoff;

        return Policy
            .Handle<Exception>(isTransientException)
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt =>
                {
                    if (exponentialBackoff)
                    {
                        var calculatedDelay = TimeSpan.FromSeconds(
                            resilienceOptions.RetryPolicy.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1));
                        return calculatedDelay > maxDelay ? maxDelay : calculatedDelay;
                    }
                    return baseDelay;
                },
                (exception, timeSpan, retryCount, _) =>
                {
                    logger.LogWarning(
                        exception,
                        "[{RepositoryKind} Repository] Retry {RetryCount}/{MaxRetries} after {Delay}ms for {ProviderName} operation",
                        repositoryKind,
                        retryCount,
                        maxRetries,
                        timeSpan.TotalMilliseconds,
                        providerName);
                });
    }
}

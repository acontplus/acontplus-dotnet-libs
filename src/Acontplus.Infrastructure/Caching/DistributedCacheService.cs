namespace Acontplus.Infrastructure.Caching;

/// <summary>
///     Distributed cache service implementation using Redis or other distributed cache.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheService"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public T? Get<T>(string key)
    {
        try
        {
            var value = _cache.GetString(key);
            return string.IsNullOrEmpty(value) ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, ct);
            return string.IsNullOrEmpty(value) ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }

            _cache.SetString(key, jsonValue, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        try
        {
            var jsonValue = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.SetAbsoluteExpiration(expiration.Value);
            }

            await _cache.SetStringAsync(key, jsonValue, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(string key, out T? value)
    {
        try
        {
            var stringValue = _cache.GetString(key);
            if (string.IsNullOrEmpty(stringValue))
            {
                value = default;
                return false;
            }

            value = JsonSerializer.Deserialize<T>(stringValue);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            value = default;
            return false;
        }
    }

    /// <inheritdoc />
    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        var value = Get<T>(key);
        if (value != null)
        {
            return value;
        }

        value = factory();
        Set(key, value, expiration);
        return value;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var value = await GetAsync<T>(key, ct);
        if (value != null)
        {
            return value;
        }

        value = await factory();
        await SetAsync(key, value, expiration, ct);
        return value;
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Note: Distributed cache doesn't support clearing all entries by design
        // This is a limitation of Redis and other distributed cache providers
        _logger.LogWarning("Clear operation not supported for distributed cache - this is a platform limitation");
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        // Note: Distributed cache doesn't support clearing all entries by design
        // This is a limitation of Redis and other distributed cache providers
        _logger.LogWarning("Clear operation not supported for distributed cache - this is a platform limitation");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool Exists(string key)
    {
        try
        {
            var value = _cache.GetString(key);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public void RemoveByPrefix(string prefix)
    {
        _logger.LogWarning(
            "RemoveByPrefix is not supported for distributed cache. Use Redis-specific clients for pattern-based removal.");
    }

    /// <inheritdoc />
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        RemoveByPrefix(prefix);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public CacheStatistics GetStatistics()
    {
        // Note: Distributed cache providers (Redis, etc.) don't expose detailed statistics
        // through the IDistributedCache interface. For detailed Redis stats, use Redis-specific clients.
        return new CacheStatistics
        {
            TotalEntries = 0, // Not available through IDistributedCache
            TotalMemoryBytes = 0, // Not available through IDistributedCache
            HitRatePercentage = 0, // Not available through IDistributedCache
            MissRatePercentage = 0, // Not available through IDistributedCache
            Evictions = 0, // Not available through IDistributedCache
            LastCleanup = null // Not available through IDistributedCache
        };
    }

    /// <inheritdoc />
    public Task<CacheStatistics?> GetStatisticsAsync(CancellationToken ct = default) =>
        Task.FromResult<CacheStatistics?>(GetStatistics());
}

namespace Acontplus.Infrastructure.Caching;

/// <summary>
///     In-memory cache service implementation using IMemoryCache.
/// </summary>
public sealed class MemoryCacheService : ICacheService
{
    private readonly CacheConfiguration _config;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private long _hits;
    private long _misses;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        IOptions<CacheConfiguration> config)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _config = config.Value;
    }

    public T? Get<T>(string key)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T? value))
            {
                Interlocked.Increment(ref _hits);
                return value;
            }

            Interlocked.Increment(ref _misses);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            return default;
        }
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    expiration ?? TimeSpan.FromMinutes(_config.ExpirationScanFrequencyMinutes),
                Size = 1
            };

            _memoryCache.Set(key, value, cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _memoryCache.Remove(key);
            _locks.TryRemove(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key: {Key}", key);
        }
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out value))
            {
                Interlocked.Increment(ref _hits);
                return true;
            }

            Interlocked.Increment(ref _misses);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key: {Key}", key);
            value = default;
            return false;
        }
    }

    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        if (_memoryCache.TryGetValue(key, out T? cached))
        {
            Interlocked.Increment(ref _hits);
            return cached!;
        }

        var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        lockObj.Wait();

        try
        {
            if (_memoryCache.TryGetValue(key, out cached))
            {
                Interlocked.Increment(ref _hits);
                return cached!;
            }

            Interlocked.Increment(ref _misses);
            var value = factory();
            Set(key, value, expiration);
            return value;
        }
        finally
        {
            lockObj.Release();
        }
    }

    // Async versions
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) => Task.FromResult(Get<T>(key));

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        Set(key, value, expiration);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(key, out T? cached))
        {
            Interlocked.Increment(ref _hits);
            return cached!;
        }

        var lockObj = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await lockObj.WaitAsync(ct);

        try
        {
            if (_memoryCache.TryGetValue(key, out cached))
            {
                Interlocked.Increment(ref _hits);
                return cached!;
            }

            Interlocked.Increment(ref _misses);
            var value = await factory();
            Set(key, value, expiration);
            return value;
        }
        finally
        {
            lockObj.Release();
        }
    }

    public void Clear()
    {
        if (_memoryCache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
            _locks.Clear();
            _logger.LogInformation("Memory cache cleared");
        }
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Clear();
        return Task.CompletedTask;
    }

    public bool Exists(string key) => _memoryCache.TryGetValue(key, out _);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Exists(key));

    public void RemoveByPrefix(string prefix)
    {
        var keysToRemove = _locks.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var key in keysToRemove)
            Remove(key);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        RemoveByPrefix(prefix);
        return Task.CompletedTask;
    }

    public CacheStatistics GetStatistics()
    {
        var totalRequests = _hits + _misses;
        var hitRate = totalRequests > 0 ? _hits * 100.0 / totalRequests : 0;
        var missRate = totalRequests > 0 ? _misses * 100.0 / totalRequests : 0;

        return new CacheStatistics
        {
            TotalEntries = _locks.Count,
            TotalMemoryBytes = 0, // Not easily accessible through IMemoryCache
            HitRatePercentage = hitRate,
            MissRatePercentage = missRate,
            Evictions = 0, // Not tracked
            LastCleanup = null
        };
    }

    public Task<CacheStatistics?> GetStatisticsAsync(CancellationToken ct = default) =>
        Task.FromResult<CacheStatistics?>(GetStatistics());
}

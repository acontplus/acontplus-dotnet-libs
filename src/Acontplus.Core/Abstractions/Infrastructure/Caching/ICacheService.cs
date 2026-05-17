namespace Acontplus.Core.Abstractions.Infrastructure.Caching;

/// <summary>
/// Interface for caching service supporting both in-memory and distributed caching.
/// </summary>
public interface ICacheService
{
    /// <summary>Gets an item from the cache by key. Returns <c>null</c> if the key does not exist.</summary>
    T? Get<T>(string key);

    /// <summary>Sets an item in the cache with an optional expiration.</summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>Removes an item from the cache by key.</summary>
    void Remove(string key);

    /// <summary>Removes all cache entries whose keys start with the given prefix.</summary>
    void RemoveByPrefix(string prefix);

    /// <summary>Tries to get an item from the cache, returning <c>true</c> if found.</summary>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>Gets an item from the cache, or creates and caches it using the factory if absent.</summary>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);

    // ── Async variants ──────────────────────────────────────────────────────────

    /// <summary>Asynchronously gets an item from the cache by key.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Asynchronously sets an item in the cache.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>Asynchronously removes an item from the cache by key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Asynchronously removes all cache entries whose keys start with the given prefix.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>Asynchronously gets or creates a cached item using the async factory if absent.</summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>Returns current cache statistics if the underlying implementation supports them.</summary>
    Task<CacheStatistics?> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Cache statistics snapshot.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>Total number of cache entries currently held.</summary>
    public long TotalEntries { get; set; }

    /// <summary>Estimated memory usage in bytes.</summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>Cache hit rate as a percentage (0–100).</summary>
    public double HitRatePercentage { get; set; }

    /// <summary>Cache miss rate as a percentage (0–100).</summary>
    public double MissRatePercentage { get; set; }

    /// <summary>Total number of evictions since last reset.</summary>
    public long Evictions { get; set; }

    /// <summary>UTC timestamp of the last cache cleanup cycle.</summary>
    public DateTime? LastCleanup { get; set; }
}

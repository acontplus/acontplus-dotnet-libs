using System.Collections.Concurrent;

namespace Acontplus.Reports.Services;

/// <summary>
/// Cache entry for report definitions with expiration
/// </summary>
internal sealed class CachedReportDefinition : IDisposable
{
    public MemoryStream Stream { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastAccessedAt { get; set; }
    private bool _disposed;

    public CachedReportDefinition(MemoryStream stream)
    {
        Stream = stream;
        CreatedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
    }

    public bool IsExpired(TimeSpan ttl)
    {
        return DateTime.UtcNow - CreatedAt > ttl;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stream.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Thread-safe cache for report definitions with size limits and TTL
/// </summary>
public class ReportDefinitionCache : IDisposable
{
    private readonly ConcurrentDictionary<string, CachedReportDefinition> _cache = new();
    private readonly int _maxSize;
    private readonly TimeSpan _ttl;
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportDefinitionCache"/> class.
    /// </summary>
    /// <param name="maxSize">The maximum number of entries to retain in cache.</param>
    /// <param name="ttl">The time-to-live expiration for cached entries.</param>
    public ReportDefinitionCache(int maxSize, TimeSpan ttl)
    {
        _maxSize = maxSize;
        _ttl = ttl;
    }

    /// <summary>
    /// Gets a cached report definition stream, or creates and caches it using the specified factory.
    /// </summary>
    /// <param name="key">The cache key (report path).</param>
    /// <param name="factory">The asynchronous factory delegate producing the report stream.</param>
    /// <returns>A seekable memory stream containing the report definition.</returns>
    public async Task<MemoryStream> GetOrAddAsync(string key, Func<string, Task<MemoryStream>> factory)
    {
        // Try to get existing non-expired entry
        if (_cache.TryGetValue(key, out var cached))
        {
            if (!cached.IsExpired(_ttl))
            {
                cached.LastAccessedAt = DateTime.UtcNow;
                // Create a copy to avoid thread safety issues
                var copy = new MemoryStream();
                cached.Stream.Position = 0;
                await cached.Stream.CopyToAsync(copy);
                copy.Position = 0;
                return copy;
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(key, out var removed);
                removed?.Dispose();
            }
        }

        // Cleanup if cache is too large
        if (_cache.Count >= _maxSize)
        {
            await CleanupOldEntriesAsync();
        }

        // Create new entry
        var stream = await factory(key);
        var cachedEntry = new CachedReportDefinition(stream);
        _cache.TryAdd(key, cachedEntry);

        // Return a copy
        var result = new MemoryStream();
        stream.Position = 0;
        await stream.CopyToAsync(result);
        result.Position = 0;
        return result;
    }

    private async Task CleanupOldEntriesAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            // Remove expired entries first
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired(_ttl))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out var removed))
                {
                    removed.Dispose();
                }
            }

            // If still too many, remove least recently used
            if (_cache.Count >= _maxSize)
            {
                var toRemove = _cache
                    .OrderBy(kvp => kvp.Value.LastAccessedAt)
                    .Take(_cache.Count - _maxSize + 10) // Remove extra to avoid frequent cleanups
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in toRemove)
                {
                    if (_cache.TryRemove(key, out var removed))
                    {
                        removed.Dispose();
                    }
                }
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Removes and disposes all cached report definitions.
    /// </summary>
    public void Clear()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Dispose();
        }
        _cache.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="ReportDefinitionCache"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Clear();
                _cleanupLock.Dispose();
            }
            _disposed = true;
        }
    }
}

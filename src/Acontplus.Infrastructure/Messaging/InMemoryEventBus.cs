using Acontplus.Core.Abstractions.Messaging;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Acontplus.Infrastructure.Messaging;

/// <summary>
/// In-memory event bus implementation using System.Threading.Channels for high-performance,
/// scalable event-driven architecture. Suitable for horizontal and vertical scaling scenarios.
/// Thread-safe and optimized for high workload throughput.
/// </summary>
public sealed class InMemoryEventBus(ILogger<InMemoryEventBus> logger) : IEventBus, IDisposable
{
    private readonly ConcurrentDictionary<Type, Channel<object>> _channels = new();
    private readonly ILogger<InMemoryEventBus> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private bool _disposed;

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S2139",
        Justification = "Exception is logged before rethrowing to notify caller of event publishing failure.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2139",
        Justification = "Exception is logged before rethrowing to notify caller of event publishing failure.")]
    public async Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(eventData);

        try
        {
            var channel = GetOrCreateChannel<T>();
            await channel.Writer.WriteAsync(eventData, cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Event published: {EventType} at {Timestamp}",
                    typeof(T).Name,
                    DateTime.UtcNow);
            }
        }
        catch (ChannelClosedException ex)
        {
            _logger.LogWarning(ex, "Cannot publish to closed channel: {EventType}", typeof(T).Name);
            throw new InvalidOperationException($"Channel for {typeof(T).Name} is closed.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> SubscribeAsync<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var channel = GetOrCreateChannel<T>();
        var reader = channel.Reader.Cast<T>();

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Subscriber started for event type: {EventType}", typeof(T).Name);
        }

        await foreach (var item in reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Subscriber completed for event type: {EventType}", typeof(T).Name);
        }
    }

    /// <summary>
    /// Gets or creates a channel for the specified event type.
    /// Uses unbounded channels optimized for multi-producer, multi-consumer scenarios.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <returns>A channel for the event type.</returns>
    private Channel<object> GetOrCreateChannel<T>() where T : class
    {
        return _channels.GetOrAdd(typeof(T), _ =>
        {
            var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                SingleWriter = false,       // Multiple publishers
                SingleReader = false,       // Multiple subscribers
                AllowSynchronousContinuations = false  // Prevent deadlocks
            });

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Created new channel for event type: {EventType}", typeof(T).Name);
            }
            return channel;
        });
    }

    /// <summary>
    /// Disposes the event bus and completes all active channels.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var channel in _channels.Values)
        {
            channel.Writer.Complete();
        }

        _channels.Clear();
        _logger.LogInformation("InMemoryEventBus disposed. All channels completed.");
    }
}

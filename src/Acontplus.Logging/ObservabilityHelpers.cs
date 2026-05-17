using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Trace;

namespace Acontplus.Logging;

/// <summary>
/// Example helper class demonstrating how to use ActivitySource for distributed tracing.
/// This class can be injected via DI and used throughout your application.
/// </summary>
public class TracingHelper
{
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TracingHelper"/> class.
    /// </summary>
    /// <param name="activitySource">The activity source for creating traces.</param>
    public TracingHelper(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    /// <summary>
    /// Creates a new activity (span) for tracing.
    /// </summary>
    /// <param name="name">The name of the activity/operation.</param>
    /// <param name="kind">The kind of activity (default: Internal).</param>
    /// <returns>The created activity, or null if tracing is disabled.</returns>
    /// <example>
    /// <code>
    /// using var activity = _tracingHelper.StartActivity("ProcessOrder");
    /// activity?.SetTag("order.id", orderId);
    /// activity?.SetTag("customer.id", customerId);
    /// try
    /// {
    ///     // Process order logic
    ///     activity?.SetStatus(ActivityStatusCode.Ok);
    /// }
    /// catch (Exception ex)
    /// {
    ///     activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    ///     activity?.RecordException(ex);
    ///     throw;
    /// }
    /// </code>
    /// </example>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(name, kind);
    }

    /// <summary>
    /// Adds a tag to the current activity if one is active.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    public void AddTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    public void RecordException(Exception exception)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.AddException(exception);
        }
    }

    /// <summary>
    /// Adds an event to the current activity.
    /// </summary>
    /// <param name="name">The event name.</param>
    public void AddEvent(string name)
    {
        Activity.Current?.AddEvent(new ActivityEvent(name));
    }
}

/// <summary>
/// Example helper class demonstrating how to use Meter for custom metrics.
/// This class can be injected via DI and used throughout your application.
/// </summary>
public class MetricsHelper
{
    private readonly Meter _meter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsHelper"/> class.
    /// </summary>
    /// <param name="meter">The meter for creating metrics.</param>
    public MetricsHelper(Meter meter)
    {
        _meter = meter;
    }

    /// <summary>
    /// Creates a counter metric for tracking counts.
    /// </summary>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">The unit of measurement (e.g., "requests", "errors").</param>
    /// <param name="description">The metric description.</param>
    /// <returns>A counter instrument.</returns>
    /// <example>
    /// <code>
    /// private readonly Counter&lt;long&gt; _orderCounter;
    ///
    /// public OrderService(MetricsHelper metricsHelper)
    /// {
    ///     _orderCounter = metricsHelper.CreateCounter&lt;long&gt;("orders.processed", "orders", "Total orders processed");
    /// }
    ///
    /// public void ProcessOrder(Order order)
    /// {
    ///     // Process order
    ///     _orderCounter.Add(1, new KeyValuePair&lt;string, object?&gt;("status", "success"));
    /// }
    /// </code>
    /// </example>
    public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateCounter<T>(name, unit, description);
    }

    /// <summary>
    /// Creates a histogram metric for tracking value distributions.
    /// </summary>
    /// <typeparam name="T">The numeric type of the histogram.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">The unit of measurement (e.g., "ms", "bytes").</param>
    /// <param name="description">The metric description.</param>
    /// <returns>A histogram instrument.</returns>
    /// <example>
    /// <code>
    /// private readonly Histogram&lt;double&gt; _requestDuration;
    ///
    /// public ApiController(MetricsHelper metricsHelper)
    /// {
    ///     _requestDuration = metricsHelper.CreateHistogram&lt;double&gt;("api.request.duration", "ms", "API request duration");
    /// }
    ///
    /// public async Task&lt;IActionResult&gt; GetData()
    /// {
    ///     var stopwatch = Stopwatch.StartNew();
    ///     var result = await _service.GetDataAsync();
    ///     _requestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair&lt;string, object?&gt;("endpoint", "GetData"));
    ///     return Ok(result);
    /// }
    /// </code>
    /// </example>
    public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateHistogram<T>(name, unit, description);
    }

    /// <summary>
    /// Creates an observable gauge for tracking current values.
    /// </summary>
    /// <typeparam name="T">The numeric type of the gauge.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="observeValue">The function to observe the current value.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="description">The metric description.</param>
    /// <returns>An observable gauge instrument.</returns>
    /// <example>
    /// <code>
    /// private int _activeConnections = 0;
    ///
    /// public ConnectionManager(MetricsHelper metricsHelper)
    /// {
    ///     metricsHelper.CreateObservableGauge("connections.active", () => _activeConnections, "connections", "Active connections");
    /// }
    /// </code>
    /// </example>
    public ObservableGauge<T> CreateObservableGauge<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null)
        where T : struct
    {
        return _meter.CreateObservableGauge(name, observeValue, unit, description);
    }

    /// <summary>
    /// Creates an up-down counter for values that can increase or decrease.
    /// </summary>
    /// <typeparam name="T">The numeric type of the counter.</typeparam>
    /// <param name="name">The metric name.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="description">The metric description.</param>
    /// <returns>An up-down counter instrument.</returns>
    /// <example>
    /// <code>
    /// private readonly UpDownCounter&lt;int&gt; _queueSize;
    ///
    /// public QueueService(MetricsHelper metricsHelper)
    /// {
    ///     _queueSize = metricsHelper.CreateUpDownCounter&lt;int&gt;("queue.size", "items", "Queue size");
    /// }
    ///
    /// public void Enqueue(Item item)
    /// {
    ///     _queue.Enqueue(item);
    ///     _queueSize.Add(1);
    /// }
    ///
    /// public Item Dequeue()
    /// {
    ///     var item = _queue.Dequeue();
    ///     _queueSize.Add(-1);
    ///     return item;
    /// }
    /// </code>
    /// </example>
    public UpDownCounter<T> CreateUpDownCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return _meter.CreateUpDownCounter<T>(name, unit, description);
    }
}

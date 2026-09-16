namespace Acontplus.Services.Filters;

/// <summary>
/// Action filter for logging request details and performance metrics.
/// </summary>
/// <param name="logger">The logger instance.</param>
public class RequestLoggingActionFilter(ILogger<RequestLoggingActionFilter> logger) : IAsyncActionFilter
{
    private readonly ILogger<RequestLoggingActionFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.HttpContext.Request;
        var correlationId = context.HttpContext.TraceIdentifier;

        // Log request start
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Request started: {Method} {Path} - CorrelationId: {CorrelationId}",
                request.Method, request.Path, correlationId);
        }

        var executedContext = await next();
        stopwatch.Stop();

        if (executedContext.Exception != null && !executedContext.ExceptionHandled)
        {
            _logger.LogError(executedContext.Exception,
                "Request failed: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                request.Method, request.Path, stopwatch.ElapsedMilliseconds, correlationId);
        }
        else
        {
            // Log successful completion
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.Method, request.Path, context.HttpContext.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds, correlationId);
            }

            // Log performance warning for slow requests
            if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.Method, request.Path, stopwatch.ElapsedMilliseconds, correlationId);
            }
        }
    }
}

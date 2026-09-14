namespace Acontplus.Services.Filters;

/// <summary>
/// Action filter for logging request details and performance metrics.
/// </summary>
public class RequestLoggingActionFilter : IAsyncActionFilter
{
    private readonly ILogger<RequestLoggingActionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingActionFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RequestLoggingActionFilter(ILogger<RequestLoggingActionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.HttpContext.Request;
        var correlationId = context.HttpContext.TraceIdentifier;

        // Log request start
        _logger.LogInformation(
            "Request started: {Method} {Path} - CorrelationId: {CorrelationId}",
            request.Method, request.Path, correlationId);

        try
        {
            var result = await next();
            stopwatch.Stop();

            // Log successful completion
            _logger.LogInformation(
                "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                request.Method, request.Path, context.HttpContext.Response.StatusCode,
                stopwatch.ElapsedMilliseconds, correlationId);

            // Log performance warning for slow requests
            if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                    request.Method, request.Path, stopwatch.ElapsedMilliseconds, correlationId);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Request failed: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                request.Method, request.Path, stopwatch.ElapsedMilliseconds, correlationId);

            throw;
        }
    }
}
namespace Acontplus.Services.Middleware;

/// <summary>
/// Middleware that logs HTTP request execution times, response status codes, and assigns a request identifier.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger instance.</param>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

    /// <summary>
    /// Executes the middleware to time and log the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        // Add request ID to response headers
        context.Response.Headers.Append("X-Request-ID", requestId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var duration = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 400)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms (RequestId: {RequestId})",
                        method, path, statusCode, duration, requestId);
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms (RequestId: {RequestId})",
                        method, path, statusCode, duration, requestId);
                }
            }
        }
    }
}
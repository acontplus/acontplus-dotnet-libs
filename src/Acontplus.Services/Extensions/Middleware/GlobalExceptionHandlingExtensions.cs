namespace Acontplus.Services.Extensions.Middleware;

/// <summary>
/// Provides extension methods for configuring global exception handling middleware.
/// </summary>
public static class GlobalExceptionHandlingExtensions
{
    /// <summary>
    /// Adds the Acontplus exception handling middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Optional configuration for exception handling options.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseAcontplusExceptionHandling(
        this IApplicationBuilder app,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        var options = new ExceptionHandlingOptions();
        configure?.Invoke(options);

        return app.UseMiddleware<ApiExceptionMiddleware>(options);
    }
}

/// <summary>
/// Configuration options for <see cref="ApiExceptionMiddleware"/>.
/// </summary>
public class ExceptionHandlingOptions
{
    /// <summary>
    /// Includes HTTP request details (Method, Path, etc.) in logs. Default: true.
    /// </summary>
    public bool IncludeRequestDetails { get; set; } = true;

    /// <summary>
    /// Logs the request body when an error occurs (caution: sensitive data). Default: false.
    /// </summary>
    public bool LogRequestBody { get; set; } = false;

    /// <summary>
    /// Includes stack traces and exception details in API responses (for debugging). Default: false.
    /// </summary>
    public bool IncludeDebugDetailsInResponse { get; set; } = false;
}
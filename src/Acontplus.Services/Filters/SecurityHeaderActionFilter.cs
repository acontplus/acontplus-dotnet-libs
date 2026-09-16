namespace Acontplus.Services.Filters;

/// <summary>
/// Action filter for applying security headers to responses.
/// </summary>
/// <param name="securityHeaderService">The security header service.</param>
/// <param name="configuration">The request context configuration options.</param>
/// <param name="logger">The logger instance.</param>
public class SecurityHeaderActionFilter(
    ISecurityHeaderService securityHeaderService,
    IOptions<RequestContextConfiguration> configuration,
    ILogger<SecurityHeaderActionFilter> logger) : IActionFilter
{
    private readonly ISecurityHeaderService _securityHeaderService = securityHeaderService ?? throw new ArgumentNullException(nameof(securityHeaderService));
    private readonly RequestContextConfiguration _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<SecurityHeaderActionFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Apply security headers before action execution
        try
        {
            _securityHeaderService.ApplySecurityHeaders(context.HttpContext, _configuration);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Security headers applied for {Path}", context.HttpContext.Request.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply security headers for {Path}", context.HttpContext.Request.Path);
            // Don't throw - security headers shouldn't break the request
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Validate that security headers were applied correctly
        try
        {
            var isValid = _securityHeaderService.ValidateSecurityHeaders(context.HttpContext);
            if (!isValid)
            {
                _logger.LogWarning("Security header validation failed for {Path}", context.HttpContext.Request.Path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate security headers for {Path}", context.HttpContext.Request.Path);
        }
    }
}
namespace Acontplus.Services.Filters;

/// <summary>
/// Action filter for applying security headers to responses.
/// </summary>
public class SecurityHeaderActionFilter : IActionFilter
{
    private readonly ISecurityHeaderService _securityHeaderService;
    private readonly RequestContextConfiguration _configuration;
    private readonly ILogger<SecurityHeaderActionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeaderActionFilter"/> class.
    /// </summary>
    /// <param name="securityHeaderService">The security header service.</param>
    /// <param name="configuration">The request context configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public SecurityHeaderActionFilter(
        ISecurityHeaderService securityHeaderService,
        IOptions<RequestContextConfiguration> configuration,
        ILogger<SecurityHeaderActionFilter> logger)
    {
        _securityHeaderService = securityHeaderService ?? throw new ArgumentNullException(nameof(securityHeaderService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Apply security headers before action execution
        try
        {
            _securityHeaderService.ApplySecurityHeaders(context.HttpContext, _configuration);
            _logger.LogDebug("Security headers applied for {Path}", context.HttpContext.Request.Path);
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
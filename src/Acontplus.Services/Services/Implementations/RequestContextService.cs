namespace Acontplus.Services.Services.Implementations;

/// <summary>
/// Implementation of request context service using HTTP context accessor.
/// </summary>
public class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RequestContextService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestContextService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger instance.</param>
    public RequestContextService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<RequestContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string GetRequestId()
    {
        var context = GetHttpContext();
        return context.GetRequestId() ?? context.TraceIdentifier;
    }

    /// <inheritdoc />
    public string GetCorrelationId()
    {
        var context = GetHttpContext();
        return context.GetCorrelationId() ?? GetRequestId();
    }

    /// <inheritdoc />
    public string? GetTenantId()
    {
        var context = GetHttpContext();
        return context.GetTenantId();
    }

    /// <inheritdoc />
    public string? GetClientId()
    {
        var context = GetHttpContext();
        return context.GetClientId();
    }

    /// <inheritdoc />
    public string? GetIssuer()
    {
        var context = GetHttpContext();
        return context.GetIssuer();
    }

    /// <inheritdoc />
    public DeviceType GetDeviceType()
    {
        var context = GetHttpContext();
        return context.GetDeviceType() ?? DeviceType.Unknown;
    }

    /// <inheritdoc />
    public bool IsMobileRequest()
    {
        var context = GetHttpContext();
        return context.GetIsMobileRequest() ?? false;
    }

    /// <inheritdoc />
    public Dictionary<string, object?> GetRequestContext()
    {
        var context = GetHttpContext();

        return new Dictionary<string, object?>
        {
            [ApiMetadataKeys.RequestId] = GetRequestId(),
            [ApiMetadataKeys.CorrelationId] = GetCorrelationId(),
            [ApiMetadataKeys.TenantId] = GetTenantId(),
            [ApiMetadataKeys.ClientId] = GetClientId(),
            [ApiMetadataKeys.Issuer] = GetIssuer(),
            ["deviceType"] = GetDeviceType().ToString(),
            ["isMobileRequest"] = IsMobileRequest(),
            ["userAgent"] = context.Request.Headers.UserAgent.ToString(),
            ["ipAddress"] = context.Connection.RemoteIpAddress?.ToString(),
            [ApiMetadataKeys.TimestampUtc] = DateTime.UtcNow
        };
    }

    private HttpContext GetHttpContext()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            _logger.LogWarning("HTTP context is not available");
            throw new InvalidOperationException("HTTP context is not available");
        }
        return context;
    }
}

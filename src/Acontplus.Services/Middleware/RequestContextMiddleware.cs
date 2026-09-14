using Microsoft.Extensions.Primitives;

namespace Acontplus.Services.Middleware;

/// <summary>
/// Middleware that populates and validates ambient request context metadata (request ID, correlation ID, tenant ID, client ID, device type).
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="options">The request context configuration options.</param>
public sealed class RequestContextMiddleware(
    RequestDelegate next,
    ILogger<RequestContextMiddleware> logger, // Injected ILogger
    IOptions<RequestContextConfiguration> options)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    private readonly RequestContextConfiguration
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly ILogger<RequestContextMiddleware> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger)); // Added for logging

    // Injected IOptions<T>

    /// <summary>
    /// Executes the middleware for an incoming HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the completion of request processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Security Hardening (conditionally applied)
        if (_options.EnableSecurityHeaders)
        {
            ApplySecurityHeaders(context);
        }

        // 2. Request Identification
        var requestId = SanitizeHeader(context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.RequestId]) ??
                        Guid.NewGuid().ToString();
        var correlationId = SanitizeHeader(context.Request.Headers["Correlation-Id"]) ??
                            requestId;
        var tenantId = SanitizeHeader(context.Request.Headers["Tenant-Id"]) ??
                       requestId;
        _logger.LogDebug(
            "Processing request: RequestId: {RequestId}, CorrelationId: {CorrelationId}, TenantId: {TenantId}",
            requestId, correlationId, tenantId);

        // 3. Client Context
        var clientId = ValidateClientId(context);
        if (_options.RequireClientId && string.IsNullOrEmpty(clientId))
        {
            // If ClientId is required and still empty after validation,
            // you might want to short-circuit with a 400 Bad Request.
            // For this example, we'll assign the anonymous ID as per original intent,
            // but log a warning.
            clientId = _options.AnonymousClientId ?? "unknown-client";
            _logger.LogWarning("Client-Id header is missing or invalid. Assigning anonymous client ID: {ClientId}",
                clientId);
        }

        var issuer = SanitizeHeader(context.Request.Headers["Issuer"]);
        _logger.LogDebug("Client ID: {ClientId}, Issuer: {Issuer}", clientId, issuer);


        // 4. Device Detection
        var deviceType = GetDeviceType(context);
        var isMobileRequest = deviceType is DeviceType.Mobile or DeviceType.Tablet;
        _logger.LogDebug("Detected device type: {DeviceType}, IsMobileRequest: {IsMobileRequest}", deviceType,
            isMobileRequest);

        // 5. Context Storage (using HttpContext.Items and extension methods)
        context.SetRequestId(requestId);
        context.SetCorrelationId(correlationId);
        context.SetTenantId(tenantId);
        context.SetClientId(clientId);
        context.SetIssuer(issuer);
        context.SetDeviceType(deviceType);
        context.SetIsMobileRequest(isMobileRequest);

        // 6. Response Headers
        SetResponseHeaders(context, requestId, correlationId, clientId);

        await _next(context);
    }

    private void ApplySecurityHeaders(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME type sniffing.
        context.Response.Headers.XContentTypeOptions = "nosniff";

        // X-Frame-Options: Prevents clickjacking attacks.
        if (_options.FrameOptionsDeny)
        {
            context.Response.Headers.XFrameOptions = "DENY";
            _logger.LogDebug("Applied X-Frame-Options: DENY");
        }

        switch (string.IsNullOrEmpty(_options.ReferrerPolicy))
        {
            // Referrer-Policy: Controls how much referrer information is sent with requests.
            case false:
                context.Response.Headers["Referrer-Policy"] = _options.ReferrerPolicy;
                _logger.LogDebug("Applied Referrer-Policy: {ReferrerPolicy}", _options.ReferrerPolicy);
                break;
        }
    }

    private string? ValidateClientId(HttpContext context)
    {
        var clientId = SanitizeHeader(context.Request.Headers["Client-Id"]);

        switch (string.IsNullOrEmpty(clientId))
        {
            case false when _options.AllowedClientIds != null && _options.AllowedClientIds.Count != 0 &&
                            !_options.AllowedClientIds.Contains(clientId, StringComparer.OrdinalIgnoreCase):
                _logger.LogWarning("Received Client-Id '{ClientId}' is not in the list of allowed client IDs.",
                    clientId);
                return null; // Treat as invalid if not in allowed list
            default:
                return clientId;
        }
    }

    private static string? SanitizeHeader(StringValues headerValue)
    {
        var value = headerValue.FirstOrDefault();
        return string.IsNullOrWhiteSpace(value)
            ? null
            :
            // Simple sanitization: trim and remove potential control characters or common injection characters.
            // More robust validation might be needed depending on the expected content.
            value.Trim().Replace("\n", "").Replace("\r", "").Replace("\"", "").Replace("'", "");
    }

    private static DeviceType GetDeviceType(HttpContext context)
    {
        // Try Device-Type header first (preferred method)
        if (Enum.TryParse<DeviceType>(context.Request.Headers["Device-Type"].FirstOrDefault(),
                ignoreCase: true,
                out var parsedType))
        {
            return parsedType;
        }

        // Fall back to legacy X-Is-Mobile header
        if (bool.TryParse(context.Request.Headers["X-Is-Mobile"].FirstOrDefault(), out var isMobile))
        {
            return isMobile ? DeviceType.Mobile : DeviceType.Desktop;
        }

        // Final fallback to user agent detection
        return IsMobileUserAgent(context) ? DeviceType.Mobile : DeviceType.Desktop;
    }

    private static bool IsMobileUserAgent(HttpContext context)
    {
        var userAgent = context.Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent)) return false;

        // Basic check for common mobile indicators in user-agent string.
        // This is not exhaustive and can be unreliable for all devices.
        return userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) || // Added iPad
               userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase); // Added Tablet
    }

    private static void SetResponseHeaders(
        HttpContext context,
        string requestId,
        string correlationId,
        string? clientId)
    {
        context.Response.Headers.RequestId = requestId;
        context.Response.Headers["Correlation-Id"] = correlationId;

        if (!string.IsNullOrEmpty(clientId))
        {
            context.Response.Headers["Client-Id"] = clientId;
        }
    }
}

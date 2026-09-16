namespace Acontplus.Services.Services.Implementations;

/// <summary>
/// Implementation of security header service for managing HTTP security headers.
/// </summary>
/// <param name="logger">The logger instance.</param>
public class SecurityHeaderService(ILogger<SecurityHeaderService> logger) : ISecurityHeaderService
{
    private readonly ILogger<SecurityHeaderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public void ApplySecurityHeaders(HttpContext context, RequestContextConfiguration configuration)
    {
        if (!configuration.EnableSecurityHeaders)
        {
            _logger.LogDebug("Security headers are disabled");
            return;
        }

        // X-Content-Type-Options: Prevents MIME type sniffing
        context.Response.Headers.XContentTypeOptions = "nosniff";

        // X-Frame-Options: Prevents clickjacking attacks
        if (configuration.FrameOptionsDeny)
        {
            context.Response.Headers.XFrameOptions = "DENY";
            _logger.LogDebug("Applied X-Frame-Options: DENY");
        }

        // Referrer-Policy: Controls referrer information
        if (!string.IsNullOrEmpty(configuration.ReferrerPolicy))
        {
            context.Response.Headers["Referrer-Policy"] = configuration.ReferrerPolicy;
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Applied Referrer-Policy: {ReferrerPolicy}", configuration.ReferrerPolicy);
            }
        }

        // X-XSS-Protection: Enable XSS filtering
        context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.XXSSProtection] = "1; mode=block";

        // Permissions-Policy: Control browser features
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=()";

        _logger.LogDebug("Applied security headers to response");
    }

    /// <inheritdoc />
    public string GenerateCspNonce()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        var nonce = Convert.ToBase64String(bytes);

        _logger.LogDebug("Generated CSP nonce");
        return nonce;
    }

    /// <inheritdoc />
    public bool ValidateSecurityHeaders(HttpContext context)
    {
        var requiredHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "Referrer-Policy"
        };

        var missingHeaders = requiredHeaders
            .Where(header => !context.Response.Headers.ContainsKey(header))
            .ToList();

        if (missingHeaders.Count > 0)
        {
            _logger.LogWarning("Missing security headers: {MissingHeaders}",
                string.Join(", ", missingHeaders));
            return false;
        }

        _logger.LogDebug("All required security headers are present");
        return true;
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetRecommendedHeaders(bool isDevelopment)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Content-Type-Options"] = "nosniff",
            ["X-Frame-Options"] = "DENY",
            ["X-XSS-Protection"] = "1; mode=block",
            ["Referrer-Policy"] = "strict-origin-when-cross-origin",
            ["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()"
        };

        if (!isDevelopment)
        {
            // Add production-only headers
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com";
        }
        else
        {
            // Development-friendly CSP
            headers["Content-Security-Policy"] = "default-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com";
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Generated {Count} recommended security headers for {Environment}",
                headers.Count, isDevelopment ? "development" : "production");
        }

        return headers;
    }
}
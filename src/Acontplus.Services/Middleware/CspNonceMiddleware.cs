namespace Acontplus.Services.Middleware;


/// <summary>
/// Middleware that generates and attaches a unique Content Security Policy (CSP) nonce to the request context.
/// </summary>
/// <param name="next">The next middleware delegate.</param>
public class CspNonceMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    /// <summary>
    /// Invokes the middleware to generate and attach a CSP nonce.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a unique nonce for this request
        var nonce = GenerateNonce();
        context.Items["csp-nonce"] = nonce;

        await _next(context);
    }

    private static string GenerateNonce()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

/// <summary>
/// Extension methods for accessing the CSP nonce from the HTTP context.
/// </summary>
public static class CspNonceExtensions
{
    /// <summary>
    /// Retrieves the CSP nonce generated for the current HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The CSP nonce string, or empty string if not found.</returns>
    public static string GetCspNonce(this HttpContext context) =>
        context.Items["csp-nonce"]?.ToString() ?? string.Empty;
}
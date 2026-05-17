namespace Acontplus.Core.Domain.Enums;

/// <summary>
/// Extension methods that enrich <see cref="ErrorType"/> with HTTP-layer mappings
/// and diagnostic metadata (severity / category strings).
/// </summary>
public static class ErrorTypeExtensions
{
    /// <summary>Maps an <see cref="ErrorType"/> to the corresponding <see cref="HttpStatusCode"/>.</summary>
    public static HttpStatusCode ToHttpStatusCode(this ErrorType errorType) =>
        errorType switch
        {
            // Client Errors (4xx)
            ErrorType.BadRequest => HttpStatusCode.BadRequest,
            ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
            ErrorType.Forbidden => HttpStatusCode.Forbidden,
            ErrorType.NotFound => HttpStatusCode.NotFound,
            ErrorType.MethodNotAllowed => HttpStatusCode.MethodNotAllowed,
            ErrorType.NotAcceptable => HttpStatusCode.NotAcceptable,
            ErrorType.RequestTimeout => HttpStatusCode.RequestTimeout,
            ErrorType.Conflict => HttpStatusCode.Conflict,
            ErrorType.PreconditionFailed => HttpStatusCode.PreconditionFailed,
            ErrorType.PayloadTooLarge => HttpStatusCode.RequestEntityTooLarge,
            ErrorType.UriTooLong => HttpStatusCode.RequestUriTooLong,
            ErrorType.UnsupportedMediaType => HttpStatusCode.UnsupportedMediaType,
            ErrorType.RangeNotSatisfiable => HttpStatusCode.RequestedRangeNotSatisfiable,
            ErrorType.ExpectationFailed => HttpStatusCode.ExpectationFailed,
            ErrorType.Validation => HttpStatusCode.UnprocessableEntity,
            ErrorType.PreconditionRequired => HttpStatusCode.PreconditionRequired,
            ErrorType.RateLimited => HttpStatusCode.TooManyRequests,
            ErrorType.RequestHeadersTooLarge => HttpStatusCode.RequestHeaderFieldsTooLarge,
            ErrorType.UnavailableForLegal => HttpStatusCode.UnavailableForLegalReasons,

            // Server Errors (5xx)
            ErrorType.Internal => HttpStatusCode.InternalServerError,
            ErrorType.NotImplemented => HttpStatusCode.NotImplemented,
            ErrorType.External => HttpStatusCode.BadGateway,
            ErrorType.ServiceUnavailable => HttpStatusCode.ServiceUnavailable,
            ErrorType.Timeout => HttpStatusCode.GatewayTimeout,
            ErrorType.HttpVersionNotSupported => HttpStatusCode.HttpVersionNotSupported,
            ErrorType.InsufficientStorage => HttpStatusCode.InsufficientStorage,
            ErrorType.LoopDetected => HttpStatusCode.LoopDetected,
            ErrorType.NotExtended => HttpStatusCode.NotExtended,
            ErrorType.NetworkAuthRequired => HttpStatusCode.NetworkAuthenticationRequired,

            _ => HttpStatusCode.InternalServerError
        };

    /// <summary>Returns a lowercase severity string (<c>"warning"</c> or <c>"error"</c>) for use in API responses.</summary>
    public static string ToSeverityString(this ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "warning",
            ErrorType.BadRequest => "warning",
            ErrorType.NotFound => "warning",
            ErrorType.Conflict => "warning",
            ErrorType.MethodNotAllowed => "warning",
            ErrorType.NotAcceptable => "warning",
            ErrorType.PayloadTooLarge => "warning",
            ErrorType.UriTooLong => "warning",
            ErrorType.UnsupportedMediaType => "warning",
            ErrorType.RangeNotSatisfiable => "warning",
            ErrorType.ExpectationFailed => "warning",
            ErrorType.PreconditionFailed => "warning",
            ErrorType.PreconditionRequired => "warning",
            ErrorType.RequestHeadersTooLarge => "warning",
            ErrorType.UnavailableForLegal => "warning",
            _ => "error"
        };

    /// <summary>Returns a lowercase category string (e.g., <c>"validation"</c>, <c>"security"</c>) for grouping errors in API responses.</summary>
    public static string ToCategoryString(this ErrorType errorType) =>
        errorType switch
        {
            // Validation & Input Errors
            ErrorType.Validation => "validation",
            ErrorType.BadRequest => "validation",
            ErrorType.PayloadTooLarge => "validation",
            ErrorType.UriTooLong => "validation",
            ErrorType.UnsupportedMediaType => "validation",
            ErrorType.RangeNotSatisfiable => "validation",
            ErrorType.ExpectationFailed => "validation",
            ErrorType.RequestHeadersTooLarge => "validation",

            // Business Logic Errors
            ErrorType.NotFound => "business",
            ErrorType.Conflict => "business",
            ErrorType.MethodNotAllowed => "business",
            ErrorType.NotAcceptable => "business",
            ErrorType.PreconditionFailed => "business",
            ErrorType.PreconditionRequired => "business",
            ErrorType.UnavailableForLegal => "business",

            // Security Errors
            ErrorType.Unauthorized => "security",
            ErrorType.Forbidden => "security",
            ErrorType.NetworkAuthRequired => "security",

            // Performance Errors
            ErrorType.RateLimited => "performance",
            ErrorType.RequestTimeout => "performance",
            ErrorType.Timeout => "performance",

            // Integration Errors
            ErrorType.External => "integration",

            // System Errors
            ErrorType.Internal => "system",

            _ => "system"
        };

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> severity string.
    /// Prefer this over <see cref="ToSeverityString"/> in hot paths to avoid string allocation.
    /// </summary>
    public static ReadOnlySpan<char> ToSeveritySpan(this ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => "warning",
            _ => "error"
        };
}

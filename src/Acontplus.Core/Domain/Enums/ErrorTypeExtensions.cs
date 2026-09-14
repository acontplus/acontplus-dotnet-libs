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

    private const string WarningSeverity = "warning";
    private const string ErrorSeverity = "error";
    private const string ValidationCategory = "validation";
    private const string BusinessCategory = "business";
    private const string SecurityCategory = "security";
    private const string PerformanceCategory = "performance";
    private const string IntegrationCategory = "integration";
    private const string SystemCategory = "system";

    /// <summary>Returns a lowercase severity string (<c>"warning"</c> or <c>"error"</c>) for use in API responses.</summary>
    public static string ToSeverityString(this ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation or
            ErrorType.BadRequest or
            ErrorType.NotFound or
            ErrorType.Conflict or
            ErrorType.MethodNotAllowed or
            ErrorType.NotAcceptable or
            ErrorType.PayloadTooLarge or
            ErrorType.UriTooLong or
            ErrorType.UnsupportedMediaType or
            ErrorType.RangeNotSatisfiable or
            ErrorType.ExpectationFailed or
            ErrorType.PreconditionFailed or
            ErrorType.PreconditionRequired or
            ErrorType.RequestHeadersTooLarge or
            ErrorType.UnavailableForLegal => WarningSeverity,
            _ => ErrorSeverity
        };

    /// <summary>Returns a lowercase category string (e.g., <c>"validation"</c>, <c>"security"</c>) for grouping errors in API responses.</summary>
    public static string ToCategoryString(this ErrorType errorType) =>
        errorType switch
        {
            // Validation & Input Errors
            ErrorType.Validation or
            ErrorType.BadRequest or
            ErrorType.PayloadTooLarge or
            ErrorType.UriTooLong or
            ErrorType.UnsupportedMediaType or
            ErrorType.RangeNotSatisfiable or
            ErrorType.ExpectationFailed or
            ErrorType.RequestHeadersTooLarge => ValidationCategory,

            // Business Logic Errors
            ErrorType.NotFound or
            ErrorType.Conflict or
            ErrorType.MethodNotAllowed or
            ErrorType.NotAcceptable or
            ErrorType.PreconditionFailed or
            ErrorType.PreconditionRequired or
            ErrorType.UnavailableForLegal => BusinessCategory,

            // Security Errors
            ErrorType.Unauthorized or
            ErrorType.Forbidden or
            ErrorType.NetworkAuthRequired => SecurityCategory,

            // Performance Errors
            ErrorType.RateLimited or
            ErrorType.RequestTimeout or
            ErrorType.Timeout => PerformanceCategory,

            // Integration Errors
            ErrorType.External => IntegrationCategory,

            // System Errors
            _ => SystemCategory
        };

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> severity string.
    /// Prefer this over <see cref="ToSeverityString"/> in hot paths to avoid string allocation.
    /// </summary>
    public static ReadOnlySpan<char> ToSeveritySpan(this ErrorType errorType) =>
        errorType switch
        {
            ErrorType.Validation => WarningSeverity,
            _ => ErrorSeverity
        };
}

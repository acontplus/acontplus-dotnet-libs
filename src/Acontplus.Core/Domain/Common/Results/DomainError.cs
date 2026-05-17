namespace Acontplus.Core.Domain.Common.Results;

/// <summary>Represents a domain error with type, code, message, and optional details.</summary>
/// <param name="Type">The error type categorizing the error.</param>
/// <param name="Code">A machine-readable error code.</param>
/// <param name="Message">A human-readable error message.</param>
/// <param name="Target">Optional field that caused the error.</param>
/// <param name="Details">Optional additional error details.</param>
public readonly record struct DomainError(
    ErrorType Type,
    string Code,
    string Message,
    string? Target = null,
    Dictionary<string, object>? Details = null)
{
    #region Client Errors (4xx)

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.BadRequest"/> (HTTP 400).</summary>
    public static DomainError BadRequest(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.BadRequest, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Unauthorized"/> (HTTP 401).</summary>
    public static DomainError Unauthorized(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Unauthorized, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Forbidden"/> (HTTP 403).</summary>
    public static DomainError Forbidden(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Forbidden, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.NotFound"/> (HTTP 404).</summary>
    public static DomainError NotFound(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.NotFound, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.MethodNotAllowed"/> (HTTP 405).</summary>
    public static DomainError MethodNotAllowed(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.MethodNotAllowed, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.NotAcceptable"/> (HTTP 406).</summary>
    public static DomainError NotAcceptable(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.NotAcceptable, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Conflict"/> (HTTP 409).</summary>
    public static DomainError Conflict(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Conflict, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Validation"/> (HTTP 400).</summary>
    public static DomainError Validation(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Validation, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.PayloadTooLarge"/> (HTTP 413).</summary>
    public static DomainError PayloadTooLarge(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.PayloadTooLarge, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.UriTooLong"/> (HTTP 414).</summary>
    public static DomainError UriTooLong(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.UriTooLong, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.UnsupportedMediaType"/> (HTTP 415).</summary>
    public static DomainError UnsupportedMediaType(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.UnsupportedMediaType, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.RangeNotSatisfiable"/> (HTTP 416).</summary>
    public static DomainError RangeNotSatisfiable(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.RangeNotSatisfiable, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.ExpectationFailed"/> (HTTP 417).</summary>
    public static DomainError ExpectationFailed(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.ExpectationFailed, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.PreconditionFailed"/> (HTTP 412).</summary>
    public static DomainError PreconditionFailed(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.PreconditionFailed, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.PreconditionRequired"/> (HTTP 428).</summary>
    public static DomainError PreconditionRequired(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.PreconditionRequired, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.RequestHeadersTooLarge"/> (HTTP 431).</summary>
    public static DomainError RequestHeadersTooLarge(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.RequestHeadersTooLarge, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.UnavailableForLegal"/> (HTTP 451).</summary>
    public static DomainError UnavailableForLegal(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.UnavailableForLegal, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.RateLimited"/> (HTTP 429).</summary>
    public static DomainError RateLimited(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.RateLimited, code, message, target, details);

    #endregion

    #region Server Errors (5xx)

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Internal"/> (HTTP 500).</summary>
    public static DomainError Internal(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Internal, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.NotImplemented"/> (HTTP 501).</summary>
    public static DomainError NotImplemented(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.NotImplemented, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.External"/> (HTTP 502).</summary>
    public static DomainError External(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.External, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.ServiceUnavailable"/> (HTTP 503).</summary>
    public static DomainError ServiceUnavailable(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.ServiceUnavailable, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.Timeout"/> (HTTP 504).</summary>
    public static DomainError Timeout(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.Timeout, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.RequestTimeout"/> (HTTP 408).</summary>
    public static DomainError RequestTimeout(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.RequestTimeout, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.HttpVersionNotSupported"/> (HTTP 505).</summary>
    public static DomainError HttpVersionNotSupported(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.HttpVersionNotSupported, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.InsufficientStorage"/> (HTTP 507).</summary>
    public static DomainError InsufficientStorage(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.InsufficientStorage, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.LoopDetected"/> (HTTP 508).</summary>
    public static DomainError LoopDetected(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.LoopDetected, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.NotExtended"/> (HTTP 510).</summary>
    public static DomainError NotExtended(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.NotExtended, code, message, target, details);

    /// <summary>Creates a <see cref="DomainError"/> for <see cref="ErrorType.NetworkAuthRequired"/> (HTTP 511).</summary>
    public static DomainError NetworkAuthRequired(
        string code, string message,
        string? target = null,
        Dictionary<string, object>? details = null) =>
        new(ErrorType.NetworkAuthRequired, code, message, target, details);

    #endregion

    #region Conversion Methods

    /// <summary>Converts this <see cref="DomainError"/> to an <see cref="ApiError"/> for API responses.</summary>
    /// <returns>An <see cref="ApiError"/> containing the error information with severity, category, and trace ID.</returns>
    public ApiError ToApiError() => new(
        Code: Code,
        Message: Message,
        Target: Target,
        Details: Details,
        Severity: Type.ToSeverityString(),
        Category: Type.ToCategoryString(),
        TraceId: Activity.Current?.Id
    );

    /// <summary>Gets the HTTP status code corresponding to this error's type.</summary>
    /// <returns>The <see cref="HttpStatusCode"/> that represents this error type.</returns>
    public HttpStatusCode GetHttpStatusCode() => Type.ToHttpStatusCode();
    #endregion
}

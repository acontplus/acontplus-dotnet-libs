namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// Base exception for HTTP-aware domain errors.
/// Carry an HTTP status code and an application-level error code so that
/// middleware can translate them into consistent API error responses.
/// </summary>
/// <param name="statusCode">The HTTP status code.</param>
/// <param name="errorCode">Application-level error code.</param>
/// <param name="message">The exception message.</param>
public abstract class ApiException(
    HttpStatusCode statusCode,
    string errorCode,
    string message) : Exception(message)
{
    /// <summary>The HTTP status code that should be returned to the client.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Application-level error code (e.g., <c>"NOT_FOUND"</c>).</summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
/// <param name="resourceName">The resource name.</param>
/// <param name="key">The resource key.</param>
public class NotFoundException(string resourceName, object key)
    : ApiException(HttpStatusCode.NotFound,
          "NOT_FOUND",
          $"Resource '{resourceName}' with key '{key}' was not found");

/// <summary>
/// Thrown when an operation would produce a state conflict.
/// Maps to HTTP 409 Conflict.
/// </summary>
/// <param name="resourceName">The resource name.</param>
/// <param name="conflictDetail">Details of the conflict.</param>
public class ConflictException(string resourceName, string conflictDetail)
    : ApiException(HttpStatusCode.Conflict,
          "CONFLICT",
          $"Conflict occurred with resource '{resourceName}': {conflictDetail}");

/// <summary>
/// Thrown when one or more input values fail validation rules.
/// Maps to HTTP 400 Bad Request.
/// </summary>
/// <param name="errors">Field-level validation errors keyed by property name.</param>
public class ValidationException(IDictionary<string, string[]> errors)
    : ApiException(HttpStatusCode.BadRequest,
          "VALIDATION_FAILED",
          "One or more validation errors occurred")
{
    /// <summary>Field-level validation errors keyed by property name.</summary>
    public IDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>
/// Thrown when the caller is not authenticated.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
/// <param name="message">Human-readable message.</param>
public class UnauthorizedException(string message)
    : ApiException(HttpStatusCode.Unauthorized,
          "UNAUTHORIZED",
          message);

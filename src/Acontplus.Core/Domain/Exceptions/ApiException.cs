namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// Base exception for HTTP-aware domain errors.
/// Carry an HTTP status code and an application-level error code so that
/// middleware can translate them into consistent API error responses.
/// </summary>
public abstract class ApiException : Exception
{
    /// <summary>The HTTP status code that should be returned to the client.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Application-level error code (e.g., <c>"NOT_FOUND"</c>).</summary>
    public string ErrorCode { get; }

    /// <summary>Initialises a new <see cref="ApiException"/>.</summary>
    protected ApiException(
        HttpStatusCode statusCode,
        string errorCode,
        string message) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : ApiException
{
    /// <summary>Creates a <see cref="NotFoundException"/> for a named resource and its key.</summary>
    public NotFoundException(string resourceName, object key)
        : base(HttpStatusCode.NotFound,
              "NOT_FOUND",
              $"Resource '{resourceName}' with key '{key}' was not found")
    {
    }
}

/// <summary>
/// Thrown when an operation would produce a state conflict.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : ApiException
{
    /// <summary>Creates a <see cref="ConflictException"/> for a named resource.</summary>
    public ConflictException(string resourceName, string conflictDetail)
        : base(HttpStatusCode.Conflict,
              "CONFLICT",
              $"Conflict occurred with resource '{resourceName}': {conflictDetail}")
    {
    }
}

/// <summary>
/// Thrown when one or more input values fail validation rules.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : ApiException
{
    /// <summary>Field-level validation errors keyed by property name.</summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>Creates a <see cref="ValidationException"/> with a dictionary of field errors.</summary>
    public ValidationException(IDictionary<string, string[]> errors)
        : base(HttpStatusCode.BadRequest,
              "VALIDATION_FAILED",
              "One or more validation errors occurred")
    {
        Errors = errors;
    }
}

/// <summary>
/// Thrown when the caller is not authenticated.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : ApiException
{
    /// <summary>Creates an <see cref="UnauthorizedException"/> with a human-readable message.</summary>
    public UnauthorizedException(string message)
        : base(HttpStatusCode.Unauthorized,
              "UNAUTHORIZED",
              message)
    {
    }
}

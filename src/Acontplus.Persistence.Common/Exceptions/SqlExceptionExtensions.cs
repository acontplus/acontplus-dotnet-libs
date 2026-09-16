namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Provides extension methods for converting SQL domain exceptions to domain error structures.
/// </summary>
public static class SqlExceptionExtensions
{
    private static readonly
        ImmutableDictionary<ErrorType, Func<string, string, string?, Dictionary<string, object>?, DomainError>>
        ErrorMappers =
            new Dictionary<ErrorType, Func<string, string, string?, Dictionary<string, object>?, DomainError>>
            {
                // Client Errors (4xx)
                [ErrorType.Validation] = DomainError.Validation,
                [ErrorType.BadRequest] = DomainError.BadRequest,
                [ErrorType.NotFound] = DomainError.NotFound,
                [ErrorType.Conflict] = DomainError.Conflict,
                [ErrorType.Unauthorized] = DomainError.Unauthorized,
                [ErrorType.Forbidden] = DomainError.Forbidden,
                [ErrorType.MethodNotAllowed] = (code, msg, target, details) =>
                    new DomainError(ErrorType.MethodNotAllowed, code, msg, target, details),
                [ErrorType.NotAcceptable] = (code, msg, target, details) =>
                    new DomainError(ErrorType.NotAcceptable, code, msg, target, details),
                [ErrorType.PayloadTooLarge] = (code, msg, target, details) =>
                    new DomainError(ErrorType.PayloadTooLarge, code, msg, target, details),
                [ErrorType.RateLimited] = DomainError.RateLimited,

                // Server Errors (5xx)
                [ErrorType.Internal] = DomainError.Internal,
                [ErrorType.External] = DomainError.External,
                [ErrorType.ServiceUnavailable] = DomainError.ServiceUnavailable,
                [ErrorType.Timeout] = DomainError.Timeout,
                [ErrorType.RequestTimeout] = (code, msg, target, details) =>
                    new DomainError(ErrorType.RequestTimeout, code, msg, target, details)
            }.ToImmutableDictionary();

    /// <summary>
    /// Converts a <see cref="SqlDomainException"/> into a strongly typed <see cref="DomainError"/>.
    /// </summary>
    /// <param name="sqlDomainException">The SQL domain exception to convert.</param>
    /// <param name="target">Optional target property or field associated with the error.</param>
    /// <param name="details">Optional additional error metadata dictionary.</param>
    /// <returns>A mapped <see cref="DomainError"/> instance.</returns>
    public static DomainError ToDomainError(
        this SqlDomainException sqlDomainException,
        string? target = null,
        Dictionary<string, object>? details = null)
    {
        if (ErrorMappers.TryGetValue(sqlDomainException.ErrorType, out var mapper))
        {
            return mapper(sqlDomainException.ErrorCode, sqlDomainException.Message, target, details);
        }

        // Default mapping for unmapped error types
        return sqlDomainException.ErrorType switch
        {
            // Additional client error mappings
            ErrorType.UriTooLong => new DomainError(ErrorType.UriTooLong, sqlDomainException.ErrorCode,
                sqlDomainException.Message, target, details),
            ErrorType.UnsupportedMediaType => new DomainError(ErrorType.UnsupportedMediaType,
                sqlDomainException.ErrorCode, sqlDomainException.Message,
                target, details),
            ErrorType.PreconditionFailed => new DomainError(ErrorType.PreconditionFailed, sqlDomainException.ErrorCode,
                sqlDomainException.Message,
                target, details),

            // Additional server error mappings
            ErrorType.NotImplemented => new DomainError(ErrorType.NotImplemented, sqlDomainException.ErrorCode,
                sqlDomainException.Message, target,
                details),
            ErrorType.HttpVersionNotSupported => new DomainError(ErrorType.HttpVersionNotSupported,
                sqlDomainException.ErrorCode,
                sqlDomainException.Message, target, details),

            _ => DomainError.Internal(
                $"SQL_{sqlDomainException.ErrorCode}",
                $"Unmapped SQL error: {sqlDomainException.Message}",
                target,
                details?.Union(new Dictionary<string, object>
                {
                    ["originalErrorType"] = sqlDomainException.ErrorType.ToString(),
                    ["sqlErrorCode"] = sqlDomainException.ErrorCode
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
        };
    }

    /// <summary>
    ///     Converts multiple SqlDomainExceptions to a DomainErrors collection
    /// </summary>
    /// <param name="exceptions">Collection of SQL domain exceptions</param>
    /// <param name="target">Optional target of the errors</param>
    /// <param name="commonDetails">Optional common details for all errors</param>
    /// <returns>DomainErrors containing all converted exceptions</returns>
    public static DomainErrors ToDomainErrors(
        this IEnumerable<SqlDomainException> exceptions,
        string? target = null,
        Dictionary<string, object>? commonDetails = null)
    {
        var domainErrors = exceptions
            .Select(ex => ex.ToDomainError(target, commonDetails))
            .ToList();

        return domainErrors.Count switch
        {
            0 => throw new ArgumentException("No exceptions provided", nameof(exceptions)),
            1 => DomainErrors.FromSingle(domainErrors[0]),
            _ => DomainErrors.Multiple(domainErrors)
        };
    }
}

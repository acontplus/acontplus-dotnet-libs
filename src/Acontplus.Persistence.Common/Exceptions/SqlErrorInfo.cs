namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Encapsulates details about a SQL error for conversion to domain exceptions.
/// </summary>
/// <param name="ErrorType">The mapped domain error type.</param>
/// <param name="Code">The SQL error code.</param>
/// <param name="Message">The human-readable error message.</param>
/// <param name="Exception">Optional underlying SQL exception.</param>
public record SqlErrorInfo(
    ErrorType ErrorType,
    string Code,
    string Message,
    Exception? Exception = null)
{
    /// <summary>
    /// Gets the stack trace from the underlying exception, if available.
    /// </summary>
    public string? StackTrace => Exception?.StackTrace;
}

namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions in the application.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the type of error that occurred.
    /// </summary>
    public ErrorType ErrorType { get; }
    
    /// <summary>
    /// Gets the specific error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="type">The type of error.</param>
    /// <param name="code">The specific error code.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
    public DomainException(ErrorType type, string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        ErrorType = type;
        ErrorCode = code;
    }
}

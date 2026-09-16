namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Represents an exception that occurred within a repository operation.
/// </summary>
public class RepositoryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RepositoryException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="exception">The inner exception that caused this exception.</param>
    public RepositoryException(string message, Exception exception)
        : base(message, exception)
    {
    }
}

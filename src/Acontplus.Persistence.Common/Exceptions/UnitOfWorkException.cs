namespace Acontplus.Persistence.Common.Exceptions;

/// <summary>
/// Exception thrown when an error occurs within UnitOfWork operations.
/// </summary>
public class UnitOfWorkException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkException" /> class.
    /// </summary>
    public UnitOfWorkException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnitOfWorkException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkException" /> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public UnitOfWorkException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

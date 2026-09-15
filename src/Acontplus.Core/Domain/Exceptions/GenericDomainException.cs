namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// General-purpose domain exception for cases that don't warrant a dedicated exception subclass.
/// Prefer creating a named subclass when the error has semantic meaning in the domain.
/// </summary>
/// <param name="type">The type of error.</param>
/// <param name="code">The specific error code.</param>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="inner">The exception that is the cause of the current exception, or null if no inner exception is specified.</param>
public class GenericDomainException(ErrorType type, string code, string message, Exception? inner = null)
    : DomainException(type, code, message, inner);

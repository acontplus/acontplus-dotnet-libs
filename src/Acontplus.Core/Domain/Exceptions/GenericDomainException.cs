namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// General-purpose domain exception for cases that don't warrant a dedicated exception subclass.
/// Prefer creating a named subclass when the error has semantic meaning in the domain.
/// </summary>
public class GenericDomainException : DomainException
{
    /// <inheritdoc />
    public GenericDomainException(ErrorType type, string code, string message, Exception? inner = null)
        : base(type, code, message, inner)
    {
    }
}

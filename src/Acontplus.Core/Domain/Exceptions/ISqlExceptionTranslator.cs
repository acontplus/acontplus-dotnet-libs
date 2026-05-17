namespace Acontplus.Core.Domain.Exceptions;

/// <summary>
/// Translates raw <see cref="Exception"/> objects thrown by a database driver into
/// well-typed <see cref="DomainException"/> instances.
/// Implement this interface in each persistence package (SqlServer, PostgreSQL, etc.)
/// and register it with the DI container.
/// </summary>
public interface ISqlExceptionTranslator
{
    /// <summary>Returns <c>true</c> when the exception is transient and the operation may be retried.</summary>
    bool IsTransient(Exception ex);

    /// <summary>Translates a raw database exception into a <see cref="DomainException"/>.</summary>
    DomainException Translate(Exception ex);
}

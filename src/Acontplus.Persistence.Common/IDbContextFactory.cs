namespace Acontplus.Persistence.Common;

/// <summary>
/// Defines a factory for retrieving named <see cref="DbContext"/> instances.
/// </summary>
/// <typeparam name="TContext">The database context type.</typeparam>
public interface IDbContextFactory<out TContext> where TContext : DbContext
{
    /// <summary>
    /// Retrieves a database context instance registered under the given name.
    /// </summary>
    /// <param name="contextName">The unique name of the database context.</param>
    /// <returns>The resolved <typeparamref name="TContext"/> instance.</returns>
    TContext GetContext(string contextName);
}

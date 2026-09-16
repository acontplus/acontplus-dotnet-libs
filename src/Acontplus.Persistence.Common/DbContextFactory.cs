namespace Acontplus.Persistence.Common;

/// <summary>
/// Default implementation of <see cref="IDbContextFactory{TContext}"/> storing contexts in a concurrent dictionary.
/// </summary>
/// <typeparam name="TContext">The database context type.</typeparam>
/// <param name="contexts">The named dictionary of context instances.</param>
public class DbContextFactory<TContext>(IDictionary<string, TContext> contexts) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ConcurrentDictionary<string, TContext> _contexts = new(contexts);

    /// <inheritdoc />
    public TContext GetContext(string contextName)
    {
        return _contexts.TryGetValue(contextName, out var context)
            ? context
            : throw new KeyNotFoundException($"DbContext with name '{contextName}' not found.");
    }
}

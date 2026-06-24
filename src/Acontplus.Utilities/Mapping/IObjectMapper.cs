namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Abstraction for compiled-delegate object mapping.
/// Register via <c>services.AddObjectMapper(...)</c> and inject as a singleton.
/// </summary>
public interface IObjectMapper
{
    /// <summary>Maps <paramref name="source"/> to a new <typeparamref name="TTarget"/> instance.</summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TTarget">The target type to map to.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <returns>A new instance of <typeparamref name="TTarget"/> populated from <paramref name="source"/>.</returns>
    TTarget Map<TSource, TTarget>(TSource source);

    /// <summary>Maps <paramref name="source"/> onto an existing <paramref name="destination"/>.</summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TTarget">The target type to map to.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <param name="destination">The existing target instance to map onto.</param>
    /// <returns>The <paramref name="destination"/> instance with mapped values applied.</returns>
    TTarget Map<TSource, TTarget>(TSource source, TTarget destination);

    /// <summary>Maps each element of <paramref name="source"/> to <typeparamref name="TTarget"/>.</summary>
    /// <typeparam name="TSource">The source element type to map from.</typeparam>
    /// <typeparam name="TTarget">The target element type to map to.</typeparam>
    /// <param name="source">The source collection to map.</param>
    /// <returns>An enumerable of mapped <typeparamref name="TTarget"/> instances.</returns>
    IEnumerable<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source);

    /// <summary>
    /// Projects a queryable source sequence to <typeparamref name="TTarget"/> using a pure
    /// <see cref="Expression"/> tree so that LINQ query providers (e.g., EF Core) can translate
    /// the projection to SQL without materialising source objects.
    /// </summary>
    /// <typeparam name="TSource">The source element type to project from.</typeparam>
    /// <typeparam name="TTarget">The target element type to project to.</typeparam>
    /// <param name="source">The queryable source sequence to project.</param>
    /// <returns>An <see cref="IQueryable{T}"/> of projected <typeparamref name="TTarget"/> instances.</returns>
    IQueryable<TTarget> ProjectTo<TSource, TTarget>(IQueryable<TSource> source);
}

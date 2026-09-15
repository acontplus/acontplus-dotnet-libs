namespace Acontplus.Persistence.PostgreSQL.Mapping;

/// <summary>
/// Provides extension methods for mapping <see cref="DbDataReader"/> results to strongly-typed objects.
/// </summary>
public static class DbDataReaderMapper
{
    /// <summary>
    /// Maps a DbDataReader to a List of entities of type T with support for records and init-only properties.
    /// </summary>
    /// <typeparam name="T">The entity type to map to.</typeparam>
    /// <param name="reader">The data reader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of mapped entities.</returns>
    public static Task<List<T>> ToListAsync<T>(this DbDataReader reader, CancellationToken cancellationToken = default) =>
        Common.Mapping.DbDataReaderMapper.ToListAsync<T>(reader, cancellationToken);

    /// <summary>
    /// Synchronously maps all rows from the data reader to a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to map each row to.</typeparam>
    /// <param name="reader">The data reader positioned before the first row.</param>
    /// <returns>A list of mapped <typeparamref name="T"/> instances.</returns>
    public static List<T> ToList<T>(this DbDataReader reader) =>
        Common.Mapping.DbDataReaderMapper.ToList<T>(reader);

    /// <summary>
    /// Maps a single row from a DbDataReader to an object of type T using reflection.
    /// </summary>
    /// <typeparam name="T">The target class type.</typeparam>
    /// <param name="reader">The data reader.</param>
    /// <returns>A task that resolves to the mapped object or null.</returns>
    public static Task<T?> MapToObject<T>(DbDataReader reader) where T : class =>
        Common.Mapping.DbDataReaderMapper.MapToObject<T>(reader);
}

namespace Acontplus.Core.Dtos.Requests;

/// <summary>
/// Request model for filtering, sorting, and searching without pagination.
/// Ideal for reports, exports, simple searches, and scenarios where all matching results are needed.
/// </summary>
public record FilterRequest
{
    /// <summary>
    /// Property name to sort by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Direction of sorting (Ascending or Descending).
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;

    /// <summary>
    /// General search term to filter results.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Dictionary of additional filters with dynamic key-value pairs.
    /// Keys represent filter field names, values represent filter values.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Filters { get; init; }

    /// <summary>
    /// Indicates whether the filter is empty (no search term and no filters).
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(SearchTerm) &&
                          (Filters is null || !Filters.Any());

    /// <summary>
    /// Indicates whether the filter has any criteria (search term or filters).
    /// </summary>
    public bool HasCriteria => !IsEmpty;

    /// <summary>
    /// Builds a dictionary of filters if they exist, otherwise returns null.
    /// Useful for converting to stored procedure parameters or other filter formats.
    /// </summary>
    /// <returns>Dictionary of filters or null if no filters exist</returns>
    public Dictionary<string, object>? BuildFilters()
    {
        return Filters == null || !Filters.Any() ? null : Filters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Builds a dictionary of filters with custom key prefix (e.g., "@" for SQL parameters).
    /// </summary>
    /// <param name="prefix">Prefix to add to filter keys</param>
    /// <returns>Dictionary of filters with prefixed keys or null if no filters exist</returns>
    public Dictionary<string, object>? BuildFiltersWithPrefix(string prefix)
    {
        return Filters == null || !Filters.Any() ? null : Filters.ToDictionary(kvp => prefix + kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Builds a dictionary of filters with "@" prefix for SQL parameters.
    /// </summary>
    /// <returns>Dictionary of filters with "@" prefix or null if no filters exist</returns>
    public Dictionary<string, object>? BuildSqlParameters()
    {
        return BuildFiltersWithPrefix("@");
    }
}

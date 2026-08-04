using System.Reflection;

namespace Acontplus.Utilities.Dtos;

/// <summary>
/// Represents a filter query with sorting and search capabilities for non-paginated results.
/// Implements the minimal API binding pattern for automatic parameter binding.
/// Ideal for reports, exports, and scenarios where all matching results are needed.
/// </summary>
public record FilterQuery(
    string? SortBy = null,
    SortDirection? SortDirection = null,
    string? SearchTerm = null,
    IReadOnlyDictionary<string, object>? Filters = null
)
{
    /// <summary>
    /// Gets whether the query has no search criteria.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(SearchTerm) &&
                          (Filters is null || !Filters.Any());

    /// <summary>
    /// Gets whether the query has any criteria (search term or filters).
    /// </summary>
    public bool HasCriteria => !IsEmpty;

    /// <summary>
    /// Binds query parameters from the HTTP context to create a FilterQuery instance.
    /// This method enables automatic parameter binding in minimal APIs.
    /// </summary>
    /// <param name="context">The HTTP context containing query parameters</param>
    /// <param name="parameter">The parameter information (required by the binding contract)</param>
    /// <returns>A ValueTask containing the bound FilterQuery or null</returns>
    public static ValueTask<FilterQuery?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        const string sortByKey = "sortBy";
        const string sortDirectionKey = "sortDirection";
        const string searchTermKey = "searchTerm";
        const string filtersKey = "filters";

        // Parse sort direction with enum parsing
        Enum.TryParse<SortDirection>(context.Request.Query[sortDirectionKey],
                                   ignoreCase: true, out var sortDirection);

        // Parse filters from query parameters
        var filters = new Dictionary<string, object>();
        foreach (var queryParam in context.Request.Query)
        {
            if (queryParam.Key.StartsWith(filtersKey + "["))
            {
                var filterName = queryParam.Key.Substring(
                    filtersKey.Length + 1,
                    queryParam.Key.Length - filtersKey.Length - 2);
                filters[filterName] = queryParam.Value.ToString();
            }
        }

        var result = new FilterQuery
        {
            SortBy = context.Request.Query[sortByKey],
            SortDirection = sortDirection,
            SearchTerm = context.Request.Query[searchTermKey],
            Filters = filters.Any() ? filters : null
        };

        return ValueTask.FromResult<FilterQuery?>(result);
    }
}

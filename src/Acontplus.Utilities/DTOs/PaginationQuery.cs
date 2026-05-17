using System.Reflection;

namespace Acontplus.Utilities.Dtos;

/// <summary>
/// Represents a pagination query with sorting and search capabilities.
/// Implements the minimal API binding pattern for automatic parameter binding.
/// </summary>
public sealed record PaginationQuery(
    int PageIndex = 1,
    int PageSize = 10
) : FilterQuery()
{
    /// <summary>
    /// Gets the number of items to skip for database queries.
    /// </summary>
    public int Skip => (PageIndex - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take for database queries.
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Binds query parameters from the HTTP context to create a PaginationQuery instance.
    /// This method enables automatic parameter binding in minimal APIs.
    /// </summary>
    /// <param name="context">The HTTP context containing query parameters</param>
    /// <param name="parameter">The parameter information (required by the binding contract)</param>
    /// <returns>A ValueTask containing the bound PaginationQuery or null</returns>
    public static new ValueTask<PaginationQuery?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        const string pageIndexKey = "pageIndex";
        const string pageSizeKey = "pageSize";
        const string sortByKey = "sortBy";
        const string sortDirectionKey = "sortDirection";
        const string searchTermKey = "searchTerm";
        const string filtersKey = "filters";

        // Parse page index with fallback to 1
        int.TryParse(context.Request.Query[pageIndexKey], out var pageIndex);
        pageIndex = pageIndex == 0 ? 1 : pageIndex;

        // Parse page size with fallback to 10 and max limit of 1000
        int.TryParse(context.Request.Query[pageSizeKey], out var pageSize);
        pageSize = pageSize switch
        {
            < 1 => 10,
            > 1000 => 1000,
            _ => pageSize
        };

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

        var result = new PaginationQuery
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            SortBy = context.Request.Query[sortByKey],
            SortDirection = sortDirection,
            SearchTerm = context.Request.Query[searchTermKey],
            Filters = filters.Any() ? filters : null
        };

        return ValueTask.FromResult<PaginationQuery?>(result);
    }
}

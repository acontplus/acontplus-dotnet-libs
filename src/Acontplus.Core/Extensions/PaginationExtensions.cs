namespace Acontplus.Core.Extensions;

/// <summary>
/// Extension methods for PaginationRequest to provide fluent API and helper functionality.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Adds a search term to the pagination request.
    /// </summary>
    /// <param name="pagination">The pagination object</param>
    /// <param name="searchTerm">The search term to add</param>
    /// <returns>A new PaginationRequest with the search term</returns>
    public static PaginationRequest WithSearch(this PaginationRequest pagination, string searchTerm)
        => pagination with { SearchTerm = searchTerm };

    /// <summary>
    /// Adds sorting criteria to the pagination request.
    /// </summary>
    /// <param name="pagination">The pagination object</param>
    /// <param name="sortBy">The field to sort by</param>
    /// <param name="direction">The sort direction</param>
    /// <returns>A new PaginationRequest with sorting criteria</returns>
    public static PaginationRequest WithSort(this PaginationRequest pagination, string sortBy, SortDirection direction = SortDirection.Asc)
        => pagination with { SortBy = sortBy, SortDirection = direction };

    /// <summary>
    /// Adds filters to the pagination request.
    /// </summary>
    /// <param name="pagination">The pagination object</param>
    /// <param name="filters">The filters to add</param>
    /// <returns>A new PaginationRequest with filters</returns>
    public static PaginationRequest WithFilters(this PaginationRequest pagination, IReadOnlyDictionary<string, object> filters)
        => pagination with { Filters = filters };

    /// <summary>
    /// Adds a single filter to the pagination request.
    /// </summary>
    /// <param name="pagination">The pagination object</param>
    /// <param name="key">The filter key</param>
    /// <param name="value">The filter value</param>
    /// <returns>A new PaginationRequest with the additional filter</returns>
    public static PaginationRequest WithFilter(this PaginationRequest pagination, string key, object value)
    {
        var existingFilters = pagination.Filters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? [];
        existingFilters[key] = value;
        return pagination with { Filters = existingFilters };
    }

    /// <summary>
    /// Creates a new PaginationRequest with the specified page and size.
    /// </summary>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A new PaginationRequest</returns>
    public static PaginationRequest Create(int pageIndex = 1, int pageSize = 10)
        => new() { PageIndex = pageIndex, PageSize = pageSize };

    /// <summary>
    /// Creates a new PaginationRequest with search term.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A new PaginationRequest with search</returns>
    public static PaginationRequest CreateWithSearch(string searchTerm, int pageIndex = 1, int pageSize = 10)
        => Create(pageIndex, pageSize).WithSearch(searchTerm);

    /// <summary>
    /// Creates a new PaginationRequest with sorting.
    /// </summary>
    /// <param name="sortBy">The field to sort by</param>
    /// <param name="direction">The sort direction</param>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A new PaginationRequest with sorting</returns>
    public static PaginationRequest CreateWithSort(string sortBy, SortDirection direction = SortDirection.Asc, int pageIndex = 1, int pageSize = 10)
        => Create(pageIndex, pageSize).WithSort(sortBy, direction);

    /// <summary>
    /// Creates a new PaginationRequest with filters.
    /// </summary>
    /// <param name="filters">The filters</param>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>A new PaginationRequest with filters</returns>
    public static PaginationRequest CreateWithFilters(IReadOnlyDictionary<string, object> filters, int pageIndex = 1, int pageSize = 10)
        => Create(pageIndex, pageSize).WithFilters(filters);

    /// <summary>
    /// Validates the pagination parameters and returns a corrected version if needed.
    /// </summary>
    /// <param name="pagination">The pagination object to validate</param>
    /// <returns>A validated and corrected PaginationRequest</returns>
    public static PaginationRequest Validate(this PaginationRequest pagination)
    {
        var pageIndex = pagination.PageIndex < 1 ? 1 : pagination.PageIndex;
        var pageSize = pagination.PageSize switch
        {
            < 1 => 10,
            > 1000 => 1000,
            _ => pagination.PageSize
        };

        return pagination with { PageIndex = pageIndex, PageSize = pageSize };
    }

    /// <summary>
    /// Gets the next page pagination object.
    /// </summary>
    /// <param name="pagination">The current pagination object</param>
    /// <returns>A new PaginationRequest for the next page</returns>
    public static PaginationRequest NextPage(this PaginationRequest pagination)
        => pagination with { PageIndex = pagination.PageIndex + 1 };

    /// <summary>
    /// Gets the previous page pagination object.
    /// </summary>
    /// <param name="pagination">The current pagination object</param>
    /// <returns>A new PaginationRequest for the previous page</returns>
    public static PaginationRequest PreviousPage(this PaginationRequest pagination)
        => pagination with { PageIndex = Math.Max(1, pagination.PageIndex - 1) };

    /// <summary>
    /// Gets a pagination object for a specific page.
    /// </summary>
    /// <param name="pagination">The current pagination object</param>
    /// <param name="pageIndex">The target page index</param>
    /// <returns>A new PaginationRequest for the specified page</returns>
    public static PaginationRequest ToPage(this PaginationRequest pagination, int pageIndex)
        => pagination with { PageIndex = Math.Max(1, pageIndex) };
}

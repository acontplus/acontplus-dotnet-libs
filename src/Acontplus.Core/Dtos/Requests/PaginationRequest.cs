namespace Acontplus.Core.Dtos.Requests;

/// <summary>
/// Request model for paginated queries with filtering, sorting, and searching capabilities.
/// Extends FilterRequest with pagination properties.
/// </summary>
public record PaginationRequest : FilterRequest
{
    private int _pageIndex = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Current page number (1-based index).
    /// </summary>
    public int PageIndex
    {
        get => _pageIndex;
        init => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page (max 1000).
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => 10,
            > 1000 => 1000, // Increased limit for large systems
            _ => value
        };
    }

    /// <summary>
    /// Number of items to skip in the query (calculated from PageIndex and PageSize).
    /// </summary>
    public int Skip => (PageIndex - 1) * PageSize;

    /// <summary>
    /// Number of items to take in the query (same as PageSize).
    /// </summary>
    public int Take => PageSize;
}

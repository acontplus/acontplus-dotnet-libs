namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Represents a paged result set returned from a query.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>The items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>The current page number (1-based).</summary>
    public int PageIndex { get; init; }

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>The total number of items across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Whether there is a page before this one.</summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>Whether there is a page after this one.</summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>Optional metadata attached to the result.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();

    // Parameterless constructor required for deserialization
    /// <summary>Parameterless constructor required for JSON serialization/deserialization.</summary>
    public PagedResult() { }

    /// <summary>Creates a paged result without metadata.</summary>
    public PagedResult(IEnumerable<T> items, int pageIndex, int pageSize, int totalCount)
        : this(items, pageIndex, pageSize, totalCount, null) { }

    /// <summary>Creates a paged result with optional metadata.</summary>
    public PagedResult(
        IEnumerable<T> items,
        int pageIndex,
        int pageSize,
        int totalCount,
        IReadOnlyDictionary<string, object>? metadata)
    {
        Items = (items ?? []).ToList();
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
        Metadata = metadata ?? new Dictionary<string, object>();
    }
}

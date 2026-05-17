namespace Acontplus.Core.Constants;

/// <summary>
/// Metadata keys specifically for pagination-related information in PagedResult.
/// Use these constants to ensure consistency across all paged query implementations.
/// </summary>
public static class PaginationMetadataKeys
{
    // Query Information
    
    /// <summary>
    /// Indicates whether any filters are applied to the query.
    /// </summary>
    public const string HasFilters = "hasFilters";
    
    /// <summary>
    /// Indicates whether a search term is applied to the query.
    /// </summary>
    public const string HasSearch = "hasSearch";
    
    /// <summary>
    /// The search term used in the query.
    /// </summary>
    public const string SearchTerm = "searchTerm";

    // Sorting Information
    
    /// <summary>
    /// The field or property name used for sorting.
    /// </summary>
    public const string SortBy = "sortBy";
    
    /// <summary>
    /// The direction of sorting (ascending or descending).
    /// </summary>
    public const string SortDirection = "sortDirection";
    
    /// <summary>
    /// Indicates whether the default sort is being used.
    /// </summary>
    public const string DefaultSort = "defaultSort";

    // Query Source (⚠️ Use with caution - may expose implementation details)
    // Recommended: Only include in Development/Staging environments, not Production
    
    /// <summary>
    /// The source type of the query (e.g., stored procedure, view). Use with caution as it may expose implementation details.
    /// Recommended: Only include in Development/Staging environments, not Production.
    /// </summary>
    public const string QuerySource = "querySource";

    // Query Source Values (internal use - avoid exposing in production metadata)
    internal const string QuerySourceStoredProcedure = "storedProcedure";
    internal const string QuerySourceRawQuery = "rawQuery";
    internal const string QuerySourceView = "view";
    internal const string QuerySourceFunction = "function";

    // Performance Metrics
    
    /// <summary>
    /// The duration of the query execution in milliseconds.
    /// </summary>
    public const string QueryDuration = "queryDurationMs";
    
    /// <summary>
    /// The duration of the count query execution in milliseconds.
    /// </summary>
    public const string CountDuration = "countDurationMs";
    
    /// <summary>
    /// The total duration of the operation in milliseconds.
    /// </summary>
    public const string TotalDuration = "totalDurationMs";

    // Filter Details (optional, for debugging/auditing)
    
    /// <summary>
    /// The number of filters applied to the query.
    /// </summary>
    public const string FilterCount = "filterCount";
    
    /// <summary>
    /// Details of the filters applied to the query.
    /// </summary>
    public const string AppliedFilters = "appliedFilters";

    // Data Quality
    
    /// <summary>
    /// Indicates whether the result set is partial or incomplete.
    /// </summary>
    public const string IsPartialResult = "isPartialResult";
    
    /// <summary>
    /// Indicates the quality level of the returned results.
    /// </summary>
    public const string ResultQuality = "resultQuality";

    // Cache Information (for future use)
    
    /// <summary>
    /// Indicates whether the result was retrieved from cache.
    /// </summary>
    public const string FromCache = "fromCache";
    
    /// <summary>
    /// The cache expiry time for the result.
    /// </summary>
    public const string CacheExpiry = "cacheExpiry";
}

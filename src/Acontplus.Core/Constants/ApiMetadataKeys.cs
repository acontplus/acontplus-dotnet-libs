namespace Acontplus.Core.Constants;

/// <summary>
/// Metadata keys for API-level response metadata (tracing, correlation, versioning, etc.).
/// For pagination-specific metadata, see <see cref="PaginationMetadataKeys"/>.
/// </summary>
public static class ApiMetadataKeys
{
    // Core Response Metadata
    /// <summary>
    /// Distributed trace identifier for the request.
    /// </summary>
    public const string TraceId = "traceId";
    
    /// <summary>
    /// Unique identifier for the individual request.
    /// </summary>
    public const string RequestId = "requestId";
    
    /// <summary>
    /// Correlation identifier for tracking related requests across services.
    /// </summary>
    public const string CorrelationId = "correlationId";
    
    /// <summary>
    /// Identifier for the client making the request.
    /// </summary>
    public const string ClientId = "clientId";
    
    /// <summary>
    /// Token issuer identifier.
    /// </summary>
    public const string Issuer = "issuer";
    
    /// <summary>
    /// Tenant identifier for multi-tenant applications.
    /// </summary>
    public const string TenantId = "tenantId";
    
    /// <summary>
    /// UTC timestamp of the response.
    /// </summary>
    public const string TimestampUtc = "timestampUtc";
    
    /// <summary>
    /// API version used for the request.
    /// </summary>
    public const string Version = "apiVersion";
    
    /// <summary>
    /// Environment name (e.g., development, staging, production).
    /// </summary>
    public const string Environment = "env";

    // Pagination Container (wraps PagedResult data for API responses)
    // Note: Individual pagination fields (pageIndex, pageSize, etc.) are part of PagedResult itself
    // This key is used when you need to nest pagination info in API metadata
    /// <summary>
    /// Container for pagination metadata in API responses.
    /// </summary>
    public const string Pagination = "paging";
    
    /// <summary>
    /// Zero-based index of the current page.
    /// </summary>
    public const string PageIndex = "pageIndex";
    
    /// <summary>
    /// Number of items per page.
    /// </summary>
    public const string PageSize = "pageSize";
    
    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public const string TotalCount = "totalCount";
    
    /// <summary>
    /// Total number of pages available.
    /// </summary>
    public const string TotalPages = "totalPages";
    
    /// <summary>
    /// Indicates whether a next page is available.
    /// </summary>
    public const string HasNextPage = "hasNextPage";
    
    /// <summary>
    /// Indicates whether a previous page is available.
    /// </summary>
    public const string HasPreviousPage = "hasPreviousPage";
    
    /// <summary>
    /// HATEOAS links for navigation.
    /// </summary>
    public const string Links = "links";  // For HATEOAS when needed

    // Performance
    /// <summary>
    /// Request processing duration in milliseconds.
    /// </summary>
    public const string Duration = "durationMs";

    // Optional Extended Metadata
    /// <summary>
    /// Deprecation notice for the API endpoint.
    /// </summary>
    public const string Deprecation = "deprecation";
    
    /// <summary>
    /// Rate limiting information for the client.
    /// </summary>
    public const string RateLimit = "rateLimit";
}

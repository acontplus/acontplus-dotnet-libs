namespace Demo.Application.Interfaces;

/// <summary>
/// Dapper-based service for high-performance order reporting queries.
/// Demonstrates IDapperRepository usage for complex read operations.
/// </summary>
public interface IOrderReportService
{
    /// <summary>
    /// Gets paginated orders using Dapper for optimized read performance.
    /// </summary>
    Task<PagedResult<OrderDto>> GetPagedOrdersAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order summary statistics using Dapper.
    /// </summary>
    Task<OrderSummaryDto> GetOrderSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by status using Dapper with automatic object mapping.
    /// </summary>
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dashboard data with multiple result sets in a single query.
    /// </summary>
    Task<OrderDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}

namespace Demo.Application.Services;

/// <summary>
/// Dapper-based service for high-performance order reporting.
/// Demonstrates IDapperRepository usage for optimized read operations.
/// </summary>
/// <remarks>
/// This service uses Dapper instead of Entity Framework for scenarios where:
/// - Complex SQL queries with JOINs are needed
/// - Maximum read performance is required
/// - Direct control over SQL is preferred
/// - Multiple result sets are needed in a single query
/// </remarks>
public class OrderReportService : IOrderReportService
{
    private readonly IDapperRepository _dapper;
    private readonly ILogger<OrderReportService> _logger;

    public OrderReportService(
        IDapperRepository dapper,
        ILogger<OrderReportService> logger)
    {
        _dapper = dapper ?? throw new ArgumentNullException(nameof(dapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PagedResult<OrderDto>> GetPagedOrdersAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting paged orders: Page {Page}, Size {Size}",
            pagination.PageIndex, pagination.PageSize);

        // Dapper handles OFFSET-FETCH pagination automatically
        var sql = @"
            SELECT
                Id AS OrderId,
                CustomerName,
                ProductName,
                Quantity,
                Price,
                TotalAmount,
                CreatedAt,
                Status
            FROM dbo.Orders
            WHERE IsDeleted = 0";

        return await _dapper.GetPagedAsync<OrderDto>(sql, pagination, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderSummaryDto> GetOrderSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting order summary statistics");

        var sql = @"
            SELECT
                COUNT(*) AS TotalOrders,
                ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingOrders,
                SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedOrders
            FROM dbo.Orders
            WHERE IsDeleted = 0";

        var result = await _dapper.QueryFirstOrDefaultAsync<OrderSummaryDto>(
            sql,
            cancellationToken: cancellationToken);

        return result ?? new OrderSummaryDto();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting orders by status: {Status}", status);

        var sql = @"
            SELECT
                Id AS OrderId,
                CustomerName,
                ProductName,
                Quantity,
                Price,
                TotalAmount,
                CreatedAt,
                Status
            FROM dbo.Orders
            WHERE Status = @Status AND IsDeleted = 0
            ORDER BY CreatedAt DESC";

        // Dapper automatically maps @Status parameter
        return await _dapper.QueryAsync<OrderDto>(
            sql,
            new { Status = status },
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting order dashboard data");

        // Multiple result sets in a single round-trip
        // Status is an int enum: Created=0, Processing=1, Processed=2, Shipped=3, Delivered=4, Cancelled=5
        var sql = @"
            -- Summary statistics
            SELECT
                COUNT(*) AS TotalOrders,
                ISNULL(SUM(TotalAmount), 0) AS TotalRevenue,
                SUM(CASE WHEN Status IN (0, 1) THEN 1 ELSE 0 END) AS PendingOrders,
                SUM(CASE WHEN Status IN (2, 3, 4) THEN 1 ELSE 0 END) AS CompletedOrders
            FROM dbo.Orders
            WHERE IsDeleted = 0;

            -- Recent orders (top 10)
            SELECT TOP 10
                Id AS OrderId,
                CustomerName,
                ProductName,
                Quantity,
                Price,
                TotalAmount,
                CreatedAt,
                Status
            FROM dbo.Orders
            WHERE IsDeleted = 0
            ORDER BY CreatedAt DESC;

            -- Top customers by total spent
            SELECT TOP 5
                CustomerName,
                COUNT(*) AS OrderCount,
                SUM(TotalAmount) AS TotalSpent
            FROM dbo.Orders
            WHERE IsDeleted = 0
            GROUP BY CustomerName
            ORDER BY TotalSpent DESC;";

        var (summaries, orders) = await _dapper.QueryMultipleAsync<OrderSummaryDto, OrderDto>(
            sql,
            cancellationToken: cancellationToken);

        var summary = summaries.FirstOrDefault() ?? new OrderSummaryDto();

        // For top customers, we need a separate query since we have 3 result sets
        // In a real scenario, you might use QueryMultipleAsync<T1, T2, T3>
        var topCustomersSql = @"
            SELECT TOP 5
                CustomerName,
                COUNT(*) AS OrderCount,
                SUM(TotalAmount) AS TotalSpent
            FROM dbo.Orders
            WHERE IsDeleted = 0
            GROUP BY CustomerName
            ORDER BY TotalSpent DESC";

        var topCustomers = await _dapper.QueryAsync<TopCustomerDto>(
            topCustomersSql,
            cancellationToken: cancellationToken);

        return new OrderDashboardDto(summary, orders, topCustomers);
    }
}

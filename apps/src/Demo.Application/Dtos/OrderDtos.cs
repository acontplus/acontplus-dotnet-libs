namespace Demo.Application.Dtos;

/// <summary>
/// Command to create a new order (CQRS Write Model)
/// Demonstrates both Domain Events and Application Events:
/// - Domain Event: EntityCreatedEvent dispatched to create OrderLineItems (transactional)
/// - Application Event: OrderCreatedEvent published for notifications (async)
/// </summary>
public record CreateOrderCommand(
    string CustomerName,
    string ProductName,
    int Quantity,
    decimal Price);

/// <summary>
/// Line item for order creation
/// </summary>
public record OrderLineItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Query to get order by ID (CQRS Read Model)
/// </summary>
public record GetOrderQuery(int OrderId);

/// <summary>
/// Result of order creation
/// </summary>
public record OrderCreatedResult(
    int OrderId,
    DateTime CreatedAt,
    decimal TotalAmount);

/// <summary>
/// Order data transfer object for API responses
/// Note: Uses class with init properties for Dapper compatibility
/// </summary>
public class OrderDto
{
    public int OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public int Status { get; init; }
}

/// <summary>
/// Order summary statistics for dashboard
/// Note: Uses class with init properties for Dapper compatibility
/// </summary>
public class OrderSummaryDto
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public int PendingOrders { get; init; }
    public int CompletedOrders { get; init; }
}

/// <summary>
/// Dashboard data combining multiple result sets
/// </summary>
public record OrderDashboardDto(
    OrderSummaryDto Summary,
    IEnumerable<OrderDto> RecentOrders,
    IEnumerable<TopCustomerDto> TopCustomers);

/// <summary>
/// Top customer by order value
/// Note: Uses class with init properties for Dapper compatibility
/// </summary>
public class TopCustomerDto
{
    public string CustomerName { get; init; } = string.Empty;
    public int OrderCount { get; init; }
    public decimal TotalSpent { get; init; }
}

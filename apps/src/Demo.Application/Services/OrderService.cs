using Acontplus.Core.Abstractions.Messaging;
using Acontplus.Core.Domain.Common.Events;
using Demo.Domain.Events;

namespace Demo.Application.Services;

/// <summary>
/// Application service implementing CQRS pattern with event-driven architecture.
/// Coordinates between domain entities, repositories (via UoW), and event bus.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _domainEventDispatcher = domainEventDispatcher ?? throw new ArgumentNullException(nameof(domainEventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Command Handler: Creates a new order and publishes OrderCreatedEvent
    /// </summary>
    public async Task<Result<OrderCreatedResult>> CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(command);

            // Calculate total
            var totalAmount = command.Quantity * command.Price;

            // Create domain entity
            var order = new Order
            {
                Id = 0, // Will be set by database
                CustomerName = command.CustomerName,
                ProductName = command.ProductName,
                Quantity = command.Quantity,
                Price = command.Price,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Created
            };

            // Persist to repository via UoW
            var orderRepository = _unitOfWork.GetRepository<Order>();
            var createdOrder = await orderRepository.AddAsync(order, cancellationToken);

            // Save changes first to get the database-generated ID
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 1. Publish DOMAIN EVENT (DDD pattern - within bounded context)
            // Handler will create OrderLineItems using createdOrder.Id (now has valid ID)
            await _domainEventDispatcher.Dispatch(new EntityCreatedEvent(
                createdOrder.Id,
                nameof(Order),
                null));

            // 2. Publish APPLICATION EVENT (Event Bus - cross-cutting concerns)
            await _eventPublisher.PublishAsync(new OrderCreatedEvent(
                createdOrder.Id,
                command.CustomerName,
                command.ProductName,
                totalAmount,
                createdOrder.CreatedAt), cancellationToken);

            _logger.LogInformation(
                "Order {OrderId} created - Domain and Application events published",
                createdOrder.Id);

            // Return result
            var result = new OrderCreatedResult(
                createdOrder.Id,
                createdOrder.CreatedAt,
                totalAmount);

            return Result<OrderCreatedResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerName}", command.CustomerName);
            return Result<OrderCreatedResult>.Failure(
                DomainError.Internal("ORDER_CREATE_FAILED", $"Failed to create order: {ex.Message}"));
        }
    }

    /// <summary>
    /// Query Handler: Retrieves order by ID
    /// </summary>
    public async Task<Result<OrderDto>> GetOrderByIdAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(query);

            var orderRepository = _unitOfWork.GetRepository<Order>();
            var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<OrderDto>.Failure(
                    DomainError.NotFound("ORDER_NOT_FOUND", $"Order with ID {query.OrderId} not found"));
            }

            var dto = new OrderDto
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                Price = order.Price,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Status = (int)order.Status
            };

            return Result<OrderDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve order {OrderId}", query.OrderId);
            return Result<OrderDto>.Failure(
                DomainError.Internal("ORDER_RETRIEVAL_FAILED", $"Failed to retrieve order: {ex.Message}"));
        }
    }

    /// <summary>
    /// Query Handler: Retrieves all orders
    /// </summary>
    public async Task<Result<IEnumerable<OrderDto>>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orderRepository = _unitOfWork.GetRepository<Order>();
            var orders = await orderRepository.GetAllAsync(cancellationToken);

            var dtos = orders.Select(order => new OrderDto
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                Price = order.Price,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Status = (int)order.Status
            }).ToList();

            return Result<IEnumerable<OrderDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all orders");
            return Result<IEnumerable<OrderDto>>.Failure(
                DomainError.Internal("ORDERS_RETRIEVAL_FAILED", $"Failed to retrieve orders: {ex.Message}"));
        }
    }
}

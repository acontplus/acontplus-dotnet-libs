using Acontplus.Core.Domain.Common.Events;

namespace Demo.Infrastructure.EventHandlers;

/// <summary>
/// DOMAIN EVENT HANDLER - Creates OrderLineItems when Order is created.
/// This demonstrates transactional domain event handling where:
/// 1. Order is created (first insert gets ID)
/// 2. EntityCreatedEvent is dispatched (synchronously)
/// 3. This handler creates OrderLineItems using Order.Id (second insert)
/// 4. Both inserts commit together in same transaction/UoW
///
/// This solves the problem: "I need two inserts where second depends on first's ID"
/// Uses IUnitOfWork to get repositories - no need for explicit DI registration.
/// </summary>
public class OrderLineItemsCreationHandler(
    IUnitOfWork unitOfWork,
    ILogger<OrderLineItemsCreationHandler> logger) : IDomainEventHandler<EntityCreatedEvent>
{
    private readonly ILogger<OrderLineItemsCreationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S2139",
        Justification = "Exception is logged before rethrowing to trigger transaction rollback.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2139",
        Justification = "Exception is logged before rethrowing to trigger transaction rollback.")]
    public async Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Only handle Order creation events
        if (domainEvent.EntityType != nameof(Order))
            return;

        _logger.LogInformation(
            "DOMAIN EVENT: Creating line items for Order {OrderId}",
            domainEvent.EntityId);

        try
        {
            // Get repositories from UoW (same transaction as Order creation)
            var orderRepository = _unitOfWork.GetRepository<Order>();
            var lineItemRepository = _unitOfWork.GetRepository<OrderLineItem>();

            // Get the created order to access its details
            var order = await orderRepository.GetByIdAsync(domainEvent.EntityId, cancellationToken);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", domainEvent.EntityId);
                return;
            }

            // Create line items using the Order.Id from the first insert
            // This demonstrates the key use case: second insert depends on first insert's ID
            var lineItems = new List<OrderLineItem>
            {
                new OrderLineItem
                {
                    Id = 0, // Will be set by database
                    OrderId = order.Id, // ← Uses ID from first insert
                    ProductName = order.ProductName,
                    Quantity = order.Quantity,
                    UnitPrice = order.Price,
                    LineTotal = order.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Add line items to repository (still in same transaction)
            foreach (var lineItem in lineItems)
            {
                await lineItemRepository.AddAsync(lineItem, cancellationToken);
                _logger.LogDebug(
                    "Line item created for Order {OrderId}: {ProductName} x {Quantity}",
                    order.Id,
                    lineItem.ProductName,
                    lineItem.Quantity);
            }

            // NOTE: Don't call SaveChangesAsync here!
            // The UnitOfWork/DbContext will commit both inserts together
            // If this handler throws an exception, BOTH inserts will be rolled back

            _logger.LogInformation(
                "Successfully created {Count} line item(s) for Order {OrderId}",
                lineItems.Count,
                order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating line items for Order {OrderId}",
                domainEvent.EntityId);

            // Rethrow to trigger transaction rollback
            throw;
        }
    }
}

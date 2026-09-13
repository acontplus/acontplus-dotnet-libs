using Acontplus.Core.Domain.Common.Events;

namespace Demo.Infrastructure.EventHandlers;

/// <summary>
/// DOMAIN EVENT HANDLER - Handles EntityCreatedEvent from IDomainEventDispatcher.
/// This demonstrates DDD domain event handling within the bounded context.
/// Runs synchronously during the same transaction as the domain operation.
/// Perfect for audit logging, domain invariants, and updating related aggregates.
/// </summary>
public class EntityAuditHandler : IDomainEventHandler<EntityCreatedEvent>
{
    private readonly ILogger<EntityAuditHandler> _logger;

    public EntityAuditHandler(ILogger<EntityAuditHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(EntityCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // This handler runs within the same transaction/unit of work
        // Perfect for:
        // - Audit logging
        // - Domain invariant enforcement
        // - Updating related aggregates in the same bounded context

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "DOMAIN EVENT: {EntityType} with ID {EntityId} created at {OccurredOn}",
                domainEvent.EntityType,
                domainEvent.EntityId,
                domainEvent.OccurredOn);
        }

        // In a full implementation, audit records can be persisted to a dedicated repository here.

        return Task.CompletedTask;
    }
}

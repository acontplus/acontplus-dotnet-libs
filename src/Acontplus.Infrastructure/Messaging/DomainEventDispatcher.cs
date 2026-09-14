using Acontplus.Core.Domain.Common.Events;

namespace Acontplus.Infrastructure.Messaging;

/// <summary>
/// Domain Event Dispatcher for synchronous event handling within transactions.
/// Dispatches domain events to registered handlers (IDomainEventHandler implementations).
/// Runs synchronously in the same transaction/Unit of Work as the operation that raised the event.
/// </summary>
public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <summary>
    /// Dispatches a domain event to all registered handlers synchronously.
    /// Runs in the same transaction as the caller - if any handler fails, the transaction can be rolled back.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S2139",
        Justification = "Exception is logged with event type and handler details before rethrowing to allow transaction rollback.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2139",
        Justification = "Exception is logged with event type and handler details before rethrowing to allow transaction rollback.")]
    public async Task Dispatch(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        // Get the generic handler type
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Resolve all handlers for this event type
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        var handlersList = handlers.ToList();
        if (handlersList.Count == 0)
        {
            _logger.LogDebug("No handlers registered for domain event: {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation(
            "Dispatching domain event {EventType} to {HandlerCount} handler(s)",
            eventType.Name,
            handlersList.Count);

        // Execute all handlers synchronously (in same transaction)
        foreach (var handler in handlersList)
        {
            if (handler == null) continue;

            try
            {
                // Call HandleAsync method via reflection
                var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, [domainEvent, CancellationToken.None]);
                    if (task != null)
                    {
                        await task;
                    }
                }

                _logger.LogDebug(
                    "Domain event {EventType} handled by {HandlerType}",
                    eventType.Name,
                    handler.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling domain event {EventType} in handler {HandlerType}",
                    eventType.Name,
                    handler.GetType().Name);

                // Rethrow to allow transaction rollback
                throw;
            }
        }

        _logger.LogInformation(
            "Domain event {EventType} successfully dispatched to all handlers",
            eventType.Name);
    }
}

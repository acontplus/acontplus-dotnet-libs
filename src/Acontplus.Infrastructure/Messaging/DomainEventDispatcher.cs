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
    public async Task Dispatch(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);
        }

        // Get the generic handler type
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Resolve all handlers for this event type
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        var handlersList = handlers.Where(h => h != null).ToList();
        if (handlersList.Count == 0)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("No handlers registered for domain event: {EventType}", eventType.Name);
            }
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Dispatching domain event {EventType} to {HandlerCount} handler(s)",
                eventType.Name,
                handlersList.Count);
        }

        // Execute all handlers synchronously (in same transaction)
        foreach (var handler in handlersList)
        {
            await InvokeHandlerAsync(handler!, handlerType, domainEvent, eventType);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Domain event {EventType} successfully dispatched to all handlers",
                eventType.Name);
        }
    }

    private async Task InvokeHandlerAsync(object handler, Type handlerType, IDomainEvent domainEvent, Type eventType)
    {
        try
        {
            // Call HandleAsync method via reflection
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<>.HandleAsync));
            if (handleMethod != null)
            {
                var task = (Task?)handleMethod.Invoke(handler, [domainEvent, CancellationToken.None]);
                if (task != null)
                {
                    await task;
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Domain event {EventType} handled by {HandlerType}",
                    eventType.Name,
                    handler.GetType().Name);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error handling domain event {eventType.Name} in handler {handler.GetType().Name}: {ex.Message}",
                ex);
        }
    }
}

using Acontplus.Core.Abstractions.Messaging;
using Demo.Domain.Events;

namespace Demo.Infrastructure.EventHandlers;

/// <summary>
/// Background service that orchestrates order workflow by listening to multiple event types.
/// Infrastructure layer - implements workflow automation.
/// </summary>
public class OrderWorkflowHandler(
    IEventSubscriber eventSubscriber,
    IEventPublisher eventPublisher,
    ILogger<OrderWorkflowHandler> logger) : BackgroundService
{
    private readonly IEventPublisher _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
    private readonly IEventSubscriber _eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
    private readonly ILogger<OrderWorkflowHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S2139",
        Justification = "Exception is logged before rethrowing to notify host of service failure.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2139",
        Justification = "Exception is logged before rethrowing to notify host of service failure.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderWorkflowHandler started.");

        // Run multiple subscriptions concurrently
        var tasks = new[]
        {
            ProcessOrderCreatedEvents(stoppingToken),
            ProcessOrderProcessedEvents(stoppingToken)
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OrderWorkflowHandler is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderWorkflowHandler");
            throw;
        }
    }

    private async Task ProcessOrderCreatedEvents(CancellationToken stoppingToken)
    {
        await foreach (var orderEvent in _eventSubscriber.SubscribeAsync<OrderCreatedEvent>(stoppingToken))
        {
            _logger.LogInformation(
                "🔄 Auto-processing Order {OrderId} - triggering processing workflow",
                orderEvent.OrderId);

            // Simulate order processing logic
            await Task.Delay(200, stoppingToken);

            // Publish next stage event
            await _eventPublisher.PublishAsync(new OrderProcessedEvent(
                orderEvent.OrderId,
                DateTime.UtcNow,
                "AutomatedSystem"), stoppingToken);

            _logger.LogInformation(
                "✅ Order {OrderId} processed and event published",
                orderEvent.OrderId);
        }
    }

    private async Task ProcessOrderProcessedEvents(CancellationToken stoppingToken)
    {
        await foreach (var processedEvent in _eventSubscriber.SubscribeAsync<OrderProcessedEvent>(stoppingToken))
        {
            _logger.LogInformation(
                "📦 Preparing shipment for Order {OrderId}",
                processedEvent.OrderId);

            // Simulate shipping preparation
            await Task.Delay(150, stoppingToken);

            // Publish shipping event
            await _eventPublisher.PublishAsync(new OrderShippedEvent(
                processedEvent.OrderId,
                DateTime.UtcNow,
                $"TRACK-{processedEvent.OrderId:D10}"), stoppingToken);

            _logger.LogInformation(
                "🚚 Order {OrderId} shipped successfully",
                processedEvent.OrderId);
        }
    }
}

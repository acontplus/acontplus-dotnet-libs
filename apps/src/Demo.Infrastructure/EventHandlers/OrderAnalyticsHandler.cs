using Acontplus.Core.Abstractions.Messaging;
using Demo.Domain.Events;

namespace Demo.Infrastructure.EventHandlers;

/// <summary>
/// Background service that listens to OrderCreatedEvent and updates analytics/reporting.
/// Infrastructure layer - implements cross-cutting concerns.
/// </summary>
public class OrderAnalyticsHandler(
    IEventSubscriber eventSubscriber,
    ILogger<OrderAnalyticsHandler> logger) : BackgroundService
{
    private readonly IEventSubscriber _eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
    private readonly ILogger<OrderAnalyticsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderAnalyticsHandler started. Listening for OrderCreatedEvent...");

        try
        {
            await foreach (var orderEvent in _eventSubscriber.SubscribeAsync<OrderCreatedEvent>(stoppingToken))
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "📊 Recording analytics for Order {OrderId} - Product: {ProductName}, Amount: ${TotalAmount}",
                        orderEvent.OrderId,
                        orderEvent.ProductName,
                        orderEvent.TotalAmount);
                }

                // Simulate analytics processing (replace with actual analytics service)
                await Task.Delay(50, stoppingToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "✅ Analytics recorded for Order {OrderId}",
                        orderEvent.OrderId);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "OrderAnalyticsHandler is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderAnalyticsHandler");
            // Don't rethrow - this would crash the application
            // Consider implementing retry logic or circuit breaker pattern
        }
    }
}

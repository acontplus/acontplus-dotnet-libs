using Acontplus.Core.Abstractions.Messaging;
using Demo.Domain.Events;

namespace Demo.Infrastructure.EventHandlers;

/// <summary>
/// Background service that listens to OrderCreatedEvent and sends email notifications.
/// Infrastructure layer - implements cross-cutting concerns.
/// </summary>
public class OrderNotificationHandler(
    IEventSubscriber eventSubscriber,
    ILogger<OrderNotificationHandler> logger) : BackgroundService
{
    private readonly IEventSubscriber _eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
    private readonly ILogger<OrderNotificationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S2139",
        Justification = "Exception is logged before rethrowing to notify host of service failure.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2139",
        Justification = "Exception is logged before rethrowing to notify host of service failure.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderNotificationHandler started. Listening for OrderCreatedEvent...");

        try
        {
            await foreach (var orderEvent in _eventSubscriber.SubscribeAsync<OrderCreatedEvent>(stoppingToken))
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "📧 Sending email notification for Order {OrderId} - Customer: {CustomerName}, Total: ${TotalAmount}",
                        orderEvent.OrderId,
                        orderEvent.CustomerName,
                        orderEvent.TotalAmount);
                }

                // Simulate email sending (replace with actual email service)
                await Task.Delay(100, stoppingToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "✅ Email notification sent successfully for Order {OrderId}",
                        orderEvent.OrderId);
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "OrderNotificationHandler is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderNotificationHandler");
            throw;
        }
    }
}

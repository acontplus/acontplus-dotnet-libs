namespace Acontplus.Core.Domain.Common.Events;

/// <summary>
/// Defines a dispatcher for domain events.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a domain event asynchronously.
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Dispatch(IDomainEvent domainEvent);
}

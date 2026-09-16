namespace Acontplus.Core.Domain.Common.Events;

/// <summary>
/// Interface for handling domain events
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event asynchronously
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for domain event handlers
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event to handle</typeparam>
public abstract class BaseDomainEventHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Determines whether this handler can handle the specified domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to evaluate.</param>
    /// <returns><c>true</c> if the event can be handled; otherwise, <c>false</c>.</returns>
    public virtual bool CanHandle(TDomainEvent domainEvent) => domainEvent is not null;

    /// <summary>
    /// Handles the domain event asynchronously
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public abstract Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

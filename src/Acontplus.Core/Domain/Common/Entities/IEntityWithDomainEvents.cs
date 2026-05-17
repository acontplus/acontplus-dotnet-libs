namespace Acontplus.Core.Domain.Common.Entities;

/// <summary>
/// Represents an entity that can track domain events.
/// </summary>
public interface IEntityWithDomainEvents
{
    /// <summary>
    /// Adds a domain event to the entity's event collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    void AddDomainEvent(IDomainEvent domainEvent);
    
    /// <summary>
    /// Clears all domain events from the entity's event collection.
    /// </summary>
    void ClearDomainEvents();
    
    /// <summary>
    /// Gets the collection of domain events associated with this entity.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}

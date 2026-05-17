namespace Acontplus.Core.Domain.Common.Entities;

/// <summary>
/// Base class for all entities with strongly-typed identifiers and domain event support.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEntityWithDomainEvents where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public required TId Id { get; init; }

    private readonly List<IDomainEvent> _domainEvents = [];
    
    /// <summary>
    /// Gets the collection of domain events associated with this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    void IEntityWithDomainEvents.AddDomainEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);

    /// <summary>
    /// Clears all domain events from this entity.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// Two entities are considered equal if they have the same type and the same non-default identifier.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is not Entity<TId> other
            ? false
            : ReferenceEquals(this, other) || GetType() == other.GetType() && !Id.Equals(default) && !other.Id.Equals(default) && Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
    {
        return a is null && b is null || a is not null && b is not null && a.Equals(b);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="a">The first entity to compare.</param>
    /// <param name="b">The second entity to compare.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);

    /// <summary>
    /// Returns the hash code for this entity based on its type and identifier.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>
    /// Creates a new instance of the specified entity type with the given identifier.
    /// </summary>
    /// <typeparam name="T">The type of entity to create.</typeparam>
    /// <param name="id">The identifier for the new entity.</param>
    /// <returns>A new instance of the specified entity type.</returns>
    protected static T Create<T>(TId id) where T : Entity<TId>, new()
    {
        return new T { Id = id };
    }
}

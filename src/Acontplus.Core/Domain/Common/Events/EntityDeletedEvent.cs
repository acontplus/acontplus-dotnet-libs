namespace Acontplus.Core.Domain.Common.Events;

/// <summary>
/// Domain event that is raised when an entity is deleted.
/// </summary>
/// <param name="EntityId">The unique identifier of the deleted entity.</param>
/// <param name="EntityType">The type name of the deleted entity.</param>
/// <param name="DeletedByUserId">The identifier of the user who deleted the entity, if available.</param>
public record EntityDeletedEvent(int EntityId, string EntityType, int? DeletedByUserId)
    : IDomainEvent
{
    /// <summary>
    /// Gets the UTC date and time when the event occurred.
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

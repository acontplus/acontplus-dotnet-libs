namespace Acontplus.Core.Domain.Common.Events;

/// <summary>Raised when a domain entity is modified.</summary>
public record EntityModifiedEvent(int EntityId, string EntityType, int? ModifiedByUserId)
    : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

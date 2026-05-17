namespace Acontplus.Core.Domain.Common.Events;

/// <summary>Raised when a previously soft-deleted domain entity is restored.</summary>
public record EntityRestoredEvent(int EntityId, string EntityType, int? RestoredByUserId)
    : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

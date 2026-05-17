namespace Acontplus.Core.Domain.Common.Events;

/// <summary>Raised when a domain entity is created.</summary>
public record EntityCreatedEvent(int EntityId, string EntityType, int? CreatedByUserId)
    : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

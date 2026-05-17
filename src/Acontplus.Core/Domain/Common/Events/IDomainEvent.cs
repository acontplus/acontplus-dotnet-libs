namespace Acontplus.Core.Domain.Common.Events;

/// <summary>
/// Represents a domain event that occurred within the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the date and time when the domain event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}

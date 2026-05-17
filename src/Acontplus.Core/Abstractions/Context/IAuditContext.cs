namespace Acontplus.Core.Abstractions.Context;

/// <summary>
/// Provides the current user's identity information for automatic audit field population.
/// Resolved per-request from claims and injected into the persistence layer.
/// </summary>
public interface IAuditContext
{
    /// <summary>
    /// Gets the numeric identifier of the current user, or <c>null</c> if not authenticated.
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// Gets the username of the current user, or <c>null</c> if not authenticated.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the role identifier of the current user, or <c>null</c> if the claim is absent.
    /// </summary>
    int? UserRoleId { get; }

    /// <summary>
    /// Gets a value indicating whether the current request originated from a mobile device.
    /// </summary>
    bool IsMobile { get; }
}

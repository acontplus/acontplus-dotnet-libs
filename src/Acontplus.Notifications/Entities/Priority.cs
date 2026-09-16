namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents the priority level for notification queueing and delivery.
/// </summary>
public class Priority : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique priority code.
    /// </summary>
    [Required, MaxLength(5)] public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the priority name (e.g. "High", "Medium", "Low").
    /// </summary>
    public string? Name { get; set; }
}
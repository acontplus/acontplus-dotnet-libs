namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents a recipient of a notification, linked directly to a user or a recipient group.
/// </summary>
public class NotificationRecipient : BaseEntity
{
    /// <summary>
    /// Gets or sets the foreign key identifier for the parent notification.
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the parent notification.
    /// </summary>
    public required Notification Notification { get; set; }

    /// <summary>
    /// Gets or sets the optional group identifier for group notifications.
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the recipient group.
    /// </summary>
    public NotificationGroup? Group { get; set; }

    /// <summary>
    /// Gets or sets whether the recipient has read the notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the recipient read the notification.
    /// </summary>
    public DateTime? ReadAt { get; set; }
}
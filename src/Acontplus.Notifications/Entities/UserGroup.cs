namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents the relationship between a user and a notification group.
/// </summary>
public class UserGroup : BaseEntity
{
    /// <summary>
    /// Gets or sets the foreign key identifier for the notification group.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the notification group.
    /// </summary>
    public required NotificationGroup Group { get; set; }
}
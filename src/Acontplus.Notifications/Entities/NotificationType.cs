namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents the classification type of a notification (e.g. Email, WhatsApp, Push).
/// </summary>
public class NotificationType : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique code for the notification type.
    /// </summary>
    [Required, MaxLength(5)] public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the display name of the notification type.
    /// </summary>
    public required string Name { get; set; }
}
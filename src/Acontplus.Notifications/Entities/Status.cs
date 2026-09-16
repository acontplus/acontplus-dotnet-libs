namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents the delivery status of a notification (e.g. Queued, Processing, Sent, Failed).
/// </summary>
public class Status : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique status code.
    /// </summary>
    [Required, MaxLength(5)] public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the human-readable status name.
    /// </summary>
    public string? Name { get; set; }
}
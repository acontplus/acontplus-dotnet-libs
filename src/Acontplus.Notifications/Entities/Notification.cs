namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents a notification record with compressed parameters, classification, and delivery tracking.
/// </summary>
public class Notification : BaseEntity
{
    private string? _decompressedParameters;

    /// <summary>
    /// Gets or sets the foreign key identifier for the notification type.
    /// </summary>
    public int NotificationTypeId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the notification type.
    /// </summary>
    public required NotificationType NotificationType { get; set; }

    /// <summary>
    /// Gets or sets the gzip-compressed parameters byte array for the notification.
    /// </summary>
    [Required] public required byte[] CompressedParameters { get; set; }

    /// <summary>
    /// Gets or sets the optional notification title.
    /// </summary>
    [MaxLength(100)] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the notification subject line.
    /// </summary>
    [MaxLength(300)] public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the notification message text.
    /// </summary>
    [MaxLength(300)] public string? Message { get; set; }

    /// <summary>
    /// Gets or sets whether the notification has been marked as read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets attachments associated with this notification.
    /// </summary>
    public ICollection<Attachment>? Attachments { get; set; }

    /// <summary>
    /// Gets or sets uncompressed parameters as a JSON string, automatically compressed to <see cref="CompressedParameters"/>.
    /// </summary>
    [NotMapped]
    public string Parameters
    {
        get
        {
            switch (_decompressedParameters)
            {
                case null when true:
                    {
                        var decompressedBytes = CompressionUtils.DecompressGZip(CompressedParameters);
                        _decompressedParameters = Encoding.UTF8.GetString(decompressedBytes);
                        break;
                    }
            }

            return _decompressedParameters;
        }
        set
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);
            CompressedParameters = CompressionUtils.CompressGZip(stringBytes);
            _decompressedParameters = value; // Cache the value
        }
    }
}
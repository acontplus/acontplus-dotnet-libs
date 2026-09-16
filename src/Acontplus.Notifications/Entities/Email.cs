namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents an email notification record with compressed body storage and lifecycle tracking.
/// </summary>
public class Email : BaseEntity
{
    private string? _decompressedBody;

    /// <summary>
    /// Gets or sets the foreign key identifier for the parent notification.
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent notification.
    /// </summary>
    public required Notification Notification { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the sender email configuration.
    /// </summary>
    public int EmailSenderConfigId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the sender email configuration.
    /// </summary>
    public required EmailSenderConfig EmailSenderConfig { get; set; }

    /// <summary>
    /// Gets or sets the email address of the recipient.
    /// </summary>
    [Required, MaxLength(254)] public required string RecipientEmail { get; set; }

    /// <summary>
    /// Gets or sets the email subject line.
    /// </summary>
    [Required, MaxLength(300)] public required string Subject { get; set; }

    /// <summary>
    /// Gets or sets carbon copy (CC) recipients.
    /// </summary>
    [MaxLength(1000)] public string? Cc { get; set; }

    /// <summary>
    /// Gets or sets whether the email body is formatted as HTML.
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// Gets or sets the gzip-compressed raw byte array of the email body.
    /// </summary>
    [Required] public required byte[] CompressedBody { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the delivery priority.
    /// </summary>
    public int PriorityId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the delivery priority.
    /// </summary>
    public required Priority Priority { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the notification status.
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the notification status.
    /// </summary>
    public required Status Status { get; set; }

    /// <summary>
    /// Gets or sets the count of delivery retry attempts.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the scheduled dispatch timestamp in UTC.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the actual dispatch timestamp in UTC.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Gets or sets optional template name or identifier used to render the email.
    /// </summary>
    [MaxLength(150)] public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the uncompressed string representation of the email body, automatically compressing to <see cref="CompressedBody"/>.
    /// </summary>
    [NotMapped]
    public string Content
    {
        get
        {
            switch (_decompressedBody)
            {
                case null when true:
                    {
                        var decompressedBytes = CompressionUtils.DecompressGZip(CompressedBody);
                        _decompressedBody = Encoding.UTF8.GetString(decompressedBytes);
                        break;
                    }
            }

            return _decompressedBody;
        }
        set
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);
            CompressedBody = CompressionUtils.CompressGZip(stringBytes);
            _decompressedBody = value; // Cache the value
        }
    }
}

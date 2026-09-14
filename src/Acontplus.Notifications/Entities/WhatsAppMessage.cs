namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents a WhatsApp notification message record with compressed body and delivery tracking.
/// </summary>
public class WhatsAppMessage : BaseEntity
{
    private string? _decompressedMessage;

    /// <summary>
    /// Gets or sets the foreign key identifier for the parent notification.
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent notification.
    /// </summary>
    public required Notification Notification { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the WhatsApp sender configuration.
    /// </summary>
    public int WhatsAppSenderConfigId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the WhatsApp sender configuration.
    /// </summary>
    public required WhatsAppSenderConfig WhatsAppSenderConfig { get; set; }

    /// <summary>
    /// Gets or sets the gzip-compressed raw byte array of the message text.
    /// </summary>
    [Required] public required byte[] CompressedMessage { get; set; }

    /// <summary>
    /// Gets or sets optional client operating system name.
    /// </summary>
    [MaxLength(150)] public string? Os { get; set; }

    /// <summary>
    /// Gets or sets optional client browser name.
    /// </summary>
    [MaxLength(150)] public string? Browser { get; set; }

    /// <summary>
    /// Gets or sets the recipient phone number in international format.
    /// </summary>
    [Required, MaxLength(25)] public required string RecipientPhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the message priority.
    /// </summary>
    public int PriorityId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the message priority.
    /// </summary>
    public required Priority Priority { get; set; }

    /// <summary>
    /// Gets or sets the foreign key identifier for the delivery status.
    /// </summary>
    public int StatusId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property for the delivery status.
    /// </summary>
    public required Status Status { get; set; }

    /// <summary>
    /// Gets or sets the scheduled dispatch timestamp in UTC.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the actual dispatch timestamp in UTC.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Gets or sets the uncompressed string representation of the message, automatically compressing to <see cref="CompressedMessage"/>.
    /// </summary>
    [NotMapped]
    public string Message
    {
        get
        {
            if (_decompressedMessage == null)
            {
                var decompressedBytes = CompressionUtils.DecompressGZip(CompressedMessage);
                _decompressedMessage = Encoding.UTF8.GetString(decompressedBytes);
            }

            return _decompressedMessage;
        }
        set
        {
            var stringBytes = Encoding.UTF8.GetBytes(value);
            CompressedMessage = CompressionUtils.CompressGZip(stringBytes);
            _decompressedMessage = value; // Cache the value
        }
    }
}

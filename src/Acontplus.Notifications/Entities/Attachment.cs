namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents a file attachment associated with a notification.
/// </summary>
public class Attachment : BaseEntity
{
    /// <summary>
    /// Gets or sets the foreign key identifier for the parent notification.
    /// </summary>
    public int NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated notification.
    /// </summary>
    public required Notification Notification { get; set; }

    /// <summary>
    /// Gets or sets the file name including its extension.
    /// </summary>
    [Required, MaxLength(300)] public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the attachment.
    /// </summary>
    [Required, MaxLength(50)] public required string FileType { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public int? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the optional local filesystem path where the file is stored.
    /// </summary>
    [MaxLength(300)] public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the optional Amazon S3 object key.
    /// </summary>
    [MaxLength(300)] public string? S3ObjectKey { get; set; }

    /// <summary>
    /// Gets or sets the optional Amazon S3 public or presigned URL.
    /// </summary>
    [MaxLength(300)] public string? S3ObjectUrl { get; set; }
}
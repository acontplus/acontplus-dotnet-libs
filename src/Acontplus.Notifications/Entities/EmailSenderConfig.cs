namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents SMTP sender configuration settings for global or tenant-scoped email delivery.
/// </summary>
public class EmailSenderConfig : BaseEntity
{
    /// <summary>
    /// Gets or sets the company identifier for tenant-specific configuration, or null for global.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Gets or sets whether this configuration is applied globally across all companies.
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Gets or sets the sender email address (e.g. no-reply@example.com).
    /// </summary>
    [Required, MaxLength(150)] public required string SenderEmail { get; set; }

    /// <summary>
    /// Gets or sets the friendly display name for the sender.
    /// </summary>
    [Required, MaxLength(300)] public required string SenderName { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server host or IP address.
    /// </summary>
    [Required, MaxLength(150)] public required string SmtpServer { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server port number.
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// Gets or sets whether SSL/TLS encryption is required.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Gets or sets the SMTP authentication username.
    /// </summary>
    [Required, MaxLength(150)] public required string Username { get; set; }

    /// <summary>
    /// Gets or sets encrypted password bytes for secure storage.
    /// </summary>
    public byte[]? EncryptedPassword { get; set; }

    /// <summary>
    /// Gets or sets optional password hash.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets optional plaintext password.
    /// </summary>
    public string? Password { get; set; }
}
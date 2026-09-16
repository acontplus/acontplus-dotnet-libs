namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents WhatsApp Cloud API sender credentials and endpoint configuration.
/// </summary>
public class WhatsAppSenderConfig : BaseEntity
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
    /// Gets or sets the Meta WhatsApp Business Account ID or Phone Number ID.
    /// </summary>
    [Required, MaxLength(50)] public required string AccountId { get; set; }

    /// <summary>
    /// Gets or sets the Meta Graph API access token.
    /// </summary>
    [Required, MaxLength(300)] public required string AuthToken { get; set; }

    /// <summary>
    /// Gets or sets the sender phone number.
    /// </summary>
    [MaxLength(25)] public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets optional custom service URL or Meta Graph API endpoint.
    /// </summary>
    [MaxLength(250)] public string? UrlService { get; set; }
}
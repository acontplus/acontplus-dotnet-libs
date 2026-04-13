namespace Acontplus.Notifications.WhatsApp.Models;

/// <summary>
/// Configuration for the WhatsApp Cloud API service.
/// Bind from the <c>"WhatsApp"</c> section of your appsettings.json via
/// <c>services.AddWhatsAppService(configuration)</c>.
/// </summary>
public sealed class WhatsAppOptions
{
    /// <summary>The appsettings section name.</summary>
    public const string SectionName = "WhatsApp";

    // -------------------------------------------------------------------------
    // Default / single-tenant credentials
    // -------------------------------------------------------------------------

    /// <summary>
    /// Default WhatsApp Business phone number ID.
    /// Obtain from Meta Business Manager → WhatsApp → API Setup.
    /// </summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>
    /// Default Meta Graph API access token (permanent system-user token recommended).
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Optional: default WhatsApp Business Account ID (WABA ID).</summary>
    public string? BusinessAccountId { get; set; }

    /// <summary>
    /// App secret used for webhook signature validation (X-Hub-Signature-256).
    /// Found in Meta App Dashboard → Settings → Basic → App Secret.
    /// </summary>
    public string? AppSecret { get; set; }

    // -------------------------------------------------------------------------
    // Multi-tenant named accounts
    // -------------------------------------------------------------------------

    /// <summary>
    /// Named accounts for multi-tenant scenarios.
    /// Each entry maps an account key to its credentials.
    /// Pass the key via <see cref="WhatsAppCredentials.AccountName"/> in any request.
    /// </summary>
    public Dictionary<string, WhatsAppAccountOptions> Accounts { get; set; } = [];

    // -------------------------------------------------------------------------
    // API settings
    // -------------------------------------------------------------------------

    /// <summary>
    /// Meta Graph API version. Default: <c>v23.0</c> (current stable, April 2026).
    /// Change to a newer version as Meta releases updates.
    /// </summary>
    public string ApiVersion { get; set; } = "v23.0";

    /// <summary>Base URL for the Meta Graph API. Default: <c>https://graph.facebook.com</c>.</summary>
    public string BaseUrl { get; set; } = "https://graph.facebook.com";

    /// <summary>
    /// HTTP request timeout in seconds for individual API calls. Default: 30.
    /// The resilience handler's total timeout may be higher due to retries.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Default country dialing code used when normalizing phone numbers that start with "0"
    /// (e.g., <c>"593"</c> for Ecuador maps 09xxxxxxxx → 593 9xxxxxxxx).
    /// Leave null to skip automatic prefix expansion.
    /// </summary>
    public string? DefaultCountryCode { get; set; }
}

/// <summary>Credentials for a named WhatsApp Business account in multi-tenant setups.</summary>
public sealed class WhatsAppAccountOptions
{
    /// <summary>WhatsApp Business phone number ID for this account.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>Meta Graph API access token for this account.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Optional WhatsApp Business Account ID (WABA ID).</summary>
    public string? BusinessAccountId { get; set; }
}

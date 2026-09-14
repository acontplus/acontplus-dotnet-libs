namespace Acontplus.Billing.Configuration;

/// <summary>
/// Configuration settings for connecting to SRI web services.
/// </summary>
public class SriConnectionSettings
{
    /// <summary>
    /// Gets or sets the SRI reception web service URL.
    /// </summary>
    public string ReceptionUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SRI authorization web service URL.
    /// </summary>
    public string AuthorizationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in seconds for SRI service requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
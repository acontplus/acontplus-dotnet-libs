namespace Acontplus.Notifications.WhatsApp.Models;

/// <summary>
/// Per-request credential override for multi-tenant scenarios.
/// When set on any send request, these credentials take precedence
/// over the default configuration in <see cref="WhatsAppOptions"/>.
/// </summary>
public sealed record WhatsAppCredentials
{
    /// <summary>
    /// References a named account from <see cref="WhatsAppOptions.Accounts"/>.
    /// When set, the named account's credentials are used and other fields are ignored.
    /// </summary>
    public string? AccountName { get; init; }

    /// <summary>
    /// WhatsApp Business phone number ID (inline override).
    /// Both <see cref="PhoneNumberId"/> and <see cref="AccessToken"/> must be set together.
    /// </summary>
    public string? PhoneNumberId { get; init; }

    /// <summary>Meta Graph API access token (inline override).</summary>
    public string? AccessToken { get; init; }

    /// <summary>Optional inline WhatsApp Business Account ID.</summary>
    public string? BusinessAccountId { get; init; }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates credentials that reference a named account from <see cref="WhatsAppOptions.Accounts"/>.
    /// </summary>
    public static WhatsAppCredentials FromAccount(string accountName) =>
        new() { AccountName = accountName };

    /// <summary>Creates inline credentials without requiring appsettings configuration.</summary>
    public static WhatsAppCredentials Inline(string phoneNumberId, string accessToken) =>
        new() { PhoneNumberId = phoneNumberId, AccessToken = accessToken };
}

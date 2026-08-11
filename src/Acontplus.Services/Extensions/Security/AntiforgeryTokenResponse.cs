namespace Acontplus.Services.Extensions.Security;

/// <summary>
/// Represents a request token issued for a browser client to send in the configured antiforgery header or form field.
/// </summary>
/// <param name="RequestToken">The request token paired with the antiforgery cookie stored in the response.</param>
public sealed record AntiforgeryTokenResponse(string RequestToken);

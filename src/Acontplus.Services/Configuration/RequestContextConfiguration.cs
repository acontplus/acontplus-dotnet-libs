namespace Acontplus.Services.Configuration;

/// <summary>
/// Configuration for request context and security headers.
/// </summary>
public class RequestContextConfiguration
{
    /// <summary>
    /// Enables security headers for all responses. Default: true.
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;
    /// <summary>
    /// Denies framing of the site (X-Frame-Options: DENY). Default: true.
    /// </summary>
    public bool FrameOptionsDeny { get; set; } = true;
    /// <summary>
    /// The referrer policy to use. Default: "strict-origin-when-cross-origin".
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    /// <summary>
    /// Requires a client ID for requests. Default: false.
    /// </summary>
    public bool RequireClientId { get; set; } = false;
    /// <summary>
    /// The default anonymous client ID. Default: "anonymous".
    /// </summary>
    public string? AnonymousClientId { get; set; } = "anonymous"; // Default anonymous client ID
    /// <summary>
    /// List of allowed client IDs for whitelisting.
    /// </summary>
    public List<string>? AllowedClientIds { get; set; } // New: For whitelisting client IDs

    /// <summary>
    /// Content Security Policy configuration.
    /// </summary>
    public CspConfiguration? Csp { get; set; } = new();
}

/// <summary>
/// Configuration for Content Security Policy.
/// </summary>
public class CspConfiguration
{
    /// <summary>
    /// List of allowed image sources (domains) for img-src directive.
    /// </summary>
    public List<string> AllowedImageSources { get; set; } = ["https://i.ytimg.com"];

    /// <summary>
    /// List of allowed style sources (domains) for style-src directive.
    /// </summary>
    public List<string> AllowedStyleSources { get; set; } = ["https://fonts.googleapis.com"];

    /// <summary>
    /// List of allowed font sources (domains) for font-src directive.
    /// </summary>
    public List<string> AllowedFontSources { get; set; } = ["https://fonts.gstatic.com"];

    /// <summary>
    /// List of allowed script sources (domains) for script-src directive.
    /// </summary>
    public List<string> AllowedScriptSources { get; set; } = [];

    /// <summary>
    /// List of allowed connect sources (domains) for connect-src directive.
    /// </summary>
    public List<string> AllowedConnectSources { get; set; } = [];

    /// <summary>
    /// List of allowed frame sources (domains) for frame-src directive.
    /// </summary>
    public List<string> AllowedFrameSources { get; set; } = ["https://www.youtube-nocookie.com"];

    /// <summary>
    /// List of allowed media sources (domains) for media-src directive.
    /// </summary>
    public List<string> AllowedMediaSources { get; set; } = [];

    /// <summary>
    /// List of allowed base URI sources (domains) for base-uri directive.
    /// </summary>
    public List<string> AllowedBaseUriSources { get; set; } = [];

    /// <summary>
    /// List of allowed form action sources (domains) for form-action directive.
    /// </summary>
    public List<string> AllowedFormActionSources { get; set; } = [];
}

namespace Acontplus.Notifications.WhatsApp.Models;

// =============================================================================
// Base
// =============================================================================

/// <summary>Base record shared by all WhatsApp send request types.</summary>
public abstract record WhatsAppBaseRequest
{
    /// <summary>
    /// Recipient phone number. Accepts E.164 format (<c>+5930991234567</c>),
    /// digits-only (<c>5930991234567</c>), or local format with leading zero (<c>0991234567</c>)
    /// when <see cref="WhatsAppOptions.DefaultCountryCode"/> is configured.
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// Optional per-request credential override.
    /// When null, the default credentials from <see cref="WhatsAppOptions"/> are used.
    /// </summary>
    public WhatsAppCredentials? Credentials { get; init; }
}

// =============================================================================
// 1. Text — requires active 24-hour conversation window
// =============================================================================

/// <summary>
/// Sends a plain text message to the recipient.
/// The recipient must have initiated contact within the last 24 hours.
/// </summary>
public sealed record SendWhatsAppTextRequest : WhatsAppBaseRequest
{
    /// <summary>Message body text (max 4 096 characters).</summary>
    public required string Body { get; init; }

    /// <summary>
    /// When <c>true</c>, Meta renders a URL preview card for links found in <see cref="Body"/>.
    /// Default: <c>false</c>.
    /// </summary>
    public bool PreviewUrl { get; init; } = false;

    /// <summary>
    /// Set to the <c>wamid.*</c> of a received message to reply in a thread.
    /// </summary>
    public string? ContextMessageId { get; init; }
}

// =============================================================================
// 2. Template — works any time, no 24-hour window required
// =============================================================================

/// <summary>
/// Sends a pre-approved WhatsApp message template.
/// Templates bypass the 24-hour rule and can be sent to opted-in users at any time.
/// </summary>
public sealed record SendWhatsAppTemplateRequest : WhatsAppBaseRequest
{
    /// <summary>Exact name of the approved template as shown in WhatsApp Business Manager.</summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// Language + locale code (e.g. <c>"en_US"</c>, <c>"es_EC"</c>).
    /// Must match the language approved for the template.
    /// </summary>
    public string LanguageCode { get; init; } = "en_US";

    /// <summary>
    /// Optional component parameter overrides (header, body, buttons).
    /// Use <see cref="WhatsAppTemplateComponent"/> factory helpers to build components.
    /// </summary>
    public IReadOnlyList<WhatsAppTemplateComponent>? Components { get; init; }

    /// <summary>Set to the <c>wamid.*</c> of a received message to reply in a thread.</summary>
    public string? ContextMessageId { get; init; }
}

/// <summary>A parameterized component within a template (header, body, or button).</summary>
public sealed record WhatsAppTemplateComponent
{
    /// <summary>Component type: <c>"header"</c>, <c>"body"</c>, or <c>"button"</c>.</summary>
    public required string Type { get; init; }

    /// <summary>Parameters for this component (text, image, document, video, etc.).</summary>
    public required IReadOnlyList<WhatsAppTemplateParameter> Parameters { get; init; }

    /// <summary>
    /// Button sub-type: <c>"url"</c> or <c>"quick_reply"</c>.
    /// Required only when <see cref="Type"/> is <c>"button"</c>.
    /// </summary>
    public string? SubType { get; init; }

    /// <summary>
    /// Zero-based index of the button within the template.
    /// Required only when <see cref="Type"/> is <c>"button"</c>.
    /// </summary>
    public int? Index { get; init; }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a body component with text parameters.</summary>
    public static WhatsAppTemplateComponent Body(params string[] textValues) =>
        new()
        {
            Type = "body",
            Parameters = [.. textValues.Select(WhatsAppTemplateParameter.FromText)]
        };

    /// <summary>Creates a header component with a single text value.</summary>
    public static WhatsAppTemplateComponent HeaderText(string text) =>
        new()
        {
            Type = "header",
            Parameters = [WhatsAppTemplateParameter.FromText(text)]
        };

    /// <summary>Creates a header component with an image (by URL or media ID).</summary>
    public static WhatsAppTemplateComponent HeaderImage(string linkOrId, bool isId = false) =>
        new()
        {
            Type = "header",
            Parameters = [WhatsAppTemplateParameter.FromImage(linkOrId, isId)]
        };

    /// <summary>Creates a header component with a document (by URL or media ID).</summary>
    public static WhatsAppTemplateComponent HeaderDocument(string linkOrId, string? filename = null, bool isId = false) =>
        new()
        {
            Type = "header",
            Parameters = [WhatsAppTemplateParameter.FromDocument(linkOrId, filename, isId)]
        };

    /// <summary>Creates a header component with a video (by URL or media ID).</summary>
    public static WhatsAppTemplateComponent HeaderVideo(string linkOrId, bool isId = false) =>
        new()
        {
            Type = "header",
            Parameters = [WhatsAppTemplateParameter.FromVideo(linkOrId, isId)]
        };

    /// <summary>Creates a quick-reply button component.</summary>
    public static WhatsAppTemplateComponent QuickReplyButton(int index, string payload) =>
        new()
        {
            Type = "button",
            SubType = "quick_reply",
            Index = index,
            Parameters = [new WhatsAppTemplateParameter { Type = "payload", Text = payload }]
        };

    /// <summary>Creates a URL button component with a dynamic URL suffix.</summary>
    public static WhatsAppTemplateComponent UrlButton(int index, string urlSuffix) =>
        new()
        {
            Type = "button",
            SubType = "url",
            Index = index,
            Parameters = [new WhatsAppTemplateParameter { Type = "text", Text = urlSuffix }]
        };
}

/// <summary>A single parameter within a template component.</summary>
public sealed record WhatsAppTemplateParameter
{
    /// <summary>
    /// Parameter type: <c>"text"</c>, <c>"image"</c>, <c>"document"</c>,
    /// <c>"video"</c>, <c>"payload"</c>, <c>"currency"</c>, <c>"date_time"</c>.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>Text value for <c>"text"</c> or button <c>"payload"</c> types.</summary>
    public string? Text { get; init; }

    /// <summary>Image media object for <c>"image"</c> type.</summary>
    public WhatsAppMediaObject? Image { get; init; }

    /// <summary>Document media object for <c>"document"</c> type.</summary>
    public WhatsAppMediaObject? Document { get; init; }

    /// <summary>Video media object for <c>"video"</c> type.</summary>
    public WhatsAppMediaObject? Video { get; init; }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a text parameter.</summary>
    public static WhatsAppTemplateParameter FromText(string text) =>
        new() { Type = "text", Text = text };

    /// <summary>Creates an image parameter (URL or pre-uploaded media ID).</summary>
    public static WhatsAppTemplateParameter FromImage(string linkOrId, bool isId = false) =>
        new()
        {
            Type = "image",
            Image = isId
                ? new WhatsAppMediaObject { Id = linkOrId }
                : new WhatsAppMediaObject { Link = linkOrId }
        };

    /// <summary>Creates a document parameter (URL or pre-uploaded media ID).</summary>
    public static WhatsAppTemplateParameter FromDocument(string linkOrId, string? filename = null, bool isId = false) =>
        new()
        {
            Type = "document",
            Document = isId
                ? new WhatsAppMediaObject { Id = linkOrId, Filename = filename }
                : new WhatsAppMediaObject { Link = linkOrId, Filename = filename }
        };

    /// <summary>Creates a video parameter (URL or pre-uploaded media ID).</summary>
    public static WhatsAppTemplateParameter FromVideo(string linkOrId, bool isId = false) =>
        new()
        {
            Type = "video",
            Video = isId
                ? new WhatsAppMediaObject { Id = linkOrId }
                : new WhatsAppMediaObject { Link = linkOrId }
        };
}

/// <summary>
/// A reference to a media asset — either by publicly accessible URL or by WhatsApp media ID.
/// </summary>
public sealed record WhatsAppMediaObject
{
    /// <summary>Pre-uploaded WhatsApp Media ID (returned by <c>IWhatsAppService.UploadMediaAsync</c>).</summary>
    public string? Id { get; init; }

    /// <summary>Publicly accessible HTTPS URL for the media file.</summary>
    public string? Link { get; init; }

    /// <summary>Filename shown to the recipient. Applicable to documents only.</summary>
    public string? Filename { get; init; }

    /// <summary>Optional caption text. Applicable to image, video, and document messages.</summary>
    public string? Caption { get; init; }
}

// =============================================================================
// 3. Media — image, document, audio, video, sticker
// =============================================================================

/// <summary>Identifies the type of media to be sent in a WhatsApp media message.</summary>
public enum WhatsAppMediaType
{
    /// <summary>JPEG, PNG image (max 5 MB). Supports captions.</summary>
    Image,
    /// <summary>PDF, DOCX, XLSX, etc. (max 100 MB). Supports captions and filename.</summary>
    Document,
    /// <summary>AAC, MP3, OGG audio (max 16 MB). No captions.</summary>
    Audio,
    /// <summary>MP4, 3GPP video (max 16 MB). Supports captions.</summary>
    Video,
    /// <summary>WebP sticker (max 100 KB static / 500 KB animated). No captions.</summary>
    Sticker
}

/// <summary>
/// Sends a media message to the recipient.
/// Requires an active 24-hour conversation window. For outbound-initiated media,
/// use a template with a media header instead.
/// </summary>
public sealed record SendWhatsAppMediaRequest : WhatsAppBaseRequest
{
    /// <summary>Type of media to send.</summary>
    public required WhatsAppMediaType MediaType { get; init; }

    /// <summary>
    /// Publicly accessible HTTPS URL of the media file.
    /// Provide either <see cref="MediaUrl"/> or <see cref="MediaId"/>, not both.
    /// </summary>
    public string? MediaUrl { get; init; }

    /// <summary>
    /// Pre-uploaded WhatsApp Media ID (from <c>IWhatsAppService.UploadMediaAsync</c>).
    /// Provide either <see cref="MediaUrl"/> or <see cref="MediaId"/>, not both.
    /// </summary>
    public string? MediaId { get; init; }

    /// <summary>Optional caption. Supported for image, video, and document. Not for audio or sticker.</summary>
    public string? Caption { get; init; }

    /// <summary>Filename shown to the recipient. Only applicable to documents.</summary>
    public string? Filename { get; init; }

    /// <summary>Set to the <c>wamid.*</c> of a received message to reply in a thread.</summary>
    public string? ContextMessageId { get; init; }
}

/// <summary>
/// Encapsulates a file for upload to the WhatsApp Media API.
/// Uses <see cref="Stream"/> instead of <c>IFormFile</c> to remain independent of ASP.NET Core.
/// </summary>
public sealed record WhatsAppMediaUpload
{
    /// <summary>
    /// The file data stream. The service does NOT dispose this stream —
    /// the caller is responsible for disposing it after the call completes.
    /// Pass this to <c>IWhatsAppService.UploadMediaAsync</c> to get a reusable media ID.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>Original file name including extension (e.g., <c>"invoice.pdf"</c>).</summary>
    public required string FileName { get; init; }

    /// <summary>MIME content type (e.g., <c>"application/pdf"</c>, <c>"image/jpeg"</c>).</summary>
    public required string ContentType { get; init; }
}

// =============================================================================
// 4. Location
// =============================================================================

/// <summary>Sends a map location pin to the recipient.</summary>
public sealed record SendWhatsAppLocationRequest : WhatsAppBaseRequest
{
    /// <summary>Decimal latitude of the location.</summary>
    public required double Latitude { get; init; }

    /// <summary>Decimal longitude of the location.</summary>
    public required double Longitude { get; init; }

    /// <summary>Optional location name displayed above the map.</summary>
    public string? Name { get; init; }

    /// <summary>Optional formatted address displayed below the location name.</summary>
    public string? Address { get; init; }
}

// =============================================================================
// 5. Interactive — quick-reply buttons, list menus, CTA URL
// =============================================================================

/// <summary>Specifies the layout type of an interactive message.</summary>
public enum WhatsAppInteractiveType
{
    /// <summary>Up to 3 quick-reply buttons.</summary>
    Button,
    /// <summary>Scrollable list with sections and rows (up to 10 rows total).</summary>
    List,
    /// <summary>Single call-to-action button that opens a URL.</summary>
    CtaUrl
}

/// <summary>Sends an interactive message with buttons, a list menu, or a CTA URL button.</summary>
public sealed record SendWhatsAppInteractiveRequest : WhatsAppBaseRequest
{
    /// <summary>Type of interactive layout to render.</summary>
    public required WhatsAppInteractiveType InteractiveType { get; init; }

    /// <summary>Main message body text shown to the recipient.</summary>
    public required string BodyText { get; init; }

    /// <summary>Optional header text (plain text; image/document/video headers not supported here).</summary>
    public string? HeaderText { get; init; }

    /// <summary>Optional footer text displayed below the action area (max 60 chars).</summary>
    public string? FooterText { get; init; }

    // -- Button type --

    /// <summary>
    /// Quick-reply buttons (1–3) for <see cref="WhatsAppInteractiveType.Button"/>.
    /// Button IDs are returned in the webhook payload when tapped.
    /// </summary>
    public IReadOnlyList<WhatsAppReplyButton>? ReplyButtons { get; init; }

    // -- List type --

    /// <summary>
    /// Label for the list-open button (max 20 chars).
    /// Used for <see cref="WhatsAppInteractiveType.List"/>. Defaults to <c>"Options"</c>.
    /// </summary>
    public string? ListButtonLabel { get; init; }

    /// <summary>
    /// Sections containing selectable rows.
    /// Used for <see cref="WhatsAppInteractiveType.List"/>.
    /// </summary>
    public IReadOnlyList<WhatsAppListSection>? ListSections { get; init; }

    // -- CTA URL type --

    /// <summary>
    /// Button display text for <see cref="WhatsAppInteractiveType.CtaUrl"/> (e.g., "Open portal").
    /// Defaults to <c>"Open"</c>.
    /// </summary>
    public string? CtaDisplayText { get; init; }

    /// <summary>
    /// Full URL opened when the CTA button is tapped.
    /// Used for <see cref="WhatsAppInteractiveType.CtaUrl"/>.
    /// </summary>
    public string? CtaUrl { get; init; }

    /// <summary>Set to the <c>wamid.*</c> of a received message to reply in a thread.</summary>
    public string? ContextMessageId { get; init; }
}

/// <summary>A quick-reply button in an interactive button message.</summary>
public sealed record WhatsAppReplyButton
{
    /// <summary>
    /// Unique button identifier returned by the webhook when the user taps this button.
    /// Max 256 chars.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Button display label (max 20 chars).</summary>
    public required string Title { get; init; }
}

/// <summary>A section within an interactive list message.</summary>
public sealed record WhatsAppListSection
{
    /// <summary>Optional section heading shown above its rows.</summary>
    public string? Title { get; init; }

    /// <summary>The selectable rows within this section.</summary>
    public required IReadOnlyList<WhatsAppListRow> Rows { get; init; }
}

/// <summary>A selectable row within a list section.</summary>
public sealed record WhatsAppListRow
{
    /// <summary>
    /// Unique row identifier returned by the webhook when selected.
    /// Max 200 chars.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>Row title (max 24 chars).</summary>
    public required string Title { get; init; }

    /// <summary>Optional secondary description text (max 72 chars).</summary>
    public string? Description { get; init; }
}

// =============================================================================
// 6. Reaction
// =============================================================================

/// <summary>Sends an emoji reaction to a specific received message.</summary>
public sealed record SendWhatsAppReactionRequest : WhatsAppBaseRequest
{
    /// <summary>The <c>wamid.*</c> message ID to react to.</summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// The emoji character to react with (e.g., <c>"👍"</c>).
    /// Pass an empty string <c>""</c> to remove an existing reaction.
    /// </summary>
    public required string Emoji { get; init; }
}

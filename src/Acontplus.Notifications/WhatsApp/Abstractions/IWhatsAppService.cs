using Acontplus.Notifications.WhatsApp.Models;

namespace Acontplus.Notifications.WhatsApp.Abstractions;

/// <summary>
/// WhatsApp Cloud API service for sending messages via Meta's Graph API.
/// Supports text, templates, media (image/document/audio/video/sticker),
/// interactive messages (buttons/list/CTA), reactions, read receipts and webhook validation.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a plain text message.
    /// Requires an active 24-hour conversation window with the recipient.
    /// </summary>
    Task<WhatsAppResult> SendTextAsync(SendWhatsAppTextRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a pre-approved message template.
    /// Works at any time — no 24-hour window required.
    /// Supports header (text/image/document/video), body params, and button params.
    /// </summary>
    Task<WhatsAppResult> SendTemplateAsync(SendWhatsAppTemplateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a media message (image, document, audio, video or sticker).
    /// Provide either <see cref="SendWhatsAppMediaRequest.MediaUrl"/> or <see cref="SendWhatsAppMediaRequest.MediaId"/>.
    /// Requires an active 24-hour window unless combined with a template.
    /// </summary>
    Task<WhatsAppResult> SendMediaAsync(SendWhatsAppMediaRequest request, CancellationToken ct = default);

    /// <summary>
    /// Uploads a media file to the WhatsApp Media API and returns a reusable media ID.
    /// Use the returned ID in <see cref="SendWhatsAppMediaRequest.MediaId"/> or template parameters.
    /// </summary>
    Task<string?> UploadMediaAsync(WhatsAppMediaUpload upload, WhatsAppCredentials? credentials = null, CancellationToken ct = default);

    /// <summary>Sends a location pin message.</summary>
    Task<WhatsAppResult> SendLocationAsync(SendWhatsAppLocationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends an interactive message:
    /// quick-reply buttons, scrollable list menu, or call-to-action URL button.
    /// </summary>
    Task<WhatsAppResult> SendInteractiveAsync(SendWhatsAppInteractiveRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends an emoji reaction to a received message.
    /// Pass an empty <see cref="SendWhatsAppReactionRequest.Emoji"/> to remove an existing reaction.
    /// </summary>
    Task<WhatsAppResult> SendReactionAsync(SendWhatsAppReactionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Marks a received message as read (shows double blue ticks to the sender).
    /// </summary>
    Task<bool> MarkAsReadAsync(string messageId, WhatsAppCredentials? credentials = null, CancellationToken ct = default);

    /// <summary>
    /// Validates the <c>X-Hub-Signature-256</c> header from an incoming Meta webhook request.
    /// Uses HMAC-SHA256 with the configured <see cref="WhatsAppOptions.AppSecret"/>.
    /// </summary>
    /// <param name="xHubSignature256">Value of the X-Hub-Signature-256 header (format: "sha256=...").</param>
    /// <param name="rawRequestBody">Raw UTF-8 request body string.</param>
    /// <param name="appSecret">
    /// Optional override for the app secret. When null, uses <see cref="WhatsAppOptions.AppSecret"/>.
    /// </param>
    bool ValidateWebhookSignature(string xHubSignature256, string rawRequestBody, string? appSecret = null);
}

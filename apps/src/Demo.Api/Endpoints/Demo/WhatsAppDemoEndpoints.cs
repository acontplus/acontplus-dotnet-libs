namespace Demo.Api.Endpoints.Demo;

/// <summary>
/// Demo endpoints for the WhatsApp Cloud API service (Acontplus.Notifications v1.6.0).
/// All endpoints require real Meta credentials to be set in appsettings (WhatsApp section).
/// See appsettings.Example.json for the full configuration reference.
/// </summary>
public static class WhatsAppDemoEndpoints
{
    public static RouteGroupBuilder MapWhatsAppDemoEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/text", SendText)
            .WithName("WhatsApp_SendText")
            .WithSummary("Send plain text")
            .WithDescription("Sends a plain text message. Requires an active 24-hour conversation window with the recipient.");

        group.MapPost("/template", SendTemplate)
            .WithName("WhatsApp_SendTemplate")
            .WithSummary("Send template message")
            .WithDescription("Sends a pre-approved template. Works at any time — no 24-hour window required.");

        group.MapPost("/document-template", SendDocumentTemplate)
            .WithName("WhatsApp_SendDocumentTemplate")
            .WithSummary("Upload document + send template")
            .WithDescription("Uploads a document file to WhatsApp Media API, then sends it via template header. Two-step: upload → send.");

        group.MapPost("/media-url", SendMediaByUrl)
            .WithName("WhatsApp_SendMediaByUrl")
            .WithSummary("Send media by URL")
            .WithDescription("Sends an image, document, audio or video via a public HTTPS URL. Requires 24-hour window.");

        group.MapPost("/location", SendLocation)
            .WithName("WhatsApp_SendLocation")
            .WithSummary("Send location pin")
            .WithDescription("Sends a map location with optional name and address.");

        group.MapPost("/interactive-buttons", SendInteractiveButtons)
            .WithName("WhatsApp_SendInteractiveButtons")
            .WithSummary("Send quick-reply buttons")
            .WithDescription("Sends up to 3 quick-reply buttons. Button IDs are returned in the webhook callback.");

        group.MapPost("/interactive-list", SendInteractiveList)
            .WithName("WhatsApp_SendInteractiveList")
            .WithSummary("Send scrollable list menu")
            .WithDescription("Sends a list message with sections and selectable rows (up to 10 rows total).");

        group.MapPost("/reaction", SendReaction)
            .WithName("WhatsApp_SendReaction")
            .WithSummary("React to a message")
            .WithDescription("Sends an emoji reaction to a received message. Pass empty emoji to remove an existing reaction.");

        group.MapPost("/mark-read", MarkAsRead)
            .WithName("WhatsApp_MarkAsRead")
            .WithSummary("Mark message as read")
            .WithDescription("Marks a received message as read (shows double blue ticks to the sender).");

        group.MapPost("/upload-media", UploadMedia)
            .DisableAntiforgery()
            .WithName("WhatsApp_UploadMedia")
            .WithSummary("Upload media file")
            .WithDescription("Uploads a file to the WhatsApp Media API. Returns a reusable media ID valid for 30 days.");

        group.MapPost("/webhook/verify", VerifyWebhook)
            .WithName("WhatsApp_VerifyWebhook")
            .WithSummary("Simulate webhook signature validation")
            .WithDescription("Validates an X-Hub-Signature-256 header against a raw request body using HMAC-SHA256.");

        return group;
    }

    // =========================================================================
    // Handlers
    // =========================================================================

    private static async Task<IResult> SendText(
        [FromBody] SendTextDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        var result = await whatsApp.SendTextAsync(new()
        {
            To = req.To,
            Body = req.Body,
            PreviewUrl = req.PreviewUrl,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendTemplate(
        [FromBody] SendTemplateDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        var components = new List<WhatsAppTemplateComponent>();

        if (req.BodyParams?.Count > 0)
            components.Add(WhatsAppTemplateComponent.Body([.. req.BodyParams]));

        var result = await whatsApp.SendTemplateAsync(new()
        {
            To = req.To,
            TemplateName = req.TemplateName,
            LanguageCode = req.LanguageCode ?? "en_US",
            Components = components.Count > 0 ? components : null,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendDocumentTemplate(
        IFormFile file,
        [FromForm] string to,
        [FromForm] string templateName,
        [FromForm] string? languageCode,
        [FromForm] string? bodyParamsCsv,
        [FromForm] string? phoneNumberId,
        [FromForm] string? accessToken,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("File is required.");

        WhatsAppCredentials? creds = null;
        if (!string.IsNullOrWhiteSpace(phoneNumberId) && !string.IsNullOrWhiteSpace(accessToken))
            creds = WhatsAppCredentials.Inline(phoneNumberId, accessToken);

        // Step 1: Upload document
        await using var stream = file.OpenReadStream();

        var mediaId = await whatsApp.UploadMediaAsync(new()
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream"
        }, creds, ct);

        if (string.IsNullOrEmpty(mediaId))
            return Results.Problem("Media upload failed. Check credentials and file type.");

        // Step 2: Build components
        var bodyParams = bodyParamsCsv?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

        var components = new List<WhatsAppTemplateComponent>
        {
            WhatsAppTemplateComponent.HeaderDocument(mediaId, file.FileName, isId: true)
        };

        if (bodyParams.Length > 0)
            components.Add(WhatsAppTemplateComponent.Body(bodyParams));

        // Step 3: Send template
        var result = await whatsApp.SendTemplateAsync(new()
        {
            To = to,
            TemplateName = templateName,
            LanguageCode = languageCode ?? "en_US",
            Components = components,
            Credentials = creds
        }, ct);

        return ToResult(result, new { mediaId });
    }

    private static async Task<IResult> SendMediaByUrl(
        [FromBody] SendMediaUrlDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        if (!Enum.TryParse<WhatsAppMediaType>(req.MediaType, ignoreCase: true, out var mediaType))
            return Results.BadRequest($"Invalid MediaType. Valid: {string.Join(", ", Enum.GetNames<WhatsAppMediaType>())}");

        var result = await whatsApp.SendMediaAsync(new()
        {
            To = req.To,
            MediaType = mediaType,
            MediaUrl = req.MediaUrl,
            Caption = req.Caption,
            Filename = req.Filename,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendLocation(
        [FromBody] SendLocationDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        var result = await whatsApp.SendLocationAsync(new()
        {
            To = req.To,
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            Name = req.Name,
            Address = req.Address,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendInteractiveButtons(
        [FromBody] SendInteractiveButtonsDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        if (req.Buttons is not { Count: > 0 })
            return Results.BadRequest("At least one button is required.");

        if (req.Buttons.Count > 3)
            return Results.BadRequest("Maximum 3 quick-reply buttons allowed.");

        var buttons = req.Buttons
            .Select(b => new WhatsAppReplyButton { Id = b.Id, Title = b.Title })
            .ToList();

        var result = await whatsApp.SendInteractiveAsync(new()
        {
            To = req.To,
            InteractiveType = WhatsAppInteractiveType.Button,
            BodyText = req.BodyText,
            HeaderText = req.HeaderText,
            FooterText = req.FooterText,
            ReplyButtons = buttons,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendInteractiveList(
        [FromBody] SendInteractiveListDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        if (req.Sections is not { Count: > 0 })
            return Results.BadRequest("At least one section with rows is required.");

        var sections = req.Sections.Select(s => new WhatsAppListSection
        {
            Title = s.Title,
            Rows = s.Rows.Select(r => new WhatsAppListRow
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description
            }).ToList()
        }).ToList();

        var result = await whatsApp.SendInteractiveAsync(new()
        {
            To = req.To,
            InteractiveType = WhatsAppInteractiveType.List,
            BodyText = req.BodyText,
            HeaderText = req.HeaderText,
            FooterText = req.FooterText,
            ListButtonLabel = req.ListButtonLabel ?? "Options",
            ListSections = sections,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> SendReaction(
        [FromBody] SendReactionDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        var result = await whatsApp.SendReactionAsync(new()
        {
            To = req.To,
            MessageId = req.MessageId,
            Emoji = req.Emoji,
            Credentials = BuildCredentials(req)
        }, ct);

        return ToResult(result);
    }

    private static async Task<IResult> MarkAsRead(
        [FromBody] MarkAsReadDemoRequest req,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        WhatsAppCredentials? creds = null;
        if (!string.IsNullOrWhiteSpace(req.PhoneNumberId) && !string.IsNullOrWhiteSpace(req.AccessToken))
            creds = WhatsAppCredentials.Inline(req.PhoneNumberId, req.AccessToken);

        var success = await whatsApp.MarkAsReadAsync(req.MessageId, creds, ct);

        return success
            ? Results.Ok(new { success = true, messageId = req.MessageId })
            : Results.Problem("Failed to mark message as read. Verify the message ID and credentials.");
    }

    private static async Task<IResult> UploadMedia(
        IFormFile file,
        [FromForm] string? phoneNumberId,
        [FromForm] string? accessToken,
        IWhatsAppService whatsApp,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("File is required.");

        WhatsAppCredentials? creds = null;
        if (!string.IsNullOrWhiteSpace(phoneNumberId) && !string.IsNullOrWhiteSpace(accessToken))
            creds = WhatsAppCredentials.Inline(phoneNumberId, accessToken);

        await using var stream = file.OpenReadStream();

        var mediaId = await whatsApp.UploadMediaAsync(new()
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType ?? "application/octet-stream"
        }, creds, ct);

        return string.IsNullOrEmpty(mediaId)
            ? Results.Problem("Media upload failed. Check credentials and file type.")
            : Results.Ok(new
            {
                success = true,
                mediaId,
                fileName = file.FileName,
                contentType = file.ContentType,
                sizeBytes = file.Length,
                note = "Media IDs are reusable for 30 days. Use in template header or SendMedia endpoints."
            });
    }

    private static Task<IResult> VerifyWebhook(
        [FromBody] WebhookValidationDemoRequest req,
        IWhatsAppService whatsApp)
    {
        var isValid = whatsApp.ValidateWebhookSignature(
            req.XHubSignature256,
            req.RawBody,
            req.AppSecret);

        return Task.FromResult(isValid
            ? Results.Ok(new { valid = true, message = "Signature is valid." })
            : (IResult)Results.UnprocessableEntity(new { valid = false, message = "Invalid signature. Check AppSecret and that the body is the raw UTF-8 string." }));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static WhatsAppCredentials? BuildCredentials(IInlineCredentials req) =>
        !string.IsNullOrWhiteSpace(req.PhoneNumberId) && !string.IsNullOrWhiteSpace(req.AccessToken)
            ? WhatsAppCredentials.Inline(req.PhoneNumberId!, req.AccessToken!)
            : !string.IsNullOrWhiteSpace(req.AccountName)
                ? WhatsAppCredentials.FromAccount(req.AccountName!)
                : null;

    private static IResult ToResult(WhatsAppResult result, object? extra = null) =>
        result.IsSuccess
            ? Results.Ok(new
            {
                success = true,
                messageId = result.MessageId,
                recipientWaId = result.RecipientWaId,
                extra
            })
            : Results.UnprocessableEntity(new
            {
                success = false,
                error = new
                {
                    code = result.Error?.Code,
                    subcode = result.Error?.ErrorSubcode,
                    message = result.Error?.Message,
                    type = result.Error?.Type,
                    fbtraceId = result.Error?.FbtraceId,
                    userMessage = result.Error?.UserMessage
                }
            });
}

// =============================================================================
// Request models (scoped to this demo — not part of the library)
// =============================================================================

/// <summary>Allows per-request credential override without touching appsettings.</summary>
internal interface IInlineCredentials
{
    string? PhoneNumberId { get; }
    string? AccessToken { get; }
    string? AccountName { get; }
}

internal sealed class SendTextDemoRequest : IInlineCredentials
{
    /// <summary>Recipient phone number (E.164 or local with leading zero).</summary>
    public required string To { get; set; }
    /// <summary>Message body text (max 4096 chars).</summary>
    public required string Body { get; set; }
    /// <summary>Render URL preview card in the message.</summary>
    public bool PreviewUrl { get; set; } = false;

    // Inline credential override — leave blank to use appsettings defaults.
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class SendTemplateDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    /// <summary>Exact template name as registered in WhatsApp Business Manager.</summary>
    public required string TemplateName { get; set; }
    /// <summary>Language code (e.g. "en_US", "es_EC").</summary>
    public string? LanguageCode { get; set; }
    /// <summary>Body parameter values mapping to {{1}}, {{2}}, etc. in the template.</summary>
    public List<string>? BodyParams { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class SendMediaUrlDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    /// <summary>Image | Document | Audio | Video | Sticker</summary>
    public required string MediaType { get; set; }
    /// <summary>Publicly accessible HTTPS URL for the media.</summary>
    public required string MediaUrl { get; set; }
    public string? Caption { get; set; }
    /// <summary>Filename shown to recipient (documents only).</summary>
    public string? Filename { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class SendLocationDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class SendInteractiveButtonsDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    public required string BodyText { get; set; }
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    /// <summary>1–3 buttons. Each must have a unique Id (max 256 chars) and a Title (max 20 chars).</summary>
    public required List<ButtonDemoItem> Buttons { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class ButtonDemoItem
{
    public required string Id { get; set; }
    public required string Title { get; set; }
}

internal sealed class SendInteractiveListDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    public required string BodyText { get; set; }
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    /// <summary>Label on the button that opens the list (max 20 chars).</summary>
    public string? ListButtonLabel { get; set; }
    public required List<SectionDemoItem> Sections { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class SectionDemoItem
{
    public string? Title { get; set; }
    public required List<RowDemoItem> Rows { get; set; }
}

internal sealed class RowDemoItem
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
}

internal sealed class SendReactionDemoRequest : IInlineCredentials
{
    public required string To { get; set; }
    /// <summary>wamid.* ID of the received message to react to.</summary>
    public required string MessageId { get; set; }
    /// <summary>Emoji character (e.g. "👍"). Empty string removes an existing reaction.</summary>
    public required string Emoji { get; set; }

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string? AccountName { get; set; }
}

internal sealed class MarkAsReadDemoRequest
{
    /// <summary>wamid.* ID of the received message to mark as read.</summary>
    public required string MessageId { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
}

internal sealed class WebhookValidationDemoRequest
{
    /// <summary>Full value of the X-Hub-Signature-256 header (format: "sha256=base16...").</summary>
    public required string XHubSignature256 { get; set; }
    /// <summary>Raw UTF-8 request body string (exactly as received, before any parsing).</summary>
    public required string RawBody { get; set; }
    /// <summary>Optional app secret override. When null, uses WhatsApp:AppSecret from appsettings.</summary>
    public string? AppSecret { get; set; }
}

using Acontplus.Notifications.WhatsApp.Abstractions;
using Acontplus.Notifications.WhatsApp.Internal;
using Acontplus.Notifications.WhatsApp.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Acontplus.Notifications.WhatsApp.Services;

/// <summary>
/// WhatsApp Cloud API service implementation targeting Meta Graph API v23.0.
/// Registered as a singleton; uses <see cref="IHttpClientFactory"/> for thread-safe,
/// lifecycle-managed HTTP connections with built-in resilience (retry + circuit breaker).
/// </summary>
public sealed class WhatsAppService : IWhatsAppService
{
    // Named HttpClient — matches the name used in AddWhatsAppService() DI registration.
    internal const string HttpClientName = "WhatsApp";

    // Reused serializer options: case-insensitive for deserialization + null-skip for serialization.
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly WhatsAppOptions _options;

    /// <summary>Creates a new <see cref="WhatsAppService"/> instance.</summary>
    public WhatsAppService(
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppService> logger,
        IOptions<WhatsAppOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendTextAsync(
        SendWhatsAppTextRequest request,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = "text",
            ["text"] = new JsonObject
            {
                ["body"] = request.Body,
                ["preview_url"] = request.PreviewUrl
            }
        };

        AddContext(payload, request.ContextMessageId);

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendTemplateAsync(
        SendWhatsAppTemplateRequest request,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);

        var template = new JsonObject
        {
            ["name"] = request.TemplateName,
            ["language"] = new JsonObject { ["code"] = request.LanguageCode }
        };

        if (request.Components is { Count: > 0 })
        {
            var components = new JsonArray();
            foreach (var c in request.Components)
                components.Add(BuildTemplateComponent(c));
            template["components"] = components;
        }

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = "template",
            ["template"] = template
        };

        AddContext(payload, request.ContextMessageId);

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendMediaAsync(
        SendWhatsAppMediaRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.MediaUrl) && string.IsNullOrWhiteSpace(request.MediaId))
            return WhatsAppResult.Failure(-1, "Either MediaUrl or MediaId must be provided.");

        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);
        var typeName = request.MediaType.ToString().ToLowerInvariant();

        var mediaObj = new JsonObject();

        if (!string.IsNullOrWhiteSpace(request.MediaId))
            mediaObj["id"] = request.MediaId;
        else
            mediaObj["link"] = request.MediaUrl;

        // Captions are only valid for image, video, document
        if (request.Caption is not null
            && request.MediaType is not WhatsAppMediaType.Audio
            && request.MediaType is not WhatsAppMediaType.Sticker)
        {
            mediaObj["caption"] = request.Caption;
        }

        // Filename is only valid for documents
        if (request.Filename is not null && request.MediaType is WhatsAppMediaType.Document)
            mediaObj["filename"] = request.Filename;

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = typeName,
            [typeName] = mediaObj
        };

        AddContext(payload, request.ContextMessageId);

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<string?> UploadMediaAsync(
        WhatsAppMediaUpload upload,
        WhatsAppCredentials? credentials = null,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(credentials);

        try
        {
            using var multipart = new MultipartFormDataContent();

            // Do NOT wrap in using — the stream is owned by the caller.
            var streamContent = new StreamContent(upload.FileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(upload.ContentType);

            multipart.Add(streamContent, "file", upload.FileName);
            multipart.Add(new StringContent("whatsapp"), "messaging_product");
            multipart.Add(new StringContent(upload.ContentType), "type");

            _logger.LogInformation(
                "WhatsApp: uploading media '{FileName}' ({ContentType}) via PhoneNumberId={PhoneNumberId}",
                upload.FileName, upload.ContentType, creds.PhoneNumberId);

            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var req = BuildHttpRequest(HttpMethod.Post, $"{creds.PhoneNumberId}/media", creds, multipart);

            var response = await httpClient.SendAsync(req, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var parsed = JsonSerializer.Deserialize<MetaMediaUploadResponse>(body, DeserializeOptions);
                if (!string.IsNullOrEmpty(parsed?.id))
                {
                    _logger.LogInformation("WhatsApp: media uploaded — MediaId={MediaId}", parsed.id);
                    return parsed.id;
                }
            }

            _logger.LogError(
                "WhatsApp: media upload failed — {StatusCode}: {Body}",
                (int)response.StatusCode, body);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp: unexpected error uploading '{FileName}'", upload.FileName);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendLocationAsync(
        SendWhatsAppLocationRequest request,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);

        var location = new JsonObject
        {
            ["latitude"] = request.Latitude,
            ["longitude"] = request.Longitude
        };

        if (request.Name is not null) location["name"] = request.Name;
        if (request.Address is not null) location["address"] = request.Address;

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = "location",
            ["location"] = location
        };

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendInteractiveAsync(
        SendWhatsAppInteractiveRequest request,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = "interactive",
            ["interactive"] = BuildInteractiveNode(request)
        };

        AddContext(payload, request.ContextMessageId);

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<WhatsAppResult> SendReactionAsync(
        SendWhatsAppReactionRequest request,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(request.Credentials);
        var phone = NormalizePhone(request.To);

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["recipient_type"] = "individual",
            ["to"] = phone,
            ["type"] = "reaction",
            ["reaction"] = new JsonObject
            {
                ["message_id"] = request.MessageId,
                ["emoji"] = request.Emoji
            }
        };

        return await SendPayloadAsync(payload, creds, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(
        string messageId,
        WhatsAppCredentials? credentials = null,
        CancellationToken ct = default)
    {
        var creds = ResolveCredentials(credentials);

        var payload = new JsonObject
        {
            ["messaging_product"] = "whatsapp",
            ["status"] = "read",
            ["message_id"] = messageId
        };

        try
        {
            var json = payload.ToJsonString();
            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = BuildHttpRequest(HttpMethod.Post, $"{creds.PhoneNumberId}/messages", creds, content);

            var response = await httpClient.SendAsync(req, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("WhatsApp: message {MessageId} marked as read", messageId);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "WhatsApp: MarkAsRead failed — {StatusCode}: {Body}",
                (int)response.StatusCode, body);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp: error marking message {MessageId} as read", messageId);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool ValidateWebhookSignature(
        string xHubSignature256,
        string rawRequestBody,
        string? appSecret = null)
    {
        var secret = appSecret ?? _options.AppSecret;

        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning(
                "WhatsApp: webhook signature validation skipped — AppSecret is not configured.");
            return false;
        }

        const string Prefix = "sha256=";

        if (!xHubSignature256.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "WhatsApp: invalid signature format — expected 'sha256=<hex>'");
            return false;
        }

        ReadOnlySpan<char> providedHex = xHubSignature256.AsSpan(Prefix.Length);

        var bodyBytes = Encoding.UTF8.GetBytes(rawRequestBody);
        var keyBytes = Encoding.UTF8.GetBytes(secret);

        // HMACSHA256.HashData is allocation-efficient in .NET 6+
        var computedHash = HMACSHA256.HashData(keyBytes, bodyBytes);
        var computedHex = Convert.ToHexString(computedHash).AsSpan();

        return providedHex.Equals(computedHex, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    private async Task<WhatsAppResult> SendPayloadAsync(
        JsonObject payload,
        ResolvedCredentials creds,
        CancellationToken ct)
    {
        var messageType = payload["type"]?.GetValue<string>() ?? "unknown";

        try
        {
            var json = payload.ToJsonString();

            _logger.LogDebug(
                "WhatsApp: sending '{Type}' message via PhoneNumberId={PhoneNumberId}",
                messageType, creds.PhoneNumberId);

            using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var req = BuildHttpRequest(HttpMethod.Post, $"{creds.PhoneNumberId}/messages", creds, content);

            var response = await httpClient.SendAsync(req, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            var apiResponse = JsonSerializer.Deserialize<MetaMessagesResponse>(body, DeserializeOptions);

            if (response.IsSuccessStatusCode && apiResponse?.messages?.Count > 0)
            {
                var msgId = apiResponse.messages[0].id!;
                var waId = apiResponse.contacts?.FirstOrDefault()?.wa_id;

                _logger.LogInformation(
                    "WhatsApp: '{Type}' sent — MessageId={MessageId} To={WaId}",
                    messageType, msgId, waId);

                return WhatsAppResult.Success(msgId, waId);
            }

            var err = apiResponse?.error;

            _logger.LogError(
                "WhatsApp: API error [{Code}/{Subcode}] {Message} — Type={ErrorType} Trace={Trace}",
                err?.code, err?.error_subcode, err?.message, err?.type, err?.fbtrace_id);

            return WhatsAppResult.Failure(
                err?.code ?? (int)response.StatusCode,
                err?.message ?? response.ReasonPhrase ?? "Unknown error",
                err?.type,
                err?.error_subcode,
                err?.fbtrace_id,
                err?.error_user_title ?? err?.error_user_msg);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("WhatsApp: '{Type}' send was cancelled", messageType);
            return WhatsAppResult.Failure(-1, "Operation cancelled.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "WhatsApp: HTTP error sending '{Type}'", messageType);
            return WhatsAppResult.Failure(-2, $"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp: unexpected error sending '{Type}'", messageType);
            return WhatsAppResult.Failure(-3, ex.Message);
        }
    }

    private HttpRequestMessage BuildHttpRequest(
        HttpMethod method,
        string path,
        ResolvedCredentials creds,
        HttpContent? content = null)
    {
        var baseUrl = $"{_options.BaseUrl.TrimEnd('/')}/{_options.ApiVersion}/";
        var uri = new Uri(new Uri(baseUrl, UriKind.Absolute), path);

        var msg = new HttpRequestMessage(method, uri);
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", creds.AccessToken);

        if (content is not null)
            msg.Content = content;

        return msg;
    }

    /// <summary>
    /// Resolves effective credentials following priority order:
    /// per-request named account → per-request inline → default from options.
    /// </summary>
    private ResolvedCredentials ResolveCredentials(WhatsAppCredentials? perRequest)
    {
        if (perRequest is not null)
        {
            // 1. Named account
            if (!string.IsNullOrEmpty(perRequest.AccountName))
            {
                if (_options.Accounts.TryGetValue(perRequest.AccountName, out var account))
                    return new ResolvedCredentials(account.PhoneNumberId, account.AccessToken);

                _logger.LogWarning(
                    "WhatsApp: named account '{AccountName}' not found — falling back to default.",
                    perRequest.AccountName);
            }
            // 2. Inline credentials
            else if (!string.IsNullOrEmpty(perRequest.PhoneNumberId)
                     && !string.IsNullOrEmpty(perRequest.AccessToken))
            {
                return new ResolvedCredentials(perRequest.PhoneNumberId, perRequest.AccessToken);
            }
        }

        // 3. Default from options
        return new ResolvedCredentials(_options.PhoneNumberId, _options.AccessToken);
    }

    /// <summary>
    /// Normalizes a phone number to digits-only, applying <see cref="WhatsAppOptions.DefaultCountryCode"/>
    /// when the number starts with a leading zero (e.g., Ecuadorian 09xxxxxxxx → 593 9xxxxxxxx).
    /// </summary>
    private string NormalizePhone(string phoneNumber)
    {
        var sb = new StringBuilder(phoneNumber.Length);

        foreach (var c in phoneNumber.AsSpan())
        {
            if (char.IsAsciiDigit(c))
                sb.Append(c);
        }

        var digits = sb.ToString();

        if (digits.StartsWith('0') && !string.IsNullOrEmpty(_options.DefaultCountryCode))
            return string.Concat(_options.DefaultCountryCode, digits.AsSpan(1));

        return digits;
    }

    private static void AddContext(JsonObject payload, string? messageId)
    {
        if (!string.IsNullOrEmpty(messageId))
            payload["context"] = new JsonObject { ["message_id"] = messageId };
    }

    // -------------------------------------------------------------------------
    // Template payload builders
    // -------------------------------------------------------------------------

    private static JsonObject BuildTemplateComponent(WhatsAppTemplateComponent component)
    {
        var node = new JsonObject { ["type"] = component.Type };

        if (component.SubType is not null) node["sub_type"] = component.SubType;
        if (component.Index is not null) node["index"] = component.Index.Value.ToString();

        var parameters = new JsonArray();
        foreach (var param in component.Parameters)
            parameters.Add(BuildTemplateParameter(param));

        node["parameters"] = parameters;
        return node;
    }

    private static JsonObject BuildTemplateParameter(WhatsAppTemplateParameter param)
    {
        var node = new JsonObject { ["type"] = param.Type };

        switch (param.Type.ToLowerInvariant())
        {
            case "text":
            case "payload":
                node["text"] = param.Text;
                break;

            case "image" when param.Image is not null:
                node["image"] = BuildMediaObjectNode(param.Image);
                break;

            case "document" when param.Document is not null:
                node["document"] = BuildMediaObjectNode(param.Document);
                break;

            case "video" when param.Video is not null:
                node["video"] = BuildMediaObjectNode(param.Video);
                break;
        }

        return node;
    }

    private static JsonObject BuildMediaObjectNode(WhatsAppMediaObject media)
    {
        var node = new JsonObject();
        if (!string.IsNullOrEmpty(media.Id)) node["id"] = media.Id;
        if (!string.IsNullOrEmpty(media.Link)) node["link"] = media.Link;
        if (!string.IsNullOrEmpty(media.Filename)) node["filename"] = media.Filename;
        if (!string.IsNullOrEmpty(media.Caption)) node["caption"] = media.Caption;
        return node;
    }

    // -------------------------------------------------------------------------
    // Interactive payload builders
    // -------------------------------------------------------------------------

    private static JsonObject BuildInteractiveNode(SendWhatsAppInteractiveRequest request)
    {
        var typeStr = request.InteractiveType switch
        {
            WhatsAppInteractiveType.Button => "button",
            WhatsAppInteractiveType.List => "list",
            WhatsAppInteractiveType.CtaUrl => "cta_url",
            _ => throw new ArgumentOutOfRangeException(nameof(request.InteractiveType))
        };

        var node = new JsonObject
        {
            ["type"] = typeStr,
            ["body"] = new JsonObject { ["text"] = request.BodyText }
        };

        if (!string.IsNullOrEmpty(request.HeaderText))
            node["header"] = new JsonObject { ["type"] = "text", ["text"] = request.HeaderText };

        if (!string.IsNullOrEmpty(request.FooterText))
            node["footer"] = new JsonObject { ["text"] = request.FooterText };

        node["action"] = request.InteractiveType switch
        {
            WhatsAppInteractiveType.Button => BuildButtonAction(request.ReplyButtons),
            WhatsAppInteractiveType.List => BuildListAction(request.ListButtonLabel, request.ListSections),
            WhatsAppInteractiveType.CtaUrl => BuildCtaUrlAction(request.CtaDisplayText, request.CtaUrl),
            _ => new JsonObject()
        };

        return node;
    }

    private static JsonObject BuildButtonAction(IReadOnlyList<WhatsAppReplyButton>? buttons)
    {
        var arr = new JsonArray();

        if (buttons is not null)
        {
            foreach (var btn in buttons)
            {
                arr.Add(new JsonObject
                {
                    ["type"] = "reply",
                    ["reply"] = new JsonObject { ["id"] = btn.Id, ["title"] = btn.Title }
                });
            }
        }

        return new JsonObject { ["buttons"] = arr };
    }

    private static JsonObject BuildListAction(
        string? buttonLabel,
        IReadOnlyList<WhatsAppListSection>? sections)
    {
        var sectionsArr = new JsonArray();

        if (sections is not null)
        {
            foreach (var section in sections)
            {
                var sectionNode = new JsonObject();
                if (section.Title is not null) sectionNode["title"] = section.Title;

                var rows = new JsonArray();
                foreach (var row in section.Rows)
                {
                    var rowNode = new JsonObject { ["id"] = row.Id, ["title"] = row.Title };
                    if (row.Description is not null) rowNode["description"] = row.Description;
                    rows.Add(rowNode);
                }

                sectionNode["rows"] = rows;
                sectionsArr.Add(sectionNode);
            }
        }

        return new JsonObject
        {
            ["button"] = buttonLabel ?? "Options",
            ["sections"] = sectionsArr
        };
    }

    private static JsonObject BuildCtaUrlAction(string? displayText, string? url) =>
        new()
        {
            ["name"] = "cta_url",
            ["parameters"] = new JsonObject
            {
                ["display_text"] = displayText ?? "Open",
                ["url"] = url ?? string.Empty
            }
        };

    // -------------------------------------------------------------------------
    // Internal value type — avoids boxing/heap allocation for credential resolution
    // -------------------------------------------------------------------------

    private readonly record struct ResolvedCredentials(string PhoneNumberId, string AccessToken);
}

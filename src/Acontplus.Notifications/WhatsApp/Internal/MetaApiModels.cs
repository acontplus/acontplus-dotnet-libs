// These types mirror the raw Meta Graph API JSON schema.
// All properties use snake_case to match Meta's naming without requiring a naming policy.
// They are intentionally internal — consumers should use WhatsAppResult / WhatsAppApiError.

namespace Acontplus.Notifications.WhatsApp.Internal;

internal sealed record MetaMessagesResponse
{
    public string? messaging_product { get; init; }
    public List<MetaContact>? contacts { get; init; }
    public List<MetaMessage>? messages { get; init; }
    public MetaApiErrorPayload? error { get; init; }
    public bool? success { get; init; } // read-receipt / typing-indicator responses
}

internal sealed record MetaContact
{
    public string? input { get; init; }
    public string? wa_id { get; init; }
}

internal sealed record MetaMessage
{
    public string? id { get; init; }
    public string? message_status { get; init; }
}

internal sealed record MetaApiErrorPayload
{
    public string? message { get; init; }
    public string? type { get; init; }
    public int code { get; init; }
    public int? error_subcode { get; init; }
    public string? fbtrace_id { get; init; }
    public string? error_user_title { get; init; }
    public string? error_user_msg { get; init; }
}

internal sealed record MetaMediaUploadResponse
{
    public string? id { get; init; }
}

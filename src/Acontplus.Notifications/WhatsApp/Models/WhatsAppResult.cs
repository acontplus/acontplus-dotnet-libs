namespace Acontplus.Notifications.WhatsApp.Models;

/// <summary>
/// Result of a WhatsApp Cloud API send or status operation.
/// Check <see cref="IsSuccess"/> before accessing <see cref="MessageId"/>.
/// </summary>
public sealed record WhatsAppResult
{
    /// <summary>Whether the API call succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The WhatsApp message ID (<c>wamid.*</c>) assigned by Meta.
    /// Only populated when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// The recipient's WhatsApp ID (normalized phone number) as confirmed by Meta.
    /// Only populated when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public string? RecipientWaId { get; init; }

    /// <summary>Error details when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public WhatsAppApiError? Error { get; init; }

    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a successful result.</summary>
    public static WhatsAppResult Success(string messageId, string? recipientWaId = null) =>
        new() { IsSuccess = true, MessageId = messageId, RecipientWaId = recipientWaId };

    /// <summary>Creates a failure result from error components.</summary>
    public static WhatsAppResult Failure(
        int code,
        string message,
        string? type = null,
        int? errorSubcode = null,
        string? fbtraceId = null,
        string? userMessage = null) =>
        new()
        {
            IsSuccess = false,
            Error = new WhatsAppApiError(code, message)
            {
                Type = type,
                ErrorSubcode = errorSubcode,
                FbtraceId = fbtraceId,
                UserMessage = userMessage
            }
        };

    /// <summary>Creates a failure result from a structured error object.</summary>
    public static WhatsAppResult Failure(WhatsAppApiError error) =>
        new() { IsSuccess = false, Error = error };

    /// <inheritdoc/>
    public override string ToString() =>
        IsSuccess
            ? $"OK  wamid={MessageId} wa_id={RecipientWaId}"
            : $"ERR [{Error?.Code}/{Error?.ErrorSubcode}] {Error?.Message}";
}

/// <summary>Structured error returned by the Meta Graph API or the service itself.</summary>
/// <param name="Code">Meta error code (negative values are library-internal codes).</param>
/// <param name="Message">Error message string.</param>
public sealed record WhatsAppApiError(int Code, string Message)
{
    /// <summary>Error type string from Meta (e.g., <c>"OAuthException"</c>).</summary>
    public string? Type { get; init; }

    /// <summary>Error subcode for finer classification (see Meta error code reference).</summary>
    public int? ErrorSubcode { get; init; }

    /// <summary>
    /// Facebook trace ID — include this when filing a Meta Developer Support ticket.
    /// </summary>
    public string? FbtraceId { get; init; }

    /// <summary>
    /// User-facing error title from Meta (can be displayed to end-users).
    /// Sourced from <c>error_user_title</c> in the Graph API response.
    /// </summary>
    public string? UserMessage { get; init; }
}

using Acontplus.Notifications.Models;

namespace Acontplus.Notifications.Abstractions;

/// <summary>
/// Service contract for sending email notifications.
/// </summary>
public interface IMailKitService
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    /// <param name="email">The email model containing recipients, content, and optional SMTP settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the email was sent successfully; otherwise, <c>false</c>.</returns>
    Task<bool> SendAsync(EmailModel email, CancellationToken ct = default);
}

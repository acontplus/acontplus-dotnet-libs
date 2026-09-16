namespace Acontplus.Notifications.Models;

/// <summary>
/// Model representing an email message and its delivery configuration.
/// </summary>
public class EmailModel
{
    /// <summary>
    /// Gets or sets the SMTP server host or IP address.
    /// </summary>
    public required string SmtpServer { get; set; }

    /// <summary>
    /// Gets or sets the SMTP server port number.
    /// </summary>
    public int SmtpPort { get; set; }

    /// <summary>
    /// Gets or sets the username for SMTP authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the encrypted password bytes for secure storage.
    /// </summary>
    public byte[]? EncryptedPassword { get; set; }

    /// <summary>
    /// Gets or sets the plaintext password for SMTP authentication.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets whether SSL/TLS encryption is used.
    /// </summary>
    public bool? UseSsl { get; set; }

    /// <summary>
    /// Gets or sets whether to check SSL/TLS certificate revocation with the certificate authority.
    /// Defaults to <see langword="true"/> to prevent MITM attacks (CWE-295).
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;

    /// <summary>
    /// Gets or sets the display name of the email sender.
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    public string? SenderEmail { get; set; }

    /// <summary>
    /// Gets or sets the primary recipient email address. Multiple addresses can be semicolon- or comma-separated.
    /// </summary>
    public required string RecipientEmail { get; set; }

    /// <summary>
    /// Gets or sets the carbon copy (CC) recipient email addresses.
    /// </summary>
    public string? Cc { get; set; }

    /// <summary>
    /// Gets or sets the email subject line.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Gets or sets whether the body content is HTML.
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// Gets or sets optional template name or identifier.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets optional logo URL or base64 data for templated emails.
    /// </summary>
    public string? Logo { get; set; }

    /// <summary>
    /// Gets or sets the body content of the email.
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Gets or sets attachments included with the email.
    /// </summary>
    public List<FileModel>? Files { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailModel"/> class.
    /// </summary>
    public EmailModel()
    {
        SmtpServer = string.Empty;
        Password = string.Empty;
        RecipientEmail = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }
}
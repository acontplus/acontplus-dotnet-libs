namespace Acontplus.Notifications.Models;

public class EmailModel
{
    public required string SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string? Username { get; set; }
    public byte[]? EncryptedPassword { get; set; } // Encrypted password
    public required string Password { get; set; }
    public bool? UseSsl { get; set; }
    /// <summary>
    /// Gets or sets whether to check SSL/TLS certificate revocation with the certificate authority.
    /// Defaults to <see langword="true"/> to prevent MITM attacks (CWE-295).
    /// </summary>
    public bool CheckCertificateRevocation { get; set; } = true;
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public required string RecipientEmail { get; set; }
    public string? Cc { get; set; }
    public required string Subject { get; set; }
    public bool IsHtml { get; set; }
    public string? Template { get; set; }
    public string? Logo { get; set; }
    public required string Body { get; set; }
    public List<FileModel>? Files { get; set; }

    public EmailModel()
    {
        SmtpServer = string.Empty;
        Password = string.Empty;
        RecipientEmail = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }
}
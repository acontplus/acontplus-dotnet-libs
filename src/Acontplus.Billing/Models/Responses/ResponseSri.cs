namespace Acontplus.Billing.Models.Responses;

/// <summary>
/// Model representing the response received from SRI web services.
/// </summary>
public class ResponseSri
{
    /// <summary>
    /// Gets or sets the authorization state (e.g. AUTORIZADO, NO AUTORIZADO).
    /// </summary>
    public string? Estado { get; set; }

    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public string? Identificador { get; set; }

    /// <summary>
    /// Gets or sets the SRI response message or description.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the raw XML response from SRI.
    /// </summary>
    public string? XmlSri { get; set; }

    /// <summary>
    /// Gets or sets the authorization code or access key returned by SRI.
    /// </summary>
    public string? CodigoAutorizacion { get; set; }

    /// <summary>
    /// Gets or sets the authorization timestamp string.
    /// </summary>
    public string? FechaAutorizacion { get; set; }

    /// <summary>
    /// Gets or sets additional informational messages from SRI.
    /// </summary>
    public string? InformacionAdicional { get; set; }

    /// <summary>
    /// Gets or sets the response message type (e.g. ERROR, INFORMATIVO).
    /// </summary>
    public string? Tipo { get; set; }
}
namespace Acontplus.Billing.Models.Responses;

/// <summary>
/// Base class for SRI web service response models.
/// </summary>
public abstract class ResponseBase
{
    /// <summary>
    /// Gets or sets the response status code or state from the SRI service.
    /// </summary>
    public string? Estado { get; set; }

    /// <summary>
    /// Gets or sets the descriptive response message.
    /// </summary>
    public string? Mensaje { get; set; }
}
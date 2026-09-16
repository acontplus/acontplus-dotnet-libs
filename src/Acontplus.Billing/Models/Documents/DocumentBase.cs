namespace Acontplus.Billing.Models.Documents;

/// <summary>
/// Base class for electronic billing document models.
/// </summary>
public abstract class DocumentBase
{
    /// <summary>
    /// Gets or sets the electronic document access key (clave de acceso).
    /// </summary>
    public string? ClaveAcceso { get; set; }
}
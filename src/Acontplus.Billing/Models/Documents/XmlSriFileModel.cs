namespace Acontplus.Billing.Models.Documents;

/// <summary>
/// Model representing an extracted SRI XML electronic document file and its parsed metadata.
/// </summary>
public class XmlSriFileModel
{
    /// <summary>
    /// Gets or sets the document type code (e.g. "01" for invoice).
    /// </summary>
    public string? CodDoc { get; set; }

    /// <summary>
    /// Gets or sets the 49-digit SRI access key (clave de acceso).
    /// </summary>
    public string? ClaveAcceso { get; set; }

    /// <summary>
    /// Gets or sets the document emission date string.
    /// </summary>
    public string? FechaEmision { get; set; }

    /// <summary>
    /// Gets or sets the electronic document schema version.
    /// </summary>
    public string? VersionComp { get; set; }

    /// <summary>
    /// Gets or sets the outer authorization XML document from SRI.
    /// </summary>
    public XmlDocument? XmlSri { get; set; }

    /// <summary>
    /// Gets or sets the inner signed electronic receipt XML document.
    /// </summary>
    public XmlDocument? XmlComprobante { get; set; }
}
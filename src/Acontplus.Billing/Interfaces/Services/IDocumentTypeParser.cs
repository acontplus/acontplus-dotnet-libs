using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Parser contract for specific electronic document types (e.g. invoice, credit note).
/// </summary>
public interface IDocumentTypeParser
{
    /// <summary>
    /// Parses the XML document into a specific document structure on the <see cref="ComprobanteElectronico"/>.
    /// </summary>
    /// <param name="xmlDocument">The raw XML document.</param>
    /// <param name="comprobante">The electronic document to populate.</param>
    /// <param name="errorMessage">Output error message if parsing fails.</param>
    /// <returns><c>true</c> if parsed successfully; otherwise <c>false</c>.</returns>
    bool Parse(XmlDocument xmlDocument, ComprobanteElectronico comprobante, out string errorMessage);
}
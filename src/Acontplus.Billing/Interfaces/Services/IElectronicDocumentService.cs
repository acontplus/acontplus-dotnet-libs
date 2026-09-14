using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Service contract for parsing electronic billing documents from SRI XML.
/// </summary>
public interface IElectronicDocumentService
{
    /// <summary>
    /// Attempts to parse an SRI XML document into a strongly-typed <see cref="ComprobanteElectronico"/>.
    /// </summary>
    /// <param name="xmlSri">The SRI XML document.</param>
    /// <param name="comprobante">The parsed electronic document if successful.</param>
    /// <param name="errorMessage">Output error message if parsing fails.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    bool TryParseDocument(XmlDocument xmlSri, out ComprobanteElectronico comprobante, out string errorMessage);
}
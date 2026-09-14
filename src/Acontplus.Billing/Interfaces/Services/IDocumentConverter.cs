using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Service contract for converting an electronic billing document into visual representations such as HTML.
/// </summary>
public interface IDocumentConverter
{
    /// <summary>
    /// Converts a <see cref="ComprobanteElectronico"/> into an HTML representation.
    /// </summary>
    /// <param name="comprobanteElectronico">The electronic receipt to convert.</param>
    /// <returns>HTML string representing the document.</returns>
    string CreateHtml(ComprobanteElectronico comprobanteElectronico);
}
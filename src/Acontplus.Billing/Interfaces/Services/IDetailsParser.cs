using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Parser contract for extracting line item details from an XML node into a <see cref="ComprobanteElectronico"/>.
/// </summary>
public interface IDetailsParser
{
    /// <summary>
    /// Parses line item details from the XML node and populates the electronic document.
    /// </summary>
    /// <param name="nodeDetails">The XML node containing details.</param>
    /// <param name="comprobante">The electronic document to populate.</param>
    void Parse(XmlNode nodeDetails, ComprobanteElectronico comprobante);
}
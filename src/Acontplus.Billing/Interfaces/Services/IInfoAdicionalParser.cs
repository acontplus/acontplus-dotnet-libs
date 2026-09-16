using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Parser contract for extracting additional fields (infoAdicional) from XML into <see cref="ComprobanteElectronico"/>.
/// </summary>
public interface IInfoAdicionalParser
{
    /// <summary>
    /// Parses additional information key-value pairs from the XML node.
    /// </summary>
    /// <param name="nodeInfoAdicional">The XML node containing infoAdicional elements.</param>
    /// <param name="comprobante">The electronic document to populate.</param>
    void Parse(XmlNode nodeInfoAdicional, ComprobanteElectronico comprobante);
}

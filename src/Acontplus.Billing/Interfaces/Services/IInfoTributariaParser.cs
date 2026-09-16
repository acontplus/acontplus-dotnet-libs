using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Parser contract for extracting tributary header metadata (infoTributaria) from XML into <see cref="ComprobanteElectronico"/>.
/// </summary>
public interface IInfoTributariaParser
{
    /// <summary>
    /// Parses tributary header fields from the XML node.
    /// </summary>
    /// <param name="nodeInfoTrib">The XML node containing infoTributaria elements.</param>
    /// <param name="comprobante">The electronic document to populate.</param>
    void Parse(XmlNode nodeInfoTrib, ComprobanteElectronico comprobante);
}

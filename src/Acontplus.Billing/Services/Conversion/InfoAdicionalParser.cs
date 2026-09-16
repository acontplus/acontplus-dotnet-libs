using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Conversion;

/// <summary>
/// Parser for extracting additional info key-value fields from XML into a <see cref="ComprobanteElectronico"/>.
/// </summary>
public class InfoAdicionalParser : IInfoAdicionalParser
{
    /// <inheritdoc />
    public void Parse(XmlNode nodeInfoAdicional, ComprobanteElectronico comprobante)
    {
        var infoAdicionals = (from XmlNode item in nodeInfoAdicional
                              select new InfoAdicional
                              {
                                  Nombre = (item.Attributes?.GetNamedItem("nombre"))?.Value ?? "",
                                  Valor = item.InnerText ?? ""
                              })
            .ToList();

        comprobante.CreateAdditionalInfo(infoAdicionals);
    }
}

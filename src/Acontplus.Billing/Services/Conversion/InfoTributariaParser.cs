using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Conversion;

/// <summary>
/// Parser for extracting tributary header metadata (infoTributaria) from XML into a <see cref="ComprobanteElectronico"/>.
/// </summary>
public class InfoTributariaParser : IInfoTributariaParser
{
    /// <inheritdoc />
    public void Parse(XmlNode nodeInfoTrib, ComprobanteElectronico comprobante)
    {
        comprobante.InfoTributaria = new InfoTributaria
        {
            Ambiente = nodeInfoTrib.SelectSingleNode("ambiente")?.InnerText ?? string.Empty,
            TipoEmision = nodeInfoTrib.SelectSingleNode("tipoEmision")?.InnerText ?? string.Empty,
            RazonSocial = nodeInfoTrib.SelectSingleNode("razonSocial")?.InnerText ?? string.Empty,
            NombreComercial = nodeInfoTrib.SelectSingleNode("nombreComercial")?.InnerText ?? string.Empty,
            Ruc = nodeInfoTrib.SelectSingleNode("ruc")?.InnerText ?? string.Empty,
            ClaveAcceso = nodeInfoTrib.SelectSingleNode("claveAcceso")?.InnerText ?? string.Empty,
            CodDoc = nodeInfoTrib.SelectSingleNode("codDoc")?.InnerText ?? string.Empty,
            Estab = nodeInfoTrib.SelectSingleNode("estab")?.InnerText ?? string.Empty,
            PtoEmi = nodeInfoTrib.SelectSingleNode("ptoEmi")?.InnerText ?? string.Empty,
            Secuencial = nodeInfoTrib.SelectSingleNode("secuencial")?.InnerText ?? string.Empty,
            DirMatriz = nodeInfoTrib.SelectSingleNode("dirMatriz")?.InnerText ?? string.Empty
        };
    }
}

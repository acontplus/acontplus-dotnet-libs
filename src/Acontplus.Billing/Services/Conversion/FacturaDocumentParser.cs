using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Conversion;

/// <summary>
/// Parser for electronic invoices (factura - codDoc 01).
/// </summary>
/// <param name="detailsParser">The item details parser.</param>
public class FacturaDocumentParser(IDetailsParser detailsParser) : IDocumentTypeParser
{
    private readonly IDetailsParser _detailsParser = detailsParser ?? throw new ArgumentNullException(nameof(detailsParser));

    /// <inheritdoc />
    public bool Parse(XmlDocument xmlDocument, ComprobanteElectronico comprobante, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            var nodeFact = xmlDocument.GetElementsByTagName("factura")[0];
            comprobante.VersionComp = nodeFact?.Attributes?["version"]?.Value ?? string.Empty;

            var nodeInfoFactura = xmlDocument.GetElementsByTagName("infoFactura")[0];
            if (nodeInfoFactura != null)
            {
                ParseInfoFactura(nodeInfoFactura, comprobante);
            }
            else
            {
                errorMessage = "No invoice information found in document";
                return false;
            }

            var nodeDetails = xmlDocument.GetElementsByTagName("detalles")[0];
            if (nodeDetails != null)
            {
                _detailsParser.Parse(nodeDetails, comprobante);
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error parsing invoice document: {ex.Message}";
            return false;
        }
    }

    private static void ParseInfoFactura(XmlNode nodeInfoFactura, ComprobanteElectronico comprobante)
    {
        var infoFac = new InfoFactura
        {
            FechaEmision = nodeInfoFactura.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoFactura.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoFactura.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoFactura.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
            TipoIdentificacionComprador = nodeInfoFactura.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoFactura.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoFactura.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            DireccionComprador = nodeInfoFactura.SelectSingleNode("direccionComprador")?.InnerText ?? string.Empty,
            GuiaRemision = nodeInfoFactura.SelectSingleNode("guiaRemision")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoFactura.SelectSingleNode("totalSinImpuestos")?.InnerText ?? string.Empty,
            TotalDescuento = nodeInfoFactura.SelectSingleNode("totalDescuento")?.InnerText ?? string.Empty,
            Propina = nodeInfoFactura.SelectSingleNode("propina")?.InnerText ?? "0.00",
            ImporteTotal = nodeInfoFactura.SelectSingleNode("importeTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoFactura.SelectSingleNode("moneda")?.InnerText ?? string.Empty
        };

        ParseTotalTaxes(infoFac, nodeInfoFactura.SelectSingleNode("totalConImpuestos"));

        var pagosNode = nodeInfoFactura.SelectSingleNode("pagos");
        if (pagosNode != null)
        {
            ParseInvoicePayments(infoFac, pagosNode);
        }

        comprobante.CreateInfoComp(comprobante.CodDoc, infoFac);
    }

    private static void ParseTotalTaxes(InfoFactura infoFac, XmlNode? impuestos)
    {
        if (impuestos == null) return;

        var totalImpuestos = (from XmlNode item in impuestos
                              select new TotalImpuesto
                              {
                                  Codigo = item.SelectSingleNode("codigo")?.InnerText ?? string.Empty,
                                  CodigoPorcentaje = item.SelectSingleNode("codigoPorcentaje")?.InnerText ?? string.Empty,
                                  DescuentoAdicional = item.SelectSingleNode("descuentoAdicional")?.InnerText ?? "0.00",
                                  BaseImponible = item.SelectSingleNode("baseImponible")?.InnerText ?? string.Empty,
                                  Valor = item.SelectSingleNode("valor")?.InnerText ?? string.Empty
                              }).ToList();

        infoFac.CreateTotalTaxes(totalImpuestos);
    }

    private static void ParseInvoicePayments(InfoFactura infoFac, XmlNode payments)
    {
        var pagos = (from XmlNode item in payments
                     select new Pago
                     {
                         FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty,
                         Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty,
                         Plazo = item.SelectSingleNode("plazo")?.InnerText ?? string.Empty,
                         UnidadTiempo = item.SelectSingleNode("unidadTiempo")?.InnerText ?? string.Empty
                     }).ToList();

        infoFac.CreatePayments(pagos);
    }
}

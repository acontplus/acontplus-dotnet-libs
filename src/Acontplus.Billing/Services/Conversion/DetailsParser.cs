using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Conversion;

/// <summary>
/// Parser for extracting line item details from electronic invoice and receipt XML nodes.
/// </summary>
public class DetailsParser : IDetailsParser
{
    /// <inheritdoc />
    public void Parse(XmlNode nodeDetails, ComprobanteElectronico comprobante)
    {
        var detalles = new List<Detalle>();
        var impuestos = new List<Impuesto>();
        var idDetalle = 0;

        foreach (XmlNode item in nodeDetails)
        {
            var detail = new Detalle
            {
                IdDetalle = idDetalle,
                CodigoPrincipal = comprobante.CodDoc == "01"
                    ? item.SelectSingleNode("codigoPrincipal")?.InnerText ?? string.Empty
                    : item.SelectSingleNode("codigoInterno")?.InnerText ?? string.Empty,
                CodigoAuxiliar = item.SelectSingleNode("codigoAuxiliar")?.InnerText ?? string.Empty,
                Descripcion = item.SelectSingleNode("descripcion")?.InnerText ?? string.Empty,
                Cantidad = item.SelectSingleNode("cantidad")?.InnerText ?? string.Empty,
                PrecioUnitario = item.SelectSingleNode("precioUnitario")?.InnerText ?? string.Empty,
                Descuento = item.SelectSingleNode("descuento")?.InnerText ?? string.Empty,
                PrecioTotalSinImpuesto = item.SelectSingleNode("precioTotalSinImpuesto")?.InnerText ?? string.Empty,
                Impuestos = item.SelectSingleNode("impuestos") == null
                    ? string.Empty
                    : item.SelectNodes("impuestos")?[0]?.OuterXml ?? string.Empty,
                DetallesAdicionales = item.SelectSingleNode("detallesAdicionales") == null
                    ? string.Empty
                    : item.SelectNodes("detallesAdicionales")?[0]?.OuterXml ?? string.Empty
            };

            // Process tax information
            var taxNodes = item.SelectNodes("impuestos");
            if (taxNodes != null)
            {
                foreach (XmlElement taxes in taxNodes)
                {
                    impuestos.Add(new Impuesto
                    {
                        IdDetalle = idDetalle,
                        CodArticulo = detail.CodigoPrincipal,
                        Codigo = taxes.GetElementsByTagName("codigo")[0]?.InnerText ?? string.Empty,
                        CodigoPorcentaje = taxes.GetElementsByTagName("codigoPorcentaje")[0]?.InnerText ?? string.Empty,
                        Tarifa = taxes.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                        BaseImponible = taxes.GetElementsByTagName("baseImponible")[0]?.InnerText ?? string.Empty,
                        Valor = taxes.GetElementsByTagName("valor")[0]?.InnerText ?? string.Empty
                    });
                }
            }

            detalles.Add(detail);
            idDetalle++;
        }

        comprobante.CreateTaxes(impuestos);
        comprobante.CreateDetails(detalles);
    }
}

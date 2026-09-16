using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents;

/// <summary>
/// Service that builds and serializes the ATS (Anexo Transaccional Simplificado) XML document.
/// </summary>
public class AtsXmlService : IAtsXmlService
{
    /// <inheritdoc />
    public async Task<byte[]> CreateAtsXmlAsync(AtsData atsData)
    {
        // Use a MemoryStream to hold the XML content
        await using var atsMs = new MemoryStream();

        // Use a StreamWriter to write to the MemoryStream.
        // It's important to set 'leaveOpen: true' so that XmlTextWriter doesn't close the MemoryStream
        // when it's disposed, allowing us to read from it afterwards.
        await using var tw = new StreamWriter(atsMs, Encoding.UTF8, leaveOpen: true);

        // XmlTextWriter for generating XML
        await using (var xtr = new XmlTextWriter(tw)) // Use a synchronous using statement
        {
            xtr.Formatting = Formatting.Indented;
            xtr.Indentation = 2;

            xtr.WriteStartDocument(true);
            xtr.WriteStartElement("iva");

            // Header
            xtr.WriteElement("TipoIDInformante", atsData.Header.TipoIdInformante);
            xtr.WriteElement("IdInformante", atsData.Header.IdInformante);
            xtr.WriteElement("razonSocial", atsData.Header.RazonSocial);
            xtr.WriteElement("Anio", atsData.Header.Anio);
            xtr.WriteElement("Mes", atsData.Header.Mes);
            xtr.WriteElement("numEstabRuc", atsData.Header.NumEstabRuc);
            xtr.WriteElement("totalVentas", atsData.Header.TotalVentas);
            xtr.WriteElement("codigoOperativo", atsData.Header.CodigoOperativo);

            // Purchases
            if (atsData.Purchases.Any())
            {
                WritePurchaseNode(xtr, atsData.Purchases, atsData.WithholdingTaxes);
            }

            // Sales
            if (atsData.Sales.Any())
            {
                WriteSalesNode(xtr, atsData.Sales);
                WriteEstablishmentSalesNode(xtr, atsData.EstablishmentSales);
            }

            // Canceled Documents
            if (atsData.CanceledDocuments.Any())
            {
                WriteCanceledDocsNode(xtr, atsData.CanceledDocuments);
            }

            xtr.WriteEndElement(); // end iva
            xtr.WriteEndDocument();

            // The 'using' statement on xtr handles disposing and flushing automatically.
        } // xtr.Dispose() is called here, flushing the StreamWriter, which writes to the MemoryStream

        // After the using block, the MemoryStream now contains the complete XML
        return atsMs.ToArray();
    }

    private static void WritePurchaseNode(XmlTextWriter xtr, IEnumerable<Purchase> purchases,
        IEnumerable<WithholdingTax> withholdingTaxes)
    {
        xtr.WriteStartElement("compras");

        foreach (var purchase in purchases)
        {
            xtr.WriteStartElement("detalleCompras");

            xtr.WriteElement("codSustento", purchase.CodSustento);
            xtr.WriteElement("tpIdProv", purchase.TpIdProv);
            xtr.WriteElement("idProv", purchase.IdProv);
            xtr.WriteElement("tipoComprobante", purchase.TipoComprobante);
            xtr.WriteElement("parteRel", purchase.ParteRel);
            xtr.WriteElement("fechaRegistro", purchase.FechaRegistro);
            xtr.WriteElement("establecimiento", purchase.Establecimiento);
            xtr.WriteElement("puntoEmision", purchase.PuntoEmision);
            xtr.WriteElement("secuencial", purchase.Secuencial);
            xtr.WriteElement("fechaEmision", purchase.FechaEmision);
            xtr.WriteElement("autorizacion", purchase.Autorizacion);
            xtr.WriteElement("baseNoGraIva", purchase.BaseNoGraIva);
            xtr.WriteElement("baseImponible", purchase.BaseImponible);
            xtr.WriteElement("baseImpGrav", purchase.BaseImpGrav);
            xtr.WriteElement("baseImpExe", purchase.BaseImpExe);
            xtr.WriteElement("montoIce", purchase.MontoIce);
            xtr.WriteElement("montoIva", purchase.MontoIva);
            xtr.WriteElement("valRetBien10", purchase.ValRetBien10);
            xtr.WriteElement("valRetServ20", purchase.ValRetServ20);
            xtr.WriteElement("valorRetBienes", purchase.ValorRetBienes);
            xtr.WriteElement("valRetServ50", purchase.ValRetServ50);
            xtr.WriteElement("valorRetServicios", purchase.ValorRetServicios);
            xtr.WriteElement("valRetServ100", purchase.ValRetServ100);
            xtr.WriteElement("totbasesImpReemb", purchase.TotbasesImpReemb);

            // Pago Exterior
            xtr.WriteStartElement("pagoExterior");
            xtr.WriteElement("pagoLocExt", purchase.PagoLocExt);
            if (purchase.PagoLocExt == "02")
            {
                xtr.WriteElement("tipoRegi", purchase.TipoRegi);
            }

            xtr.WriteElement("denopagoRegFis", purchase.DenopagoRegFis);
            xtr.WriteElement("paisEfecPago", purchase.PaisEfecPago);
            xtr.WriteElement("aplicConvDobTrib", purchase.AplicConvDobTrib);
            xtr.WriteElement("pagExtSujRetNorLeg", purchase.PagExtSujRetNorLeg);
            xtr.WriteEndElement(); // end pagoExterior

            if (!string.IsNullOrEmpty(purchase.FormaPago))
            {
                xtr.WriteStartElement("formasDePago");
                xtr.WriteElement("formaPago", purchase.FormaPago);
                xtr.WriteEndElement();
            }

            // Filter withholding taxes relevant to this purchase
            var relevantWithholdingTaxes = withholdingTaxes.Where(wt =>
                wt.NroDocumento == purchase.NroDocumento && wt.ClaveAcceso == purchase.Autorizacion);

            if (relevantWithholdingTaxes.Any())
            {
                xtr.WriteStartElement("air");
                WriteWithHoldingTaxesNode(xtr, relevantWithholdingTaxes);
                xtr.WriteEndElement();
            }

            if (purchase.TipoComprobante == "04")
            {
                xtr.WriteElement("docModificado", purchase.DocModificado);
                xtr.WriteElement("estabModificado", purchase.EstabModificado);
                xtr.WriteElement("ptoEmiModificado", purchase.PtoEmiModificado);
                xtr.WriteElement("secModificado", purchase.SecModificado);
                xtr.WriteElement("autModificado", purchase.AutModificado);
            }

            if (!string.IsNullOrEmpty(purchase.EstabRetencion1))
            {
                xtr.WriteElement("estabRetencion1", purchase.EstabRetencion1);
                xtr.WriteElement("ptoEmiRetencion1", purchase.PtoEmiRetencion1);
                xtr.WriteElement("secRetencion1", purchase.SecRetencion1);
                xtr.WriteElement("autRetencion1", purchase.AutRetencion1);
                xtr.WriteElement("fechaEmiRet1", purchase.FechaEmiRet1);
            }

            xtr.WriteEndElement(); // end detalleCompras
        }

        xtr.WriteEndElement(); // end compras
    }

    private static void WriteWithHoldingTaxesNode(XmlTextWriter xtr, IEnumerable<WithholdingTax> withholdingTaxes)
    {
        foreach (var tax in withholdingTaxes)
        {
            xtr.WriteStartElement("detalleAir");
            xtr.WriteElement("codRetAir", tax.CodRetAir);
            xtr.WriteElement("baseImpAir", tax.BaseImpAir);
            xtr.WriteElement("porcentajeAir", tax.PorcentajeAir);
            xtr.WriteElement("valRetAir", tax.ValRetAir);
            xtr.WriteEndElement(); // end detalleAir
        }
    }

    private static void WriteSalesNode(XmlTextWriter xtr, IEnumerable<Sale> sales)
    {
        xtr.WriteStartElement("ventas");

        foreach (var sale in sales)
        {
            xtr.WriteStartElement("detalleVentas");

            xtr.WriteElement("tpIdCliente", sale.TpIdCliente);
            xtr.WriteElement("idCliente", sale.IdCliente);

            if (sale.TpIdCliente is "04" or "05" or "06")
            {
                xtr.WriteElement("parteRelVtas", sale.ParteRelVtas);
            }

            if (sale.TpIdCliente == "06")
            {
                xtr.WriteElement("tipoCliente", sale.TipoCliente);
                xtr.WriteElement("denoCli", sale.DenoCli);
            }

            xtr.WriteElement("tipoComprobante", sale.TipoComprobante);
            xtr.WriteElement("tipoEmision", sale.TipoEmision);
            xtr.WriteElement("numeroComprobantes", sale.NumeroComprobantes);
            xtr.WriteElement("baseNoGraIva", sale.BaseNoGraIva);
            xtr.WriteElement("baseImponible", sale.BaseImponible);
            xtr.WriteElement("baseImpGrav", sale.BaseImpGrav);
            xtr.WriteElement("montoIva", sale.MontoIva);

            if (!string.IsNullOrEmpty(sale.TipoCompe))
            {
                xtr.WriteStartElement("compensaciones");
                xtr.WriteStartElement("compensacion");
                xtr.WriteElement("tipoCompe", sale.TipoCompe);
                xtr.WriteElement("monto", sale.MontoCompensacion);
                xtr.WriteEndElement(); // end compensacion
                xtr.WriteEndElement(); // end compensaciones
            }

            xtr.WriteElement("montoIce", sale.MontoIce);
            xtr.WriteElement("valorRetIva", sale.ValorRetIva);
            xtr.WriteElement("valorRetRenta", sale.ValorRetRenta);

            if (!string.IsNullOrEmpty(sale.FormaPago))
            {
                xtr.WriteStartElement("formasDePago");
                xtr.WriteElement("formaPago", sale.FormaPago);
                xtr.WriteEndElement(); // end formasDePago
            }
            //end formasDePago

            xtr.WriteEndElement(); // end detalleVentas
        }

        xtr.WriteEndElement(); // end ventas
    }

    private static void WriteEstablishmentSalesNode(XmlTextWriter xtr, IEnumerable<EstablishmentSale> establishmentSales)
    {
        xtr.WriteStartElement("ventasEstablecimiento");
        foreach (var estSale in establishmentSales)
        {
            xtr.WriteStartElement("ventaEst");
            xtr.WriteElement("codEstab", estSale.CodEstab);
            xtr.WriteElement("ventasEstab", estSale.VentasEstab);
            xtr.WriteElement("ivaComp", estSale.IvaComp);
            xtr.WriteEndElement(); // end ventaEst
        }

        xtr.WriteEndElement(); // end ventasEstablecimiento
    }

    private static void WriteCanceledDocsNode(XmlTextWriter xtr, IEnumerable<CanceledDocument> canceledDocuments)
    {
        xtr.WriteStartElement("anulados");

        foreach (var canceledDoc in canceledDocuments)
        {
            xtr.WriteStartElement("detalleAnulados");
            xtr.WriteElement("tipoComprobante", canceledDoc.TipoComprobante);
            xtr.WriteElement("establecimiento", canceledDoc.Establecimiento);
            xtr.WriteElement("puntoEmision", canceledDoc.PuntoEmision);
            xtr.WriteElement("secuencialInicio", canceledDoc.SecuencialInicio);
            xtr.WriteElement("secuencialFin", canceledDoc.SecuencialFin);
            xtr.WriteElement("autorizacion", canceledDoc.Autorizacion);
            xtr.WriteEndElement(); // end detalleAnulados
        }

        xtr.WriteEndElement(); // end anulados
    }
}

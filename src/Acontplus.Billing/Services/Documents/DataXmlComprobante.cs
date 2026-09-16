using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents;

/// <summary>
/// Legacy XML document parser for SRI electronic receipts.
/// </summary>
public static class DataXmlComprobante
{
    private const string TagVersion = "version";
    private const string TagDetalles = "detalles";
    private const string TagImpuestos = "impuestos";
    private const string TagFechaEmision = "fechaEmision";
    private const string TagDirEstablecimiento = "dirEstablecimiento";
    private const string TagContribuyenteEspecial = "contribuyenteEspecial";
    private const string TagObligadoContabilidad = "obligadoContabilidad";
    private const string TagFechaEmisionDocSustento = "fechaEmisionDocSustento";
    private const string TagTotalSinImpuestos = "totalSinImpuestos";
    private const string TagMoneda = "moneda";
    private const string TagPagos = "pagos";
    private const string TagCodigo = "codigo";
    private const string TagCodigoPorcentaje = "codigoPorcentaje";
    private const string TagBaseImponible = "baseImponible";

    /// <summary>
    /// Extracts electronic receipt metadata and nodes from an SRI XML document.
    /// </summary>
    /// <param name="xmlSri">The SRI XML document.</param>
    /// <param name="comp">The electronic receipt reference to populate.</param>
    /// <param name="message">The output error message if parsing fails.</param>
    /// <returns><c>true</c> if successfully extracted; otherwise <c>false</c>.</returns>
    public static bool GetData(XmlDocument xmlSri, ref ComprobanteElectronico comp, ref string message)
    {
        var resp = true;
        try
        {
            var authList = xmlSri.GetElementsByTagName("autorizacion");
            if (authList.Count == 0) return false;
            var nodeAuth = authList[0];
            if (nodeAuth == null) return false;

            var nodeComp = nodeAuth.SelectSingleNode("comprobante");
            var xmlComp = new XmlDocument();
            if (nodeComp != null)
            {
                // Sanitizar el XML antes de cargarlo para evitar errores de parsing
                var cleanedXml = XmlValidator.CleanXmlForSqlServer(nodeComp.InnerText);
                xmlComp.LoadXml(cleanedXml);
            }
            else
                return false;

            comp.NumeroAutorizacion = nodeAuth.SelectSingleNode("numeroAutorizacion")?.InnerText ?? string.Empty;
            comp.FechaAutorizacion = nodeAuth.SelectSingleNode("fechaAutorizacion")?.InnerText ?? string.Empty;

            var infoTribList = xmlComp.GetElementsByTagName("infoTributaria");
            if (infoTribList.Count > 0)
            {
                var nodeInfoTrib = infoTribList[0];
                if (nodeInfoTrib != null)
                {
                    comp.CodDoc = nodeInfoTrib.SelectSingleNode("codDoc")?.InnerText ?? string.Empty;
                    GetInfoTributaria(comp, nodeInfoTrib);
                }
            }

            ProcessDocumentByCode(comp.CodDoc, xmlComp, comp);

            if (xmlComp.GetElementsByTagName("infoAdicional")[0] != null)
                GetInfoAdicional(comp, xmlComp.GetElementsByTagName("infoAdicional")[0]);
        }
        catch (Exception ex)
        {
            resp = false;
            message = "Error al obtener la informacion del comprobante: " + ex.Message;
        }

        return resp;
    }

    private static void ProcessDocumentByCode(string codDoc, XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        switch (codDoc)
        {
            case "01":
                ProcessFactura(xmlComp, comp);
                break;
            case "03":
                ProcessLiquidacionCompra(xmlComp, comp);
                break;
            case "04":
                ProcessNotaCredito(xmlComp, comp);
                break;
            case "05":
                ProcessNotaDebito(xmlComp, comp);
                break;
            case "06":
                ProcessGuiaRemision(xmlComp, comp);
                break;
            case "07":
                ProcessComprobanteRetencion(xmlComp, comp);
                break;
        }
    }

    private static void ProcessFactura(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeFact = xmlComp.GetElementsByTagName("factura")[0];
        comp.VersionComp = nodeFact?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoFactura = xmlComp.GetElementsByTagName("infoFactura")[0];
        if (nodeInfoFactura != null) GetInfoFactura(comp.CodDoc, comp, nodeInfoFactura);

        GetDetails(comp, xmlComp.GetElementsByTagName(TagDetalles)[0]);
    }

    private static void ProcessLiquidacionCompra(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeLiq = xmlComp.GetElementsByTagName("liquidacionCompra")[0];
        comp.VersionComp = nodeLiq?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoLiquidacion = xmlComp.GetElementsByTagName("infoLiquidacionCompra")[0];
        if (nodeInfoLiquidacion != null) GetInfoLiquidacionCompra(comp.CodDoc, comp, nodeInfoLiquidacion);

        GetDetails(comp, xmlComp.GetElementsByTagName(TagDetalles)[0]);
    }

    private static void ProcessNotaCredito(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeNc = xmlComp.GetElementsByTagName("notaCredito")[0];
        comp.VersionComp = nodeNc?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoNotaCredito = xmlComp.GetElementsByTagName("infoNotaCredito")[0];
        if (nodeInfoNotaCredito != null) GetInfoNotaCredito(comp.CodDoc, comp, nodeInfoNotaCredito);

        GetDetails(comp, xmlComp.GetElementsByTagName(TagDetalles)[0]);
    }

    private static void ProcessNotaDebito(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeNd = xmlComp.GetElementsByTagName("notaDebito")[0];
        comp.VersionComp = nodeNd?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoNotaDebito = xmlComp.GetElementsByTagName("infoNotaDebito")[0];
        if (nodeInfoNotaDebito != null) GetInfoNotaDebito(comp.CodDoc, comp, nodeInfoNotaDebito);

        var nodeMotivos = xmlComp.GetElementsByTagName("motivos")[0];
        if (nodeMotivos != null) GetMotivosNotaDebito(comp, nodeMotivos);
    }

    private static void ProcessGuiaRemision(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeGr = xmlComp.GetElementsByTagName("guiaRemision")[0];
        comp.VersionComp = nodeGr?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoGuiaRemision = xmlComp.GetElementsByTagName("infoGuiaRemision")[0];
        if (nodeInfoGuiaRemision != null) GetInfoGuiaRemision(comp, nodeInfoGuiaRemision);

        var nodeDestinatarios = xmlComp.GetElementsByTagName("destinatarios")[0];
        if (nodeDestinatarios != null) GetDestinatarios(comp, nodeDestinatarios);
    }

    private static void ProcessComprobanteRetencion(XmlDocument xmlComp, ComprobanteElectronico comp)
    {
        var nodeRet = xmlComp.GetElementsByTagName("comprobanteRetencion")[0];
        comp.VersionComp = nodeRet?.Attributes?[TagVersion]?.Value ?? string.Empty;

        var nodeInfoCompRetencion = xmlComp.GetElementsByTagName("infoCompRetencion")[0];
        GetInfoCompRetencion(comp.VersionComp, comp, nodeInfoCompRetencion);

        if (comp.VersionComp == "2.0.0")
            GetDocSustento(comp, xmlComp.GetElementsByTagName("docsSustento")[0]);
        else
            GetImpuestoRetencion(comp, xmlComp.GetElementsByTagName(TagImpuestos)[0]);
    }

    private static void GetInfoTributaria(ComprobanteElectronico ce, XmlNode nodeInfoTrib) =>
        new Acontplus.Billing.Services.Conversion.InfoTributariaParser().Parse(nodeInfoTrib, ce);

    /// <summary>
    /// Extracts credit note information from the XML node.
    /// </summary>
    /// <param name="codDoc">The document type code.</param>
    /// <param name="ce">The electronic receipt being populated.</param>
    /// <param name="nodeInfoNotaCredito">The XML node containing credit note info.</param>
    public static void GetInfoNotaCredito(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoNotaCredito)
    {
        var infoFac = new InfoNotaCredito
        {
            FechaEmision = nodeInfoNotaCredito.SelectSingleNode(TagFechaEmision)?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoNotaCredito.SelectSingleNode(TagDirEstablecimiento) == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
            TipoIdentificacionComprador =
                nodeInfoNotaCredito.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoNotaCredito.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoNotaCredito.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoNotaCredito.SelectSingleNode(TagContribuyenteEspecial) == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoNotaCredito.SelectSingleNode(TagObligadoContabilidad) == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
            Rise = nodeInfoNotaCredito.SelectSingleNode("rise") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            CodDocModificado = nodeInfoNotaCredito.SelectSingleNode("codDocModificado")?.InnerText ?? string.Empty,
            NumDocModificado = nodeInfoNotaCredito.SelectSingleNode("numDocModificado")?.InnerText ?? string.Empty,
            FechaEmisionDocSustento = nodeInfoNotaCredito.SelectSingleNode(TagFechaEmisionDocSustento) == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode(TagFechaEmisionDocSustento)?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoNotaCredito.SelectSingleNode(TagTotalSinImpuestos)?.InnerText ?? string.Empty,
            ValorModificacion = nodeInfoNotaCredito.SelectSingleNode("valorModificacion") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("valorModificacion")?.InnerText ?? string.Empty,
            Moneda = nodeInfoNotaCredito.SelectSingleNode(TagMoneda) == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode(TagMoneda)?.InnerText ?? string.Empty,
            Motivo = nodeInfoNotaCredito.SelectSingleNode("motivo") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("motivo")?.InnerText ?? string.Empty
        };
        GetTotalTaxes(codDoc, infoFac, nodeInfoNotaCredito.SelectSingleNode("totalConImpuestos"));
        ce.CreateInfoComp(codDoc, infoFac);
    }

    /// <summary>
    /// Extracts additional information (infoAdicional) name-value pairs into the electronic receipt.
    /// </summary>
    /// <param name="comp">The electronic receipt to populate.</param>
    /// <param name="infoAdi">The XML node containing infoAdicional elements.</param>
    public static void GetInfoAdicional(ComprobanteElectronico comp, XmlNode? infoAdi)
    {
        if (infoAdi == null) return;

        var infoAdicionals = (from XmlNode item in infoAdi
                              select new InfoAdicional
                              {
                                  Nombre = item.Attributes?.GetNamedItem("nombre")!.Value ?? string.Empty,
                                  Valor = item.InnerText ?? string.Empty
                              })
            .ToList();

        comp.CreateAdditionalInfo(infoAdicionals);
    }

    private static void GetInfoFactura(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoFactura)
    {
        var infoFac = new InfoFactura
        {
            FechaEmision = nodeInfoFactura.SelectSingleNode(TagFechaEmision)?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoFactura.SelectSingleNode(TagDirEstablecimiento) == null
                ? ""
                : nodeInfoFactura.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoFactura.SelectSingleNode(TagContribuyenteEspecial) == null
                ? ""
                : nodeInfoFactura.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoFactura.SelectSingleNode(TagObligadoContabilidad) == null
                ? ""
                : nodeInfoFactura.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
            TipoIdentificacionComprador =
                nodeInfoFactura.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoFactura.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoFactura.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            DireccionComprador = nodeInfoFactura.SelectSingleNode("direccionComprador") == null
                ? ""
                : nodeInfoFactura.SelectSingleNode("direccionComprador")?.InnerText ?? string.Empty,
            GuiaRemision = nodeInfoFactura.SelectSingleNode("guiaRemision") == null
                ? ""
                : nodeInfoFactura.SelectSingleNode("guiaRemision")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoFactura.SelectSingleNode(TagTotalSinImpuestos)?.InnerText ?? string.Empty,
            TotalDescuento = nodeInfoFactura.SelectSingleNode("totalDescuento")?.InnerText ?? string.Empty,
            Propina = nodeInfoFactura.SelectSingleNode("propina") == null
                ? "0.00"
                : nodeInfoFactura.SelectSingleNode("propina")?.InnerText ?? string.Empty,
            ImporteTotal = nodeInfoFactura.SelectSingleNode("importeTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoFactura.SelectSingleNode(TagMoneda) == null
                ? ""
                : nodeInfoFactura.SelectSingleNode(TagMoneda)?.InnerText ?? string.Empty
        };

        GetTotalTaxes(codDoc, infoFac, nodeInfoFactura.SelectSingleNode("totalConImpuestos"));

        if (nodeInfoFactura.SelectSingleNode(TagPagos) != null)
            GetInvoicePayments(infoFac, nodeInfoFactura.SelectSingleNode(TagPagos));

        ce.CreateInfoComp(codDoc, infoFac);
    }

    private static void GetTotalTaxes(string codDoc, object obj, XmlNode? impuestos)
    {
        if (impuestos == null) return;

        var totalImpuestos = (from XmlNode item in impuestos
                              select new TotalImpuesto
                              {
                                  Codigo = item.SelectSingleNode(TagCodigo)?.InnerText ?? string.Empty,
                                  CodigoPorcentaje = item.SelectSingleNode(TagCodigoPorcentaje)?.InnerText ?? string.Empty,
                                  DescuentoAdicional = item.SelectSingleNode("descuentoAdicional") == null
                                      ? "0.00"
                                      : item.SelectSingleNode("descuentoAdicional")?.InnerText ?? string.Empty,
                                  BaseImponible = item.SelectSingleNode(TagBaseImponible)?.InnerText ?? string.Empty,
                                  Valor = item.SelectSingleNode("valor")?.InnerText ?? string.Empty
                              }).ToList();

        switch (codDoc)
        {
            case "01":
                var infoFactura = obj as InfoFactura;
                infoFactura?.CreateTotalTaxes(totalImpuestos);
                break;
            case "03":
                var infoLiquidacion = obj as InfoLiquidacionCompra;
                infoLiquidacion?.CreateTotalTaxes(totalImpuestos);
                break;
            case "04":
                var infoNotaCredito = obj as InfoNotaCredito;
                infoNotaCredito?.CreateTotalTaxes(totalImpuestos);
                break;
            case "05":
                var infoNotaDebito = obj as InfoNotaDebito;
                infoNotaDebito?.CreateTotalTaxes(totalImpuestos);
                break;
        }
    }

    private static void GetInvoicePayments(InfoFactura comp, XmlNode? payments)
    {
        if (payments == null) return;
        comp.CreatePayments(ParsePagos(payments));
    }

    private static List<Pago> ParsePagos(XmlNode payments) =>
        (from XmlNode item in payments
         select new Pago
         {
             FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty,
             Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty,
             Plazo = item.SelectSingleNode("plazo")?.InnerText ?? string.Empty,
             UnidadTiempo = item.SelectSingleNode("unidadTiempo")?.InnerText ?? string.Empty
         }).ToList();

    private static void GetDetails(ComprobanteElectronico comp, XmlNode? details)
    {
        if (details == null) return;

        var detalles = new List<Detalle>();
        var impuestos = new List<Impuesto>();
        var idDetalle = 0;
        foreach (XmlNode item in details)
        {
            var detail = ParseDetalle(item, comp.CodDoc, idDetalle);
            impuestos.AddRange(ParseDetalleImpuestos(item, idDetalle, detail.CodigoPrincipal));
            detalles.Add(detail);
            idDetalle++;
        }

        comp.CreateTaxes(impuestos);
        comp.CreateDetails(detalles);
    }

    private static Detalle ParseDetalle(XmlNode item, string codDoc, int idDetalle) =>
        new()
        {
            IdDetalle = idDetalle,
            CodigoPrincipal = codDoc == "01"
                ? item.SelectSingleNode("codigoPrincipal")?.InnerText ?? string.Empty
                : item.SelectSingleNode("codigoInterno")?.InnerText ?? string.Empty,
            CodigoAuxiliar = item.SelectSingleNode("codigoAuxiliar")?.InnerText ?? string.Empty,
            Descripcion = item.SelectSingleNode("descripcion")?.InnerText ?? string.Empty,
            Cantidad = item.SelectSingleNode("cantidad")?.InnerText ?? string.Empty,
            PrecioUnitario = item.SelectSingleNode("precioUnitario")?.InnerText ?? string.Empty,
            Descuento = item.SelectSingleNode("descuento")?.InnerText ?? string.Empty,
            PrecioTotalSinImpuesto = item.SelectSingleNode("precioTotalSinImpuesto")?.InnerText ?? string.Empty,
            Impuestos = item.SelectSingleNode(TagImpuestos)?.OuterXml ?? string.Empty,
            DetallesAdicionales = item.SelectSingleNode("detallesAdicionales")?.OuterXml ?? string.Empty
        };

    private static List<Impuesto> ParseDetalleImpuestos(XmlNode item, int idDetalle, string codArticulo)
    {
        var taxesNodes = item.SelectNodes(TagImpuestos);
        if (taxesNodes == null) return [];

        return (from XmlElement taxes in taxesNodes
                select new Impuesto
                {
                    IdDetalle = idDetalle,
                    CodArticulo = codArticulo,
                    Codigo = taxes.GetElementsByTagName(TagCodigo)[0]?.InnerText ?? string.Empty,
                    CodigoPorcentaje = taxes.GetElementsByTagName(TagCodigoPorcentaje)[0]?.InnerText ?? string.Empty,
                    Tarifa = taxes.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                    BaseImponible = taxes.GetElementsByTagName(TagBaseImponible)[0]?.InnerText ?? string.Empty,
                    Valor = taxes.GetElementsByTagName("valor")[0]?.InnerText ?? string.Empty
                }).ToList();
    }

    private static void GetInfoCompRetencion(string? versionComp, ComprobanteElectronico ce, XmlNode? nodeInfoCompRetencion)
    {
        if (nodeInfoCompRetencion != null)
        {
            var infoRet = new InfoCompRetencion
            {
                FechaEmision = nodeInfoCompRetencion.SelectSingleNode(TagFechaEmision)?.InnerText ?? string.Empty,
                DirEstablecimiento = nodeInfoCompRetencion.SelectSingleNode(TagDirEstablecimiento) == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
                ContribuyenteEspecial = nodeInfoCompRetencion.SelectSingleNode(TagContribuyenteEspecial) == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
                ObligadoContabilidad = nodeInfoCompRetencion.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
                TipoIdentificacionSujetoRetenido = nodeInfoCompRetencion.SelectSingleNode("tipoIdentificacionSujetoRetenido")?.InnerText ?? string.Empty,
                RazonSocialSujetoRetenido = nodeInfoCompRetencion.SelectSingleNode("razonSocialSujetoRetenido")?.InnerText ?? string.Empty,
                IdentificacionSujetoRetenido = nodeInfoCompRetencion.SelectSingleNode("identificacionSujetoRetenido")?.InnerText ?? string.Empty,
                PeriodoFiscal = nodeInfoCompRetencion.SelectSingleNode("periodoFiscal") == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode("periodoFiscal")?.InnerText ?? string.Empty
            };
            if (versionComp == "2.0.0")
            {
                infoRet.ParteRel = nodeInfoCompRetencion.SelectSingleNode("parteRel")?.InnerText ?? string.Empty;
                infoRet.TipoSujetoRetenido = nodeInfoCompRetencion.SelectSingleNode("tipoSujetoRetenido") == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode("tipoSujetoRetenido")?.InnerText ?? string.Empty;
            }

            ce.CreateInfoComp("07", infoRet);
        }
    }

    private static void GetImpuestoRetencion(ComprobanteElectronico comp, XmlNode? impuestosRet)
    {
        if (impuestosRet == null) return;

        var impuestos = new List<ImpuestoRetencion>();
        foreach (XmlElement taxes in impuestosRet)
        {
            var tax = new ImpuestoRetencion
            {
                Codigo = taxes.GetElementsByTagName(TagCodigo)[0]?.InnerText ?? string.Empty,
                CodigoRetencion = taxes.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = taxes.GetElementsByTagName(TagBaseImponible)[0]?.InnerText ?? string.Empty,
                PorcentajeRetener = taxes.GetElementsByTagName("porcentajeRetener")[0]?.InnerText ?? string.Empty,
                ValorRetenido = taxes.GetElementsByTagName("valorRetenido")[0]?.InnerText ?? string.Empty,
                CodDocSustento = taxes.GetElementsByTagName("codDocSustento")[0]?.InnerText ?? string.Empty,
                NumDocSustento = taxes.GetElementsByTagName("numDocSustento")[0]?.InnerText ?? string.Empty,
                FechaEmisionDocSustento = taxes.GetElementsByTagName(TagFechaEmisionDocSustento)[0]?.InnerText ?? string.Empty
            };
            impuestos.Add(tax);
        }

        comp.CreateRetencionTaxes(impuestos);
    }

    private static void GetDocSustento(ComprobanteElectronico ce, XmlNode? nodeDocsSustento)
    {
        if (nodeDocsSustento == null) return;

        var docsSustento = new List<DocSustento>();
        foreach (XmlElement item in nodeDocsSustento)
        {
            docsSustento.Add(ParseDocSustentoItem(item, ce));
        }

        ce.CreateDocSustentos(docsSustento);
    }

    private static DocSustento ParseDocSustentoItem(XmlElement item, ComprobanteElectronico ce)
    {
        var docSustento = new DocSustento
        {
            CodSustento = item.GetElementsByTagName("codSustento")[0]?.InnerText ?? string.Empty,
            CodDocSustento = item.GetElementsByTagName("codDocSustento")[0]?.InnerText ?? string.Empty,
            NumDocSustento = item.GetElementsByTagName("numDocSustento")[0]?.InnerText ?? string.Empty,
            FechaEmisionDocSustento = item.GetElementsByTagName(TagFechaEmisionDocSustento)[0]?.InnerText ?? string.Empty,
            NumAutDocSustento = item.SelectSingleNode("numAutDocSustento")?.InnerText ?? string.Empty,
            PagoLocExt = item.GetElementsByTagName("pagoLocExt")[0]?.InnerText ?? string.Empty,
            TipoRegi = item.SelectSingleNode("tipoRegi")?.InnerText ?? string.Empty,
            PaisEfecPago = item.SelectSingleNode("paisEfecPago")?.InnerText ?? string.Empty,
            AplicConvDobTrib = item.SelectSingleNode("aplicConvDobTrib")?.InnerText ?? string.Empty,
            PagExtSujRetNorLeg = item.SelectSingleNode("pagExtSujRetNorLeg")?.InnerText ?? string.Empty,
            PagoRegFis = item.SelectSingleNode("pagoRegFis")?.InnerText ?? string.Empty,
            TotalComprobantesReembolso = item.SelectSingleNode("totalComprobantesReembolso")?.InnerText ?? string.Empty,
            TotalBaseImponibleReembolso = item.SelectSingleNode("totalBaseImponibleReembolso")?.InnerText ?? string.Empty,
            TotalImpuestoReembolso = item.SelectSingleNode("totalImpuestoReembolso")?.InnerText ?? string.Empty,
            TotalSinImpuestos = item.GetElementsByTagName(TagTotalSinImpuestos)[0]?.InnerText ?? string.Empty,
            ImporteTotal = item.GetElementsByTagName("importeTotal")[0]?.InnerText ?? string.Empty
        };

        GetImpuestoDocSustento(docSustento, item.SelectSingleNode("impuestosDocSustento"));
        GetRetenciones(docSustento, item.SelectSingleNode("retenciones"), ce);
        if (docSustento.CodDocSustento == "41")
        {
            GetReembolsos(docSustento, item.SelectSingleNode("reembolsos"));
        }

        var pagosNode = item.SelectSingleNode(TagPagos);
        if (pagosNode != null)
        {
            GetRetencionPayments(docSustento, pagosNode);
        }

        return docSustento;
    }

    private static void GetImpuestoDocSustento(DocSustento doc, XmlNode? nodeImpuestos)
    {
        if (nodeImpuestos == null) return;

        var impuestos = (from XmlElement item in nodeImpuestos
                         select new ImpuestoDocSustento
                         {
                             CodImpuestoDocSustento = item.GetElementsByTagName("codImpuestoDocSustento")[0]?.InnerText ?? string.Empty,
                             CodigoPorcentaje = item.GetElementsByTagName(TagCodigoPorcentaje)[0]?.InnerText ?? string.Empty,
                             BaseImponible = item.GetElementsByTagName(TagBaseImponible)[0]?.InnerText ?? string.Empty,
                             Tarifa = item.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                             ValorImpuesto = item.GetElementsByTagName("valorImpuesto")[0]?.InnerText ?? string.Empty
                         }).ToList();

        doc.CreateTax(impuestos);
    }

    private static void GetRetenciones(DocSustento doc, XmlNode? nodeRetenciones, ComprobanteElectronico ce)
    {
        if (nodeRetenciones == null) return;

        var retenciones = new List<Retencion>();
        var retToView = new List<ImpuestoRetencion>();

        foreach (XmlElement item in nodeRetenciones)
        {
            var retencion = new Retencion
            {
                Codigo = item.GetElementsByTagName(TagCodigo)[0]?.InnerText ?? string.Empty,
                CodigoRetencion = item.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = item.GetElementsByTagName(TagBaseImponible)[0]?.InnerText ?? string.Empty,
                PorcentajeRetener = item.GetElementsByTagName("porcentajeRetener")[0]?.InnerText ?? string.Empty,
                ValorRetenido = item.GetElementsByTagName("valorRetenido")[0]?.InnerText ?? string.Empty
            };
            retenciones.Add(retencion);

            var ir = new ImpuestoRetencion
            {
                Codigo = item.GetElementsByTagName(TagCodigo)[0]?.InnerText ?? string.Empty,
                CodigoRetencion = item.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = item.GetElementsByTagName(TagBaseImponible)[0]?.InnerText ?? string.Empty,
                PorcentajeRetener = item.GetElementsByTagName("porcentajeRetener")[0]?.InnerText ?? string.Empty,
                ValorRetenido = item.GetElementsByTagName("valorRetenido")[0]?.InnerText ?? string.Empty,
                CodDocSustento = doc.CodDocSustento,
                NumDocSustento = doc.NumDocSustento,
                FechaEmisionDocSustento = doc.FechaEmisionDocSustento
            };
            retToView.Add(ir);
        }

        ce.CreateRetencionTaxes(retToView);
        doc.CreateRetencion(retenciones);
    }

    private static void GetReembolsos(DocSustento doc, XmlNode? nodeReemb)
    {
        if (nodeReemb == null) return;
        doc.CreateReembolsos(ParseReembolsos(nodeReemb));
    }

    private static List<ReembolsoDetalle> ParseReembolsos(XmlNode nodeReemb)
    {
        var reembolsos = new List<ReembolsoDetalle>();
        foreach (XmlElement item in nodeReemb)
        {
            var reembolso = new ReembolsoDetalle
            {
                TipoIdentificacionProveedorReembolso = item.GetElementsByTagName("tipoIdentificacionProveedorReembolso")[0]?.InnerText ?? string.Empty,
                IdentificacionProveedorReembolso = item.GetElementsByTagName("identificacionProveedorReembolso")[0]?.InnerText ?? string.Empty,
                CodPaisPagoProveedorReembolso = item.GetElementsByTagName("codPaisPagoProveedorReembolso")[0]?.InnerText ?? string.Empty,
                TipoProveedorReembolso = item.GetElementsByTagName("tipoProveedorReembolso")[0]?.InnerText ?? string.Empty,
                CodDocReembolso = item.GetElementsByTagName("codDocReembolso")[0]?.InnerText ?? string.Empty,
                EstabDocReembolso = item.GetElementsByTagName("estabDocReembolso")[0]?.InnerText ?? string.Empty,
                PtoEmiDocReembolso = item.GetElementsByTagName("ptoEmiDocReembolso")[0]?.InnerText ?? string.Empty,
                SecuencialDocReembolso = item.GetElementsByTagName("secuencialDocReembolso")[0]?.InnerText ?? string.Empty,
                FechaEmisionDocReembolso = item.GetElementsByTagName("fechaEmisionDocReembolso")[0]?.InnerText ?? string.Empty,
                NumeroAutorizacionDocReemb = (item.GetElementsByTagName("numeroAutorizacionDocReemb")[0] ?? item.GetElementsByTagName("numeroautorizacionDocReemb")[0])?.InnerText ?? string.Empty
            };
            GetImpuestosReembolsos(reembolso, item.SelectSingleNode("detalleImpuestos"));
            reembolsos.Add(reembolso);
        }

        return reembolsos;
    }

    private static void GetImpuestosReembolsos(ReembolsoDetalle reembolsoDetalles, XmlNode? impuestosReembolso)
    {
        if (impuestosReembolso == null) return;

        var impuestos = (from XmlElement item in impuestosReembolso
                         select new DetalleImpuesto
                         {
                             Codigo = item.GetElementsByTagName(TagCodigo)[0]?.InnerText ?? string.Empty,
                             CodigoPorcentaje = item.GetElementsByTagName(TagCodigoPorcentaje)[0]?.InnerText ?? string.Empty,
                             Tarifa = item.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                             BaseImponibleReembolso = item.GetElementsByTagName("baseImponibleReembolso")[0]?.InnerText ?? string.Empty,
                             ImpuestoReembolso = item.GetElementsByTagName("impuestoReembolso")[0]?.InnerText ?? string.Empty
                         }).ToList();

        reembolsoDetalles.CreateTax(impuestos);
    }

    private static void GetRetencionPayments(DocSustento doc, XmlNode? payments)
    {
        if (payments == null) return;

        var pagos = (from XmlNode item in payments select new Pago { FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty, Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty }).ToList();

        doc.CreatePayments(pagos);
    }

    private static void GetInfoLiquidacionCompra(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoLiquidacion)
    {
        var infoLiq = new InfoLiquidacionCompra
        {
            FechaEmision = nodeInfoLiquidacion.SelectSingleNode(TagFechaEmision)?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoLiquidacion.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoLiquidacion.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoLiquidacion.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
            TipoIdentificacionProveedor = nodeInfoLiquidacion.SelectSingleNode("tipoIdentificacionProveedor")?.InnerText ?? string.Empty,
            RazonSocialProveedor = nodeInfoLiquidacion.SelectSingleNode("razonSocialProveedor")?.InnerText ?? string.Empty,
            IdentificacionProveedor = nodeInfoLiquidacion.SelectSingleNode("identificacionProveedor")?.InnerText ?? string.Empty,
            DireccionProveedor = nodeInfoLiquidacion.SelectSingleNode("direccionProveedor")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoLiquidacion.SelectSingleNode(TagTotalSinImpuestos)?.InnerText ?? string.Empty,
            TotalDescuento = nodeInfoLiquidacion.SelectSingleNode("totalDescuento")?.InnerText ?? string.Empty,
            CodDocReembolso = nodeInfoLiquidacion.SelectSingleNode("codDocReembolso")?.InnerText ?? string.Empty,
            TotalComprobantesReembolso = nodeInfoLiquidacion.SelectSingleNode("totalComprobantesReembolso")?.InnerText ?? string.Empty,
            TotalBaseImponibleReembolso = nodeInfoLiquidacion.SelectSingleNode("totalBaseImponibleReembolso")?.InnerText ?? string.Empty,
            TotalImpuestoReembolso = nodeInfoLiquidacion.SelectSingleNode("totalImpuestoReembolso")?.InnerText ?? string.Empty,
            ImporteTotal = nodeInfoLiquidacion.SelectSingleNode("importeTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoLiquidacion.SelectSingleNode(TagMoneda)?.InnerText ?? string.Empty
        };

        GetTotalTaxes(codDoc, infoLiq, nodeInfoLiquidacion.SelectSingleNode("totalConImpuestos"));
        GetLiquidacionPayments(infoLiq, nodeInfoLiquidacion.SelectSingleNode(TagPagos));
        GetLiquidacionReembolsos(infoLiq, nodeInfoLiquidacion.SelectSingleNode("reembolsos"));

        ce.CreateInfoComp(codDoc, infoLiq);
    }

    private static void GetLiquidacionPayments(InfoLiquidacionCompra info, XmlNode? payments)
    {
        if (payments == null) return;
        info.CreatePayments(ParsePagos(payments));
    }

    private static void GetLiquidacionReembolsos(InfoLiquidacionCompra info, XmlNode? nodeReembolsos)
    {
        if (nodeReembolsos == null) return;
        info.CreateReembolsos(ParseReembolsos(nodeReembolsos));
    }

    private static void GetInfoNotaDebito(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoNotaDebito)
    {
        var infoNd = new InfoNotaDebito
        {
            FechaEmision = nodeInfoNotaDebito.SelectSingleNode(TagFechaEmision)?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoNotaDebito.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
            TipoIdentificacionComprador = nodeInfoNotaDebito.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoNotaDebito.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoNotaDebito.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoNotaDebito.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoNotaDebito.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
            Rise = nodeInfoNotaDebito.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            CodDocModificado = nodeInfoNotaDebito.SelectSingleNode("codDocModificado")?.InnerText ?? string.Empty,
            NumDocModificado = nodeInfoNotaDebito.SelectSingleNode("numDocModificado")?.InnerText ?? string.Empty,
            FechaEmisionDocSustento = nodeInfoNotaDebito.SelectSingleNode(TagFechaEmisionDocSustento)?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoNotaDebito.SelectSingleNode(TagTotalSinImpuestos)?.InnerText ?? string.Empty,
            ImpuestoTotal = nodeInfoNotaDebito.SelectSingleNode("valorTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoNotaDebito.SelectSingleNode(TagMoneda) == null ? "DOLAR" : nodeInfoNotaDebito.SelectSingleNode(TagMoneda)?.InnerText ?? string.Empty
        };

        GetTotalTaxes(codDoc, infoNd, nodeInfoNotaDebito.SelectSingleNode(TagImpuestos));
        GetNotaDebitoPayments(infoNd, nodeInfoNotaDebito.SelectSingleNode(TagPagos));

        ce.CreateInfoComp(codDoc, infoNd);
    }

    private static void GetMotivosNotaDebito(ComprobanteElectronico comp, XmlNode nodeMotivos)
    {
        if (nodeMotivos == null) return;

        var motivos = (from XmlNode item in nodeMotivos
                       select new MotivoNotaDebito
                       {
                           Razon = item.SelectSingleNode("razon")?.InnerText ?? string.Empty,
                           Valor = item.SelectSingleNode("valor")?.InnerText ?? string.Empty
                       }).ToList();

        comp.InfoNotaDebito?.CreateMotivos(motivos);
    }

    private static void GetNotaDebitoPayments(InfoNotaDebito info, XmlNode? payments)
    {
        if (payments == null) return;
        info.CreatePayments(ParsePagos(payments));
    }

    private static void GetInfoGuiaRemision(ComprobanteElectronico ce, XmlNode nodeInfoGuiaRemision)
    {
        var infoGr = new InfoGuiaRemision
        {
            DirEstablecimiento = nodeInfoGuiaRemision.SelectSingleNode(TagDirEstablecimiento)?.InnerText ?? string.Empty,
            DirPartida = nodeInfoGuiaRemision.SelectSingleNode("dirPartida")?.InnerText ?? string.Empty,
            RazonSocialTransportista = nodeInfoGuiaRemision.SelectSingleNode("razonSocialTransportista")?.InnerText ?? string.Empty,
            TipoIdentificacionTransportista = nodeInfoGuiaRemision.SelectSingleNode("tipoIdentificacionTransportista")?.InnerText ?? string.Empty,
            RucTransportista = nodeInfoGuiaRemision.SelectSingleNode("rucTransportista")?.InnerText ?? string.Empty,
            Rise = nodeInfoGuiaRemision.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoGuiaRemision.SelectSingleNode(TagObligadoContabilidad)?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoGuiaRemision.SelectSingleNode(TagContribuyenteEspecial)?.InnerText ?? string.Empty,
            FechaIniTransporte = nodeInfoGuiaRemision.SelectSingleNode("fechaIniTransporte")?.InnerText ?? string.Empty,
            FechaFinTransporte = nodeInfoGuiaRemision.SelectSingleNode("fechaFinTransporte")?.InnerText ?? string.Empty,
            Placa = nodeInfoGuiaRemision.SelectSingleNode("placa")?.InnerText ?? string.Empty
        };

        ce.CreateInfoComp("06", infoGr);
    }

    private static void GetDestinatarios(ComprobanteElectronico comp, XmlNode nodeDestinatarios)
    {
        if (nodeDestinatarios == null) return;

        var destinatarios = new List<Destinatario>();
        foreach (XmlElement item in nodeDestinatarios)
        {
            var destinatario = new Destinatario
            {
                IdentificacionDestinatario = item.GetElementsByTagName("identificacionDestinatario")[0]?.InnerText ?? string.Empty,
                RazonSocialDestinatario = item.GetElementsByTagName("razonSocialDestinatario")[0]?.InnerText ?? string.Empty,
                DirDestinatario = item.GetElementsByTagName("dirDestinatario")[0]?.InnerText ?? string.Empty,
                MotivoTraslado = item.GetElementsByTagName("motivoTraslado")[0]?.InnerText ?? string.Empty,
                DocAduaneroUnico = item.GetElementsByTagName("docAduaneroUnico")[0]?.InnerText ?? string.Empty,
                CodEstabDestino = item.GetElementsByTagName("codEstabDestino")[0]?.InnerText ?? string.Empty,
                Ruta = item.GetElementsByTagName("ruta")[0]?.InnerText ?? string.Empty,
                CodDocSustento = item.GetElementsByTagName("codDocSustento")[0]?.InnerText ?? string.Empty,
                NumDocSustento = item.GetElementsByTagName("numDocSustento")[0]?.InnerText ?? string.Empty,
                NumAutDocSustento = item.GetElementsByTagName("numAutDocSustento")[0]?.InnerText ?? string.Empty,
                FechaEmisionDocSustento = item.GetElementsByTagName(TagFechaEmisionDocSustento)[0]?.InnerText ?? string.Empty
            };

            GetDetallesDestinatario(destinatario, item.SelectSingleNode(TagDetalles));
            destinatarios.Add(destinatario);
        }

        comp.CreateDestinatarios(destinatarios);
    }

    private static void GetDetallesDestinatario(Destinatario destinatario, XmlNode? nodeDetalles)
    {
        if (nodeDetalles == null) return;

        var detalles = new List<DetalleDestinatario>();
        foreach (XmlElement item in nodeDetalles)
        {
            var detalle = new DetalleDestinatario
            {
                CodigoInterno = item.GetElementsByTagName("codigoInterno")[0]?.InnerText ?? string.Empty,
                CodigoAdicional = item.GetElementsByTagName("codigoAdicional")[0]?.InnerText ?? string.Empty,
                Descripcion = item.GetElementsByTagName("descripcion")[0]?.InnerText ?? string.Empty,
                Cantidad = item.GetElementsByTagName("cantidad")[0]?.InnerText ?? string.Empty
            };

            var nodeDetAdicionales = item.SelectSingleNode("detallesAdicionales");
            if (nodeDetAdicionales != null)
            {
                var detallesAdicionales = (from XmlElement detAd in nodeDetAdicionales
                                           select new DetalleAdicional
                                           {
                                               Nombre = detAd.GetAttribute("nombre"),
                                               Valor = detAd.InnerText
                                           }).ToList();
                detalle.CreateDetallesAdicionales(detallesAdicionales);
            }

            detalles.Add(detalle);
        }

        destinatario.CreateDetalles(detalles);
    }
}

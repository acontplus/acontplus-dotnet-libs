using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents;

public class DataXmlComprobante
{
    public bool GetData(XmlDocument xmlSri, ref ComprobanteElectronico comp, ref string message)
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

            switch (comp.CodDoc)
            {
                case "01":
                    var nodeFact = xmlComp.GetElementsByTagName("factura")[0];
                    comp.VersionComp = nodeFact?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoFactura = xmlComp.GetElementsByTagName("infoFactura")[0];
                    if (nodeInfoFactura != null) GetInfoFactura(comp.CodDoc, comp, nodeInfoFactura);

                    GetDetails(comp, xmlComp.GetElementsByTagName("detalles")[0]);

                    break;
                case "03":
                    var nodeLiq = xmlComp.GetElementsByTagName("liquidacionCompra")[0];
                    comp.VersionComp = nodeLiq?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoLiquidacion = xmlComp.GetElementsByTagName("infoLiquidacionCompra")[0];
                    if (nodeInfoLiquidacion != null) GetInfoLiquidacionCompra(comp.CodDoc, comp, nodeInfoLiquidacion);

                    GetDetails(comp, xmlComp.GetElementsByTagName("detalles")[0]);

                    break;
                case "04":
                    var nodeNc = xmlComp.GetElementsByTagName("notaCredito")[0];
                    comp.VersionComp = nodeNc?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoNotaCredito = xmlComp.GetElementsByTagName("infoNotaCredito")[0];

                    if (nodeInfoNotaCredito != null) GetInfoNotaCredito(comp.CodDoc, comp, nodeInfoNotaCredito);

                    GetDetails(comp, xmlComp.GetElementsByTagName("detalles")[0]);

                    break;
                case "05":
                    var nodeNd = xmlComp.GetElementsByTagName("notaDebito")[0];
                    comp.VersionComp = nodeNd?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoNotaDebito = xmlComp.GetElementsByTagName("infoNotaDebito")[0];
                    if (nodeInfoNotaDebito != null) GetInfoNotaDebito(comp.CodDoc, comp, nodeInfoNotaDebito);

                    var nodeMotivos = xmlComp.GetElementsByTagName("motivos")[0];
                    if (nodeMotivos != null) GetMotivosNotaDebito(comp, nodeMotivos);

                    break;
                case "06":
                    var nodeGr = xmlComp.GetElementsByTagName("guiaRemision")[0];
                    comp.VersionComp = nodeGr?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoGuiaRemision = xmlComp.GetElementsByTagName("infoGuiaRemision")[0];
                    if (nodeInfoGuiaRemision != null) GetInfoGuiaRemision(comp, nodeInfoGuiaRemision);

                    var nodeDestinatarios = xmlComp.GetElementsByTagName("destinatarios")[0];
                    if (nodeDestinatarios != null) GetDestinatarios(comp, nodeDestinatarios);

                    break;
                case "07":
                    var nodeRet = xmlComp.GetElementsByTagName("comprobanteRetencion")[0];
                    comp.VersionComp = nodeRet?.Attributes?["version"]?.Value ?? string.Empty;

                    var nodeInfoCompRetencion = xmlComp.GetElementsByTagName("infoCompRetencion")[0];

                    GetInfoCompRetencion(comp.VersionComp, comp, nodeInfoCompRetencion);

                    if (comp.VersionComp == "2.0.0")
                        GetDocSustento(comp, xmlComp.GetElementsByTagName("docsSustento")[0]);
                    else
                        GetImpuestoRetencion(comp, xmlComp.GetElementsByTagName("impuestos")[0]);

                    break;
            }

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

    private void GetInfoTributaria(ComprobanteElectronico ce, XmlNode nodeInfoTrib)
    {
        ce.InfoTributaria = new InfoTributaria
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

    public void GetInfoNotaCredito(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoNotaCredito)
    {
        var infoFac = new InfoNotaCredito
        {
            FechaEmision = nodeInfoNotaCredito.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoNotaCredito.SelectSingleNode("dirEstablecimiento") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
            TipoIdentificacionComprador =
                nodeInfoNotaCredito.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoNotaCredito.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoNotaCredito.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoNotaCredito.SelectSingleNode("contribuyenteEspecial") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoNotaCredito.SelectSingleNode("obligadoContabilidad") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
            Rise = nodeInfoNotaCredito.SelectSingleNode("rise") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            CodDocModificado = nodeInfoNotaCredito.SelectSingleNode("codDocModificado")?.InnerText ?? string.Empty,
            NumDocModificado = nodeInfoNotaCredito.SelectSingleNode("numDocModificado")?.InnerText ?? string.Empty,
            FechaEmisionDocSustento = nodeInfoNotaCredito.SelectSingleNode("fechaEmisionDocSustento") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("fechaEmisionDocSustento")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoNotaCredito.SelectSingleNode("totalSinImpuestos")?.InnerText ?? string.Empty,
            ValorModificacion = nodeInfoNotaCredito.SelectSingleNode("valorModificacion") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("valorModificacion")?.InnerText ?? string.Empty,
            Moneda = nodeInfoNotaCredito.SelectSingleNode("moneda") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("moneda")?.InnerText ?? string.Empty,
            Motivo = nodeInfoNotaCredito.SelectSingleNode("motivo") == null
                ? ""
                : nodeInfoNotaCredito.SelectSingleNode("motivo")?.InnerText ?? string.Empty
        };
        GetTotalTaxes(codDoc, infoFac, nodeInfoNotaCredito.SelectSingleNode("totalConImpuestos"));
        ce.CreateInfoComp(codDoc, infoFac);
    }

    public void GetInfoAdicional(ComprobanteElectronico comp, XmlNode? infoAdi)
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

    private void GetInfoFactura(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoFactura)
    {
        var infoFac = new InfoFactura();
        infoFac.FechaEmision = nodeInfoFactura.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty;
        infoFac.DirEstablecimiento = nodeInfoFactura.SelectSingleNode("dirEstablecimiento") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty;
        infoFac.ContribuyenteEspecial = nodeInfoFactura.SelectSingleNode("contribuyenteEspecial") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty;
        infoFac.ObligadoContabilidad = nodeInfoFactura.SelectSingleNode("obligadoContabilidad") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty;
        infoFac.TipoIdentificacionComprador =
            nodeInfoFactura.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty;
        infoFac.RazonSocialComprador = nodeInfoFactura.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty;
        infoFac.IdentificacionComprador = nodeInfoFactura.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty;
        infoFac.DireccionComprador = nodeInfoFactura.SelectSingleNode("direccionComprador") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("direccionComprador")?.InnerText ?? string.Empty;
        infoFac.GuiaRemision = nodeInfoFactura.SelectSingleNode("guiaRemision") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("guiaRemision")?.InnerText ?? string.Empty;
        infoFac.TotalSinImpuestos = nodeInfoFactura.SelectSingleNode("totalSinImpuestos")?.InnerText ?? string.Empty;
        infoFac.TotalDescuento = nodeInfoFactura.SelectSingleNode("totalDescuento")?.InnerText ?? string.Empty;
        infoFac.Propina = nodeInfoFactura.SelectSingleNode("propina") == null
            ? "0.00"
            : nodeInfoFactura.SelectSingleNode("propina")?.InnerText ?? string.Empty;
        infoFac.ImporteTotal = nodeInfoFactura.SelectSingleNode("importeTotal")?.InnerText ?? string.Empty;
        infoFac.Moneda = nodeInfoFactura.SelectSingleNode("moneda") == null
            ? ""
            : nodeInfoFactura.SelectSingleNode("moneda")?.InnerText ?? string.Empty;

        GetTotalTaxes(codDoc, infoFac, nodeInfoFactura.SelectSingleNode("totalConImpuestos"));

        if (nodeInfoFactura.SelectSingleNode("pagos") != null)
            GetInvoicePayments(infoFac, nodeInfoFactura.SelectSingleNode("pagos"));

        ce.CreateInfoComp(codDoc, infoFac);
    }

    private void GetTotalTaxes(string codDoc, object obj, XmlNode? impuestos)
    {
        if (impuestos == null) return;

        var totalImpuestos = (from XmlNode item in impuestos
                              select new TotalImpuesto
                              {
                                  Codigo = item.SelectSingleNode("codigo")?.InnerText ?? string.Empty,
                                  CodigoPorcentaje = item.SelectSingleNode("codigoPorcentaje")?.InnerText ?? string.Empty,
                                  DescuentoAdicional = item.SelectSingleNode("descuentoAdicional") == null
                                      ? "0.00"
                                      : item.SelectSingleNode("descuentoAdicional")?.InnerText ?? string.Empty,
                                  BaseImponible = item.SelectSingleNode("baseImponible")?.InnerText ?? string.Empty,
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

    private void GetInvoicePayments(InfoFactura comp, XmlNode? payments)
    {
        if (payments == null) return;

        var pagos = (from XmlNode item in payments
                     select new Pago
                     {
                         FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty,
                         Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty,
                         Plazo = item.SelectSingleNode("plazo") == null ? "" : item.SelectSingleNode("plazo")?.InnerText ?? string.Empty,
                         UnidadTiempo = item.SelectSingleNode("unidadTiempo") == null
                             ? ""
                             : item.SelectSingleNode("unidadTiempo")?.InnerText ?? string.Empty
                     }).ToList();

        comp.CreatePayments(pagos);
    }

    private void GetDetails(ComprobanteElectronico comp, XmlNode? details)
    {
        if (details == null) return;

        var detalles = new List<Detalle>();
        var impuestos = new List<Impuesto>();
        var idDetalle = 0;
        foreach (XmlNode item in details)
        {
            var detail = new Detalle { IdDetalle = idDetalle };
            detail.CodigoPrincipal = comp.CodDoc == "01"
                ? item.SelectSingleNode("codigoPrincipal") == null
                    ? ""
                    : item.SelectSingleNode("codigoPrincipal")?.InnerText ?? string.Empty
                : item.SelectSingleNode("codigoInterno") == null
                    ? ""
                    : item.SelectSingleNode("codigoInterno")?.InnerText ?? string.Empty;

            detail.CodigoAuxiliar = item.SelectSingleNode("codigoAuxiliar") == null
                ? ""
                : item.SelectSingleNode("codigoAuxiliar")?.InnerText ?? string.Empty;
            detail.Descripcion = item.SelectSingleNode("descripcion")?.InnerText ?? string.Empty;
            detail.Cantidad = item.SelectSingleNode("cantidad")?.InnerText ?? string.Empty;
            detail.PrecioUnitario = item.SelectSingleNode("precioUnitario")?.InnerText ?? string.Empty;
            detail.Descuento = item.SelectSingleNode("descuento") == null
                ? ""
                : item.SelectSingleNode("descuento")?.InnerText ?? string.Empty;
            detail.PrecioTotalSinImpuesto = item.SelectSingleNode("precioTotalSinImpuesto")?.InnerText ?? string.Empty;
            detail.Impuestos = item.SelectSingleNode("impuestos") == null
                ? ""
                : item.SelectNodes("impuestos")?[0]?.OuterXml ?? string.Empty;
            detail.DetallesAdicionales = item.SelectSingleNode("detallesAdicionales") == null
                ? ""
                : item.SelectNodes("detallesAdicionales")?[0]?.OuterXml ?? string.Empty;

            var taxesNodes = item.SelectNodes("impuestos");
            if (taxesNodes != null)
            {
                impuestos.AddRange(from XmlElement taxes in taxesNodes
                                   select new Impuesto
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

            detalles.Add(detail);
            idDetalle++;
        }

        comp.CreateTaxes(impuestos);

        comp.CreateDetails(detalles);
    }

    private void GetInfoCompRetencion(string? versionComp, ComprobanteElectronico ce, XmlNode? nodeInfoCompRetencion)
    {
        if (nodeInfoCompRetencion != null)
        {
            var infoRet = new InfoCompRetencion
            {
                FechaEmision = nodeInfoCompRetencion.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty,
                DirEstablecimiento = nodeInfoCompRetencion.SelectSingleNode("dirEstablecimiento") == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
                ContribuyenteEspecial = nodeInfoCompRetencion.SelectSingleNode("contribuyenteEspecial") == null
                    ? ""
                    : nodeInfoCompRetencion.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
                ObligadoContabilidad = nodeInfoCompRetencion.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
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

    private void GetImpuestoRetencion(ComprobanteElectronico comp, XmlNode? impuestosRet)
    {
        if (impuestosRet == null) return;

        var impuestos = new List<ImpuestoRetencion>();
        foreach (XmlElement taxes in impuestosRet)
        {
            var tax = new ImpuestoRetencion
            {
                Codigo = taxes.GetElementsByTagName("codigo")[0]?.InnerText ?? string.Empty,
                CodigoRetencion = taxes.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = taxes.GetElementsByTagName("baseImponible")[0]?.InnerText ?? string.Empty,
                PorcentajeRetener = taxes.GetElementsByTagName("porcentajeRetener")[0]?.InnerText ?? string.Empty,
                ValorRetenido = taxes.GetElementsByTagName("valorRetenido")[0]?.InnerText ?? string.Empty,
                CodDocSustento = taxes.GetElementsByTagName("codDocSustento")[0]?.InnerText ?? string.Empty,
                NumDocSustento = taxes.GetElementsByTagName("numDocSustento")[0]?.InnerText ?? string.Empty,
                FechaEmisionDocSustento = taxes.GetElementsByTagName("fechaEmisionDocSustento")[0]?.InnerText ?? string.Empty
            };
            impuestos.Add(tax);
        }

        comp.CreateRetencionTaxes(impuestos);
    }

    private void GetDocSustento(ComprobanteElectronico ce, XmlNode? nodeDocsSustento)
    {
        if (nodeDocsSustento == null) return;

        var docsSustento = new List<DocSustento>();
        foreach (XmlElement item in nodeDocsSustento)
        {
            var docSustento = new DocSustento
            {
                CodSustento = item.GetElementsByTagName("codSustento")[0]?.InnerText ?? string.Empty,
                CodDocSustento = item.GetElementsByTagName("codDocSustento")[0]?.InnerText ?? string.Empty,
                NumDocSustento = item.GetElementsByTagName("numDocSustento")[0]?.InnerText ?? string.Empty,
                FechaEmisionDocSustento = item.GetElementsByTagName("fechaEmisionDocSustento")[0]?.InnerText ?? string.Empty,
                NumAutDocSustento = item.SelectSingleNode("numAutDocSustento") == null
                    ? ""
                    : item.GetElementsByTagName("numAutDocSustento")[0]?.InnerText ?? string.Empty,
                PagoLocExt = item.GetElementsByTagName("pagoLocExt")[0]?.InnerText ?? string.Empty,
                TipoRegi = item.SelectSingleNode("tipoRegi") == null
                    ? ""
                    : item.GetElementsByTagName("tipoRegi")[0]?.InnerText ?? string.Empty,
                PaisEfecPago = item.SelectSingleNode("paisEfecPago") == null
                    ? ""
                    : item.GetElementsByTagName("paisEfecPago")[0]?.InnerText ?? string.Empty,
                AplicConvDobTrib = item.SelectSingleNode("aplicConvDobTrib") == null
                    ? ""
                    : item.GetElementsByTagName("aplicConvDobTrib")[0]?.InnerText ?? string.Empty,
                PagExtSujRetNorLeg = item.SelectSingleNode("pagExtSujRetNorLeg") == null
                    ? ""
                    : item.GetElementsByTagName("pagExtSujRetNorLeg")[0]?.InnerText ?? string.Empty,
                PagoRegFis = item.SelectSingleNode("pagoRegFis") == null
                    ? ""
                    : item.GetElementsByTagName("pagoRegFis")[0]?.InnerText ?? string.Empty,
                TotalComprobantesReembolso = item.SelectSingleNode("totalComprobantesReembolso") == null
                    ? ""
                    : item.GetElementsByTagName("totalComprobantesReembolso")[0]?.InnerText ?? string.Empty,
                TotalBaseImponibleReembolso = item.SelectSingleNode("totalBaseImponibleReembolso") == null
                    ? ""
                    : item.GetElementsByTagName("totalBaseImponibleReembolso")[0]?.InnerText ?? string.Empty,
                TotalImpuestoReembolso = item.SelectSingleNode("totalImpuestoReembolso") == null
                    ? ""
                    : item.GetElementsByTagName("totalImpuestoReembolso")[0]?.InnerText ?? string.Empty,
                TotalSinImpuestos = item.GetElementsByTagName("totalSinImpuestos")[0]?.InnerText ?? string.Empty,
                ImporteTotal = item.GetElementsByTagName("importeTotal")[0]?.InnerText ?? string.Empty
            };
            GetImpuestoDocSustento(docSustento, item.SelectSingleNode("impuestosDocSustento"));
            GetRetenciones(docSustento, item.SelectSingleNode("retenciones"), ce);
            if (docSustento.CodDocSustento == "41") GetReembolsos(docSustento, item.SelectSingleNode("reembolsos"));

            if (item.SelectSingleNode("pagos") != null)
                GetRetencionPayments(docSustento, item.SelectSingleNode("pagos"));

            docsSustento.Add(docSustento);
        }

        ce.CreateDocSustentos(docsSustento);
    }

    private void GetImpuestoDocSustento(DocSustento doc, XmlNode? nodeImpuestos)
    {
        if (nodeImpuestos == null) return;

        var impuestos = (from XmlElement item in nodeImpuestos
                         select new ImpuestoDocSustento
                         {
                             CodImpuestoDocSustento = item.GetElementsByTagName("codImpuestoDocSustento")[0]?.InnerText ?? string.Empty,
                             CodigoPorcentaje = item.GetElementsByTagName("codigoPorcentaje")[0]?.InnerText ?? string.Empty,
                             BaseImponible = item.GetElementsByTagName("baseImponible")[0]?.InnerText ?? string.Empty,
                             Tarifa = item.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                             ValorImpuesto = item.GetElementsByTagName("valorImpuesto")[0]?.InnerText ?? string.Empty
                         }).ToList();

        doc.CreateTax(impuestos);
    }

    private void GetRetenciones(DocSustento doc, XmlNode? nodeRetenciones, ComprobanteElectronico ce)
    {
        if (nodeRetenciones == null) return;

        var retenciones = new List<Retencion>();
        var retToView = new List<ImpuestoRetencion>();

        foreach (XmlElement item in nodeRetenciones)
        {
            var retencion = new Retencion
            {
                Codigo = item.GetElementsByTagName("codigo")[0]?.InnerText ?? string.Empty,
                CodigoRetencion = item.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = item.GetElementsByTagName("baseImponible")[0]?.InnerText ?? string.Empty,
                PorcentajeRetener = item.GetElementsByTagName("porcentajeRetener")[0]?.InnerText ?? string.Empty,
                ValorRetenido = item.GetElementsByTagName("valorRetenido")[0]?.InnerText ?? string.Empty
            };
            retenciones.Add(retencion);

            var ir = new ImpuestoRetencion
            {
                Codigo = item.GetElementsByTagName("codigo")[0]?.InnerText ?? string.Empty,
                CodigoRetencion = item.GetElementsByTagName("codigoRetencion")[0]?.InnerText ?? string.Empty,
                BaseImponible = item.GetElementsByTagName("baseImponible")[0]?.InnerText ?? string.Empty,
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

    private void GetReembolsos(DocSustento doc, XmlNode? nodeReemb)
    {
        if (nodeReemb == null) return;

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
                NumeroAutorizacionDocReemb = item.GetElementsByTagName("numeroAutorizacionDocReemb")[0]?.InnerText ?? string.Empty
            };
            GetImpuestosReembolsos(reembolso, item.SelectSingleNode("detalleImpuestos"));
            reembolsos.Add(reembolso);
        }

        doc.CreateReembolsos(reembolsos);
    }

    private void GetImpuestosReembolsos(ReembolsoDetalle reembolsoDetalles, XmlNode? impuestosReembolso)
    {
        if (impuestosReembolso == null) return;

        var impuestos = (from XmlElement item in impuestosReembolso
                         select new DetalleImpuesto
                         {
                             Codigo = item.GetElementsByTagName("codigo")[0]?.InnerText ?? string.Empty,
                             CodigoPorcentaje = item.GetElementsByTagName("codigoPorcentaje")[0]?.InnerText ?? string.Empty,
                             Tarifa = item.GetElementsByTagName("tarifa")[0]?.InnerText ?? string.Empty,
                             BaseImponibleReembolso = item.GetElementsByTagName("baseImponibleReembolso")[0]?.InnerText ?? string.Empty,
                             ImpuestoReembolso = item.GetElementsByTagName("impuestoReembolso")[0]?.InnerText ?? string.Empty
                         }).ToList();

        reembolsoDetalles.CreateTax(impuestos);
    }

    private void GetRetencionPayments(DocSustento doc, XmlNode? payments)
    {
        if (payments == null) return;

        var pagos = (from XmlNode item in payments select new Pago { FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty, Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty }).ToList();

        doc.CreatePayments(pagos);
    }

    private void GetInfoLiquidacionCompra(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoLiquidacion)
    {
        var infoLiq = new InfoLiquidacionCompra
        {
            FechaEmision = nodeInfoLiquidacion.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoLiquidacion.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoLiquidacion.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoLiquidacion.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
            TipoIdentificacionProveedor = nodeInfoLiquidacion.SelectSingleNode("tipoIdentificacionProveedor")?.InnerText ?? string.Empty,
            RazonSocialProveedor = nodeInfoLiquidacion.SelectSingleNode("razonSocialProveedor")?.InnerText ?? string.Empty,
            IdentificacionProveedor = nodeInfoLiquidacion.SelectSingleNode("identificacionProveedor")?.InnerText ?? string.Empty,
            DireccionProveedor = nodeInfoLiquidacion.SelectSingleNode("direccionProveedor")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoLiquidacion.SelectSingleNode("totalSinImpuestos")?.InnerText ?? string.Empty,
            TotalDescuento = nodeInfoLiquidacion.SelectSingleNode("totalDescuento")?.InnerText ?? string.Empty,
            CodDocReembolso = nodeInfoLiquidacion.SelectSingleNode("codDocReembolso")?.InnerText ?? string.Empty,
            TotalComprobantesReembolso = nodeInfoLiquidacion.SelectSingleNode("totalComprobantesReembolso")?.InnerText ?? string.Empty,
            TotalBaseImponibleReembolso = nodeInfoLiquidacion.SelectSingleNode("totalBaseImponibleReembolso")?.InnerText ?? string.Empty,
            TotalImpuestoReembolso = nodeInfoLiquidacion.SelectSingleNode("totalImpuestoReembolso")?.InnerText ?? string.Empty,
            ImporteTotal = nodeInfoLiquidacion.SelectSingleNode("importeTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoLiquidacion.SelectSingleNode("moneda")?.InnerText ?? string.Empty
        };

        GetTotalTaxes(codDoc, infoLiq, nodeInfoLiquidacion.SelectSingleNode("totalConImpuestos"));
        GetLiquidacionPayments(infoLiq, nodeInfoLiquidacion.SelectSingleNode("pagos"));
        GetLiquidacionReembolsos(infoLiq, nodeInfoLiquidacion.SelectSingleNode("reembolsos"));

        ce.CreateInfoComp(codDoc, infoLiq);
    }

    private void GetLiquidacionPayments(InfoLiquidacionCompra info, XmlNode? payments)
    {
        if (payments == null) return;

        var pagos = (from XmlNode item in payments
                     select new Pago
                     {
                         FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty,
                         Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty,
                         Plazo = item.SelectSingleNode("plazo")?.InnerText ?? string.Empty,
                         UnidadTiempo = item.SelectSingleNode("unidadTiempo")?.InnerText ?? string.Empty
                     }).ToList();

        info.CreatePayments(pagos);
    }

    private void GetLiquidacionReembolsos(InfoLiquidacionCompra info, XmlNode? nodeReembolsos)
    {
        if (nodeReembolsos == null) return;

        var reembolsos = new List<ReembolsoDetalle>();
        foreach (XmlElement item in nodeReembolsos)
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
                NumeroAutorizacionDocReemb = item.GetElementsByTagName("numeroautorizacionDocReemb")[0]?.InnerText ?? string.Empty
            };
            GetImpuestosReembolsos(reembolso, item.SelectSingleNode("detalleImpuestos"));
            reembolsos.Add(reembolso);
        }

        info.CreateReembolsos(reembolsos);
    }

    private void GetInfoNotaDebito(string codDoc, ComprobanteElectronico ce, XmlNode nodeInfoNotaDebito)
    {
        var infoNd = new InfoNotaDebito
        {
            FechaEmision = nodeInfoNotaDebito.SelectSingleNode("fechaEmision")?.InnerText ?? string.Empty,
            DirEstablecimiento = nodeInfoNotaDebito.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
            TipoIdentificacionComprador = nodeInfoNotaDebito.SelectSingleNode("tipoIdentificacionComprador")?.InnerText ?? string.Empty,
            RazonSocialComprador = nodeInfoNotaDebito.SelectSingleNode("razonSocialComprador")?.InnerText ?? string.Empty,
            IdentificacionComprador = nodeInfoNotaDebito.SelectSingleNode("identificacionComprador")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoNotaDebito.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoNotaDebito.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
            Rise = nodeInfoNotaDebito.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            CodDocModificado = nodeInfoNotaDebito.SelectSingleNode("codDocModificado")?.InnerText ?? string.Empty,
            NumDocModificado = nodeInfoNotaDebito.SelectSingleNode("numDocModificado")?.InnerText ?? string.Empty,
            FechaEmisionDocSustento = nodeInfoNotaDebito.SelectSingleNode("fechaEmisionDocSustento")?.InnerText ?? string.Empty,
            TotalSinImpuestos = nodeInfoNotaDebito.SelectSingleNode("totalSinImpuestos")?.InnerText ?? string.Empty,
            ImpuestoTotal = nodeInfoNotaDebito.SelectSingleNode("valorTotal")?.InnerText ?? string.Empty,
            Moneda = nodeInfoNotaDebito.SelectSingleNode("moneda") == null ? "DOLAR" : nodeInfoNotaDebito.SelectSingleNode("moneda")?.InnerText ?? string.Empty
        };

        GetTotalTaxes(codDoc, infoNd, nodeInfoNotaDebito.SelectSingleNode("impuestos"));
        GetNotaDebitoPayments(infoNd, nodeInfoNotaDebito.SelectSingleNode("pagos"));

        ce.CreateInfoComp(codDoc, infoNd);
    }

    private void GetMotivosNotaDebito(ComprobanteElectronico comp, XmlNode nodeMotivos)
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

    private void GetNotaDebitoPayments(InfoNotaDebito info, XmlNode? payments)
    {
        if (payments == null) return;

        var pagos = (from XmlNode item in payments
                     select new Pago
                     {
                         FormaPago = item.SelectSingleNode("formaPago")?.InnerText ?? string.Empty,
                         Total = item.SelectSingleNode("total")?.InnerText ?? string.Empty,
                         Plazo = item.SelectSingleNode("plazo")?.InnerText ?? string.Empty,
                         UnidadTiempo = item.SelectSingleNode("unidadTiempo")?.InnerText ?? string.Empty
                     }).ToList();

        info.CreatePayments(pagos);
    }

    private void GetInfoGuiaRemision(ComprobanteElectronico ce, XmlNode nodeInfoGuiaRemision)
    {
        var infoGr = new InfoGuiaRemision
        {
            DirEstablecimiento = nodeInfoGuiaRemision.SelectSingleNode("dirEstablecimiento")?.InnerText ?? string.Empty,
            DirPartida = nodeInfoGuiaRemision.SelectSingleNode("dirPartida")?.InnerText ?? string.Empty,
            RazonSocialTransportista = nodeInfoGuiaRemision.SelectSingleNode("razonSocialTransportista")?.InnerText ?? string.Empty,
            TipoIdentificacionTransportista = nodeInfoGuiaRemision.SelectSingleNode("tipoIdentificacionTransportista")?.InnerText ?? string.Empty,
            RucTransportista = nodeInfoGuiaRemision.SelectSingleNode("rucTransportista")?.InnerText ?? string.Empty,
            Rise = nodeInfoGuiaRemision.SelectSingleNode("rise")?.InnerText ?? string.Empty,
            ObligadoContabilidad = nodeInfoGuiaRemision.SelectSingleNode("obligadoContabilidad")?.InnerText ?? string.Empty,
            ContribuyenteEspecial = nodeInfoGuiaRemision.SelectSingleNode("contribuyenteEspecial")?.InnerText ?? string.Empty,
            FechaIniTransporte = nodeInfoGuiaRemision.SelectSingleNode("fechaIniTransporte")?.InnerText ?? string.Empty,
            FechaFinTransporte = nodeInfoGuiaRemision.SelectSingleNode("fechaFinTransporte")?.InnerText ?? string.Empty,
            Placa = nodeInfoGuiaRemision.SelectSingleNode("placa")?.InnerText ?? string.Empty
        };

        ce.CreateInfoComp("06", infoGr);
    }

    private void GetDestinatarios(ComprobanteElectronico comp, XmlNode nodeDestinatarios)
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
                FechaEmisionDocSustento = item.GetElementsByTagName("fechaEmisionDocSustento")[0]?.InnerText ?? string.Empty
            };

            GetDetallesDestinatario(destinatario, item.SelectSingleNode("detalles"));
            destinatarios.Add(destinatario);
        }

        comp.CreateDestinatarios(destinatarios);
    }

    private void GetDetallesDestinatario(Destinatario destinatario, XmlNode? nodeDetalles)
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
    }}

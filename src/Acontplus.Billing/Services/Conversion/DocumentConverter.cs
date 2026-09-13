using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;
using System.Globalization;

namespace Acontplus.Billing.Services.Conversion;

public class DocumentConverter : IDocumentConverter
{
    public string CreateHtml(ComprobanteElectronico comprobanteElectronico)
    {
        var assembly = typeof(DocumentConverter).Assembly;
        byte[] imageBytes;
        string resourceName = "Common.Billing.Resources.Images.logo-generic.png";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                // Si no encontramos el recurso con el nombre exacto, buscaremos entre todos los recursos
                var resourceNames = assembly.GetManifestResourceNames();
                string? matchingResource = null;

                foreach (var name in resourceNames)
                {
                    if (name.EndsWith("logo-generic.png"))
                    {
                        matchingResource = name;
                        break;
                    }
                }

                if (matchingResource != null)
                {
                    using var foundStream = assembly.GetManifestResourceStream(matchingResource);
                    if (foundStream != null)
                    {
                        using var ms = new MemoryStream();
                        foundStream.CopyTo(ms);
                        imageBytes = ms.ToArray();
                    }
                    else
                    {
                        throw new FileNotFoundException($"No se pudo cargar el recurso '{matchingResource}'.");
                    }
                }
                else
                {
                    // Si no encontramos el recurso, proporcionamos información de diagnóstico
                    var availableResources = string.Join(", ", resourceNames);
                    throw new FileNotFoundException(
                        $"No se pudo encontrar el recurso embebido 'logo-generic.png'. " +
                        $"Recursos disponibles: {availableResources}");
                }
            }
            else
            {
                // Leer el stream en un array de bytes
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                imageBytes = ms.ToArray();
            }
        }

        // Convertir la imagen a base64 para incluirla en el HTML
        var base64Image = Convert.ToBase64String(imageBytes);

        var dt = (SriDocument)Convert.ToInt32(comprobanteElectronico.CodDoc);
        var documentName = dt.DisplayName();
        var doc = @"
                    <div class=""row"">
                        <div class=""col-xl-6 col-lg-6 col-md-6 col-sm-6 col-6"">
                            <div class=""card border border-dark rounded"">
                                <div class=""card-body"">
<div class=""text-center"">
                                    <img src=" +
                  $"data:image/png;base64,{base64Image}" +
                  @" alt=""No Logo"" class=""img-fluid"" align=""center"" width=""100"">
</div>
                                    <h6>" + comprobanteElectronico.InfoTributaria.RazonSocial + @"</h6>

                                    <h6>" + comprobanteElectronico.InfoTributaria.NombreComercial + @"</h6>
                                    <br>
                                    <h6><label>Dirección Matriz:</label> " + comprobanteElectronico.InfoTributaria.DirMatriz + @"</h6>
                                    <br>
                                    " + GetInfoTrib(comprobanteElectronico) + @"
                                </div>
                            </div>
                        </div>
                        <div class=""col-xl-6 col-lg-6 col-md-6 col-sm-6 col-6"">
                            <div class=""card border border-dark rounded"">
                                <div class=""card-body"">
                                    <h5><label>RUC: </label> "
                  + comprobanteElectronico.InfoTributaria.Ruc + @"</h5>
                                    <h4>" + documentName + @"</h4>
                                    <h5>
                                        <label>No.</label>
                                        " + comprobanteElectronico.InfoTributaria.Estab + @"-" + comprobanteElectronico.InfoTributaria.PtoEmi + @"-" +
                  comprobanteElectronico.InfoTributaria.Secuencial + @"
                                    </h5>
                                    <label>NÚMERO DE AUTORIZACIÓN</label><br>
                                    <small id=""claveAcceso"">
                                        " + comprobanteElectronico.InfoTributaria.ClaveAcceso + @"
                                    </small>
                                    <label>FECHA Y HORA DE AUTORIZACIÓN</label>
                                    <small>
                                        " + comprobanteElectronico.FechaAutorizacion + @"
                                    </small>
                                    <h5>
                                        <label>AMBIENTE:</label>
                                         " + (comprobanteElectronico.InfoTributaria.Ambiente == "1" ? "PRUEBAS" : "PRODUCCIÓN") + @"
                                    </h5>
                                    <h5>
                                        <label>EMISIÓN:</label>
                                        " + (comprobanteElectronico.InfoTributaria.TipoEmision == "1" ? "NORMAL" : "") + @"
                                    </h5>
                                    <label>CLAVE DE ACCESSO</label><br />
<div class=""text-center"">
                                    <img id=""barcodeImg"" src=" +
                  $"data:image/png;base64,{Convert.ToBase64String(BarcodeGen.Create(new BarcodeConfig { Text = comprobanteElectronico.InfoTributaria.ClaveAcceso }), 0, BarcodeGen.Create(new BarcodeConfig { Text = comprobanteElectronico.InfoTributaria.ClaveAcceso }).Length)}" +
                  @" alt="""" align=""center"" class=""img-fluid"" height=""60"" width=""500"">
                                    <br><small> " + comprobanteElectronico.InfoTributaria.ClaveAcceso + @"</small></div>
                                </div>

                            </div>

                        </div>
                    </div> " + GetInfoComprobante(comprobanteElectronico) + @"
                    </div>
                    <div class=""row"">

                           " + GetInfoAdicional(comprobanteElectronico) + GetTotals(comprobanteElectronico) + @"

                    </div>
                    <div class=""row"">
                        <div class=""col-xl-12 col-lg-12 col-md-12 col-sm-12 col-12"">
                            <div class=""form-group"">
                                <label for=""obs"">Observación:</label>
                                <textarea name=""obs"" id=""obs"" class=""form-control"" ng-model=""Factura.obs""></textarea>
                            </div>
                        </div>
                    </div>";
        return doc;
    }

    private static string GetInfoTrib(ComprobanteElectronico data)
    {
        var infoTrib = data.CodDoc switch
        {
            "01" when data.InfoFactura is { } f => @"<h6>
                                        <label>Dirección Sucursal: </label>
                                        " + f.DirEstablecimiento + @"
                                    </h6>
                                    <br>
                                    <h6>
                                        <label>
                                            Contribuyente
                                            Especial Nro.
                                        </label>
                                        " + f.ContribuyenteEspecial + @"
                                    </h6>
                                    <h6>
                                        <label>OBLIGADO A LLEVAR CONTABILIDAD </label>
                                        " + f.ObligadoContabilidad + @"
                                    </h6>",
            "04" when data.InfoNotaCredito is { } nc => @"<h5>
                                        <label>Dirección Sucursal: </label>
                                        " + nc.DirEstablecimiento + @"
                                    </h5>
                                    <br>
                                    <h5 ng-show="""">
                                        <label data.infoFactura.contribuyenteEspecial.length > 0"">
                                            Contribuyente
                                            Especial Nro.
                                        </label>
                                        " + nc.ContribuyenteEspecial + @"
                                    </h5>
                                    <h5>
                                        <label>OBLIGADO A LLEVAR CONTABILIDAD </label>
                                        " + nc.ObligadoContabilidad + @"
                                    </h5>",
            "07" when data.InfoCompRetencion is { } cr => @"<h5>
                                        <label>Dirección Sucursal: </label>
                                        " + cr.DirEstablecimiento + @"
                                    </h5>
                                    <br>
                                    <h5 ng-show="""">
                                        <label data.infoFactura.contribuyenteEspecial.length > 0"">
                                            Contribuyente
                                            Especial Nro.
                                        </label>
                                        " + cr.ContribuyenteEspecial + @"
                                    </h5>
                                    <h5>
                                        <label>OBLIGADO A LLEVAR CONTABILIDAD </label>
                                        " + cr.ObligadoContabilidad + @"
                                    </h5>",
            _ => string.Empty
        };

        return infoTrib;
    }

    private static string GetInfoComprobante(ComprobanteElectronico data)
    {
        var infoComp = string.Empty;
        switch (data.CodDoc)
        {
            case "01" when data.InfoFactura is { } f:
                infoComp = @" <div class=""row"">
                        <div class=""col-xl-12 col-lg-12 col-md-12 col-sm-12 col-12"">
                            <div class=""card border border-dark rounded"">
                                <div class=""card-body"">
                                    <div class=""row"">
                                        <div class=""col-xl-8 col-lg-8 col-md-8 col-sm-8 col-8"">
                                            <h5>
                                                <label>Razón Social / Nombres y Apellidos: </label>
                                                " + f.RazonSocialComprador + @"
                                            </h5>
                                            <br>
                                            <h5>
                                                <label>Fecha Emisión: </label>
                                                " + f.FechaEmision + @"
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                <label>Identificación: </label>
                                                " + f.IdentificacionComprador + @"
                                            </h5><br>

                                                <h5> <label>Guía Remisión: </label>
                                                " + f.GuiaRemision + @"
                                            </h5>
                                        </div>
                                    </div>
                                    <hr>
                                    <div class=""row"">
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                <label>Dirección: </label>
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                " + f.DireccionComprador + @"
                                            </h5>
                                        </div>
                                    </div>                                 </div>
                            </div>
                        </div>
                        <br>" + @"<div class=""table-responsive"">
                                        <table class=""table table-borderded"">
                                            <thead>
                                                <tr>
                                                    <th>Código</th>
                                                    <th>Código Auxiliar</th>
                                                    <th>Cantidad</th>
                                                    <th>Descripción</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Precio Unitario</th>
                                                    <th>Descuento</th>
                                                    <th>Precio Total</th>
                                                </tr>
                                            </thead>
                                            <tbody>" + GetDetails(data) + @"</tbody>
                                        </table>
                                    </div>";
                ;
                break;
            case "04" when data.InfoNotaCredito is { } nc:
                infoComp = @" <div class=""row"">
                        <div class=""col-xl-12 col-lg-12 col-md-12 col-sm-12 col-12"">
                            <div class=""card border border-dark rounded"">
                                <div class=""card-body"">
                                    <div class=""row"">
                                        <div class=""col-xl-8 col-lg-8 col-md-8 col-sm-8 col-8"">
                                            <h5>
                                                <label>Razón Social / Nombres y Apellidos: </label>
                                                " + nc.RazonSocialComprador + @"
                                            </h5>
                                            <br>
                                            <h5>
                                                <label>Fecha Emisión: </label>
                                                " + nc.FechaEmision + @"
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                <label>Identificación: </label>
                                                " + nc.IdentificacionComprador + @"
                                            </h5>
                                        </div>
                                    </div>
                                    <hr>
                                    <div class=""row"">
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                <label>Comprobante que se modifica: </label>
                                             </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                " + nc.CodDocModificado + @"
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                " + nc.NumDocModificado + @"
                                            </h5>
                                        </div>
                                    </div>

                                    <div class=""row"">
                                        <div class=""col-xl-6 col-lg-6 col-md-6 col-sm-6 col-6"">
                                            <h5>
                                                <label>Fecha Emisión comprobante a modificar: </label>
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                " + nc.FechaEmisionDocSustento + @"
                                            </h5>
                                        </div>
                                    </div>
                                    <div class=""row"">
                                        <div class=""col-xl-6 col-lg-6 col-md-6 col-sm-6 col-6"">
                                            <h5>
                                                <label>Razón de Modificación: </label>
                                            </h5>
                                        </div>
                                        <div class=""col-xl-4 col-lg-4 col-md-4 col-sm-4 col-4"">
                                            <h5>
                                                " + nc.Motivo + @"
                                            </h5>
                                        </div>
                                    </div>
 <div class=""table-responsive"">
                                        <table class=""table table-borderded"">
                                            <thead>
                                                <tr>
                                                    <th>Código</th>
                                                    <th>Código Auxiliar</th>
                                                    <th>Cantidad</th>
                                                    <th>Descripción</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Detalle Adicional</th>
                                                    <th>Precio Unitario</th>
                                                    <th>Descuento</th>
                                                    <th>Precio Total</th>
                                                </tr>
                                            </thead>
                                            <tbody>" + GetDetails(data);

                infoComp += @"</tbody>
                                        </table>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <br>";
                break;
        }

        return infoComp;
    }

    private static string GetDetails(ComprobanteElectronico data)
    {
        var details = string.Empty;
        details = data.CodDoc switch
        {
            "01" or "04" => (data.Detalles ?? Enumerable.Empty<Detalle>()).Aggregate(details, (current, item) => current + @"<tr class=""small"">
                                                    <td> " + item.CodigoPrincipal + @" </td >
                                                    <td> " + item.CodigoAuxiliar + @" </td >
                                                    <td> " + item.Cantidad + @" </td>
                                                    <td> " + item.Descripcion + @" </td >
                                                    <td></td>
                                                    <td></td>
                                                    <td></td>
                                                    <td class=""text-right"">" + item.PrecioUnitario + @"</td>
                                                    <td class=""text-right"">" + item.Descuento + @"</td>
                                                    <td class=""text-right"">" + item.PrecioTotalSinImpuesto + @"</td>
                                                </tr>"),
            _ => details
        };

        return details;
    }

    private static string GetInfoAdicional(ComprobanteElectronico data)
    {
        if (data.InfoAdicional == null) return string.Empty;

        var info = data.InfoAdicional.Aggregate(
            @"<div class=""col-xl-7 col-lg-7 col-md-7 col-sm-7 col-7""><div class=""card border border-dark rounded"" ng-show=""infoAdicional.length > 0"">
                                <div class=""card-body"">

                                                <table class=""table table-borderless"">
                                        <thead>
                                            <tr>
                                                <th>Nombre</th>
                                                <th>Valor</th>
                                            </tr>
                                        </thead>
                                        <tbody>", (current, item) => current + @"<tr><td>" + item.Nombre + @"</td>
                <td>" + item.Valor + @"</td ></tr>");

        info += @"</tbody></table></div></div>
                        </div>";
        return info;
    }

    private static string GetTotals(ComprobanteElectronico data)
    {
        double subtotalIva0 = 0;
        double subtotal12 = 0;
        var subtotalNoObjetoIva = 0.00;
        var subtotalExcentoIva = 0.00;
        var ice = 0.00;
        var iva12 = 0.00;
        var irbpnr = 0.00;

        var totals = @"<div class=""col-xl-5 col-lg-5 col-md-5 col-sm-5 col-5"">
                            <div class=""table-responsive"">
                                <h5><strong></strong></h5>
                                <table class=""table table-bordered"">
                                    <tbody>";

        switch (data.CodDoc)
        {
            case "01":
                if (data.InfoFactura != null)
                {
                    if (data.InfoFactura.TotalImpuestos != null)
                    {
                        foreach (var item in data.InfoFactura.TotalImpuestos)
                        {
                            if (item.Codigo == "2" && item.CodigoPorcentaje == "2")
                                totals += @"<tr>
                                            <td class=""text-right""><strong>SubTotal Iva 12%</strong></td>
                                            <td class=""text-right"">" +
                                          Convert.ToString(subtotal12 += Convert.ToDouble(item.BaseImponible),
                                              CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";
                            if (item.Codigo == "2" && item.CodigoPorcentaje == "4")
                                totals += @"<tr>
                                            <td class=""text-right""><strong>SubTotal Iva 15%</strong></td>
                                            <td class=""text-right"">" +
                                          Convert.ToString(subtotal12 += Convert.ToDouble(item.BaseImponible),
                                              CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";

                            if (item.Codigo == "2" && item.CodigoPorcentaje == "0")
                                totals += @"<tr>
                                            <td class=""text-right""><strong>SubTotal Iva 0%</strong></td>
                                            <td class=""text-right"">" +
                                          Convert.ToString(subtotalIva0 += Convert.ToDouble(item.BaseImponible),
                                              CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";

                            if (item.Codigo == "2" && item.CodigoPorcentaje == "6")
                                totals += @"<tr>
                                            <td class=""text-right""><strong>SubTotal No Objeto Iva</strong></td>
                                            <td class=""text-right"">" +
                                          Convert.ToString(subtotalNoObjetoIva += Convert.ToDouble(item.BaseImponible), CultureInfo.InvariantCulture) +
                                          @"</td>
                                        </tr>";

                            if (item.Codigo == "2" && item.CodigoPorcentaje == "7")
                                totals += @"<tr>
                                            <td class=""text-right""><strong>SubTotal Excento Iva</strong></td>
                                            <td class=""text-right"">" +
                                          Convert.ToString(subtotalExcentoIva += Convert.ToDouble(item.BaseImponible), CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";

                            totals += @"<tr>
                                            <td class=""text-right""><strong>Total Sin Impuestos</strong></td>
                                            <td class=""text-right"">" + data.InfoFactura.TotalSinImpuestos + @"</td>
                                        </tr>";
                            switch (item.Codigo)
                            {
                                case "3":
                                    totals += @"<tr>
                                            <td class=""text-right""><strong>ICE</strong></td>
                                            <td class=""text-right"">" +
                                              Convert.ToString(ice += Convert.ToDouble(item.Valor),
                                                  CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";
                                    break;
                                case "2" when item.CodigoPorcentaje == "2":
                                    totals += @"<tr>
                                            <td class=""text-right""><strong>IVA 12%</strong></td>
                                            <td class=""text-right"">" +
                                              Convert.ToString(iva12 += Convert.ToDouble(item.Valor),
                                                  CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";
                                    break;
                                case "5":
                                    totals += @"<tr>
                                            <td class=""text-right""><strong>IRBPNR</strong></td>
                                            <td class=""text-right"">" +
                                              Convert.ToString(irbpnr += Convert.ToDouble(item.Valor),
                                                  CultureInfo.InvariantCulture) + @"</td>
                                        </tr>";
                                    break;
                            }
                        }
                    }

                    totals += @" <tr><td class=""text-right""><strong>Propina</strong></td>
                                            <td class=""text-right"">" + data.InfoFactura.Propina + @"</td>
                                        </tr>";
                    totals += @" <tr><td class=""text-right""><strong>Valor Total</strong></td>
                                            <td class=""text-right"">" + data.InfoFactura.ImporteTotal + @"</td>
                                        </tr>";
                }
                break;
            case "04":

                break;
        }
        ;
        totals += @"</tbody>
                                </table>
                            </div>
                            <h4 class=""float-right""><strong></strong></h4><br>
                            <h4 class=""float-right""><strong></strong></h4>
                        </div>";
        return totals;
    }
}

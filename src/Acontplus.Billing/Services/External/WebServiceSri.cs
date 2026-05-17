using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Responses;

namespace Acontplus.Billing.Services.External;

public class WebServiceSri : IWebServiceSri
{
    //SRI AUTORIZA EL COMPROBANTE
    public async Task<ResponseSri> AuthorizationAsync(string claveAcceso, string url)
    {
        var responseSri = new ResponseSri();
        try
        {
            var xml =
                $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                            <soapenv:Body>
                                <ec:autorizacionComprobante>
                                   <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
                                </ec:autorizacionComprobante>
                             </soapenv:Body>
                             </soapenv:Envelope>";

            using var sriService = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
            using var response =
                await sriService.PostAsync(url, new StringContent(xml, Encoding.UTF8, "text/xml"));
            await using var streamResponse = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(streamResponse);
            responseSri.XmlSri = await streamReader.ReadToEndAsync();

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);
            var estadoComp = doc.GetElementsByTagName("estado");

            var nlNroCompAuth = doc.GetElementsByTagName("numeroComprobantes");

            var nroCompNode = nlNroCompAuth.Count > 0 ? nlNroCompAuth[0] : null;
            var nroComp = nroCompNode?.InnerText ?? string.Empty;

            var estadoCompNode = estadoComp.Count > 0 ? estadoComp[0] : null;
            responseSri.Estado = nroComp == "0" ? "NO AUTORIZADO" : estadoCompNode?.InnerText ?? string.Empty;

            switch (responseSri.Estado)
            {
                case "AUTORIZADO":
                    {
                        var codAutorizacion = doc.GetElementsByTagName("numeroAutorizacion");
                        responseSri.CodigoAutorizacion = codAutorizacion.Count > 0 ? codAutorizacion[0]?.InnerText : null;
                        var xFecha = doc.GetElementsByTagName("fechaAutorizacion");
                        responseSri.FechaAutorizacion = xFecha.Count > 0 ? xFecha[0]?.InnerText : null;
                        responseSri.Message = "EL COMPROBANTE FUE AUTORIZADO CON ÉXITO";
                        break;
                    }

                case "EN PROCESO":
                    {
                        responseSri.Message = "EL COMPROBANTE ESTA EN PROCESO";
                        break;
                    }

                default:
                    {
                        var xmessage = doc.GetElementsByTagName("mensaje");
                        if (xmessage.Count > 0)
                        {
                            var messageNode = xmessage[0] as XmlElement;
                            var nodos = messageNode?.ChildNodes;
                            if (nodos != null)
                                foreach (XmlElement nodo in nodos)
                                    switch (nodo.Name)
                                    {
                                        case "identificador":
                                            responseSri.Identificador = nodo.InnerText;
                                            break;
                                        case "mensaje":
                                            responseSri.Message = nodo.InnerText;
                                            break;
                                        case "informacionAdicional":
                                            responseSri.InformacionAdicional = nodo.InnerText;
                                            break;
                                        case "tipo":
                                            responseSri.Tipo = nodo.InnerText;
                                            break;
                                    }
                        }

                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            _ = ex.Message;
            responseSri.Estado = "ERROR";
            responseSri.Message = "No se pudo autorizar el comprobante";
        }

        return responseSri;
    }

    //SRI AUTORIZACION POR LOTE
    public async Task<ResponseSri> AuthorizationLoteAsync(string claveAcceso, string url)
    {
        var responseSri = new ResponseSri();
        try
        {
            var xml =
                $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                            <soapenv:Body>
                                <ec:autorizacionComprobanteLote>
                                   <claveAccesoLote>{claveAcceso}</claveAccesoLote>
                                </ec:autorizacionComprobanteLote>
                             </soapenv:Body>
                             </soapenv:Envelope>";

            using var sriService = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
            using var response = await sriService.PostAsync(url, new StringContent(xml, Encoding.UTF8, "text/xml"));
            await using var streamResponse = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(streamResponse);
            responseSri.XmlSri = await streamReader.ReadToEndAsync();

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);
        }
        catch (Exception ex)
        {
            _ = ex.Message;
            responseSri.Estado = "ERROR";
            responseSri.Message = "No se pudo al autorizar el lote";
        }

        return responseSri;
    }

    //VERIFICA SI YA EXISTE EL COMPROBANTE EN EL SRI
    public async Task<ResponseSri> CheckExistenceAsync(string claveAcceso, string url)
    {
        var responseSri = new ResponseSri();
        try
        {
            var xml =
                $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                            <soapenv:Body>
                                <ec:autorizacionComprobante>
                                   <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
                                </ec:autorizacionComprobante>
                             </soapenv:Body>
                             </soapenv:Envelope>";

            using var sriService = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
            using var response =
                await sriService.PostAsync(url, new StringContent(xml, Encoding.UTF8, "text/xml"));
            await using var streamResponse = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(streamResponse);
            responseSri.XmlSri = await streamReader.ReadToEndAsync();

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);

            var numeroComprobantes = doc.GetElementsByTagName("numeroComprobantes");
            if (numeroComprobantes.Count > 0)
            {
                if (numeroComprobantes[0]?.InnerText == "1")
                {
                    var xEstado = doc.GetElementsByTagName("estado");
                    var estadoNode = xEstado.Count > 0 ? xEstado[0] : null;

                    switch (estadoNode?.InnerText)
                    {
                        case "AUTORIZADO":
                            {
                                responseSri.Estado = estadoNode?.InnerText;
                                responseSri.Message = "EL COMPROBANTE  YA FUE AUTORIZADO";
                                var xNumAuto = doc.GetElementsByTagName("numeroAutorizacion");
                                responseSri.CodigoAutorizacion = xNumAuto.Count > 0 ? xNumAuto[0]?.InnerText : null;
                                var xFecha = doc.GetElementsByTagName("fechaAutorizacion");
                                responseSri.FechaAutorizacion = xFecha.Count > 0 ? xFecha[0]?.InnerText : null;
                                break;
                            }
                        default:
                            {
                                responseSri.Estado = estadoNode?.InnerText;
                                var xmessage = doc.GetElementsByTagName("mensaje");
                                if (xmessage.Count > 0)
                                {
                                    var messageNode = xmessage[0] as XmlElement;
                                    var nodos = messageNode?.ChildNodes;
                                    if (nodos != null)
                                        foreach (XmlElement nodo in nodos)
                                            switch (nodo.Name)
                                            {
                                                case "identificador":
                                                    responseSri.Identificador = nodo.InnerText;
                                                    break;
                                                case "mensaje":
                                                    responseSri.Message = nodo.InnerText;
                                                    break;
                                                case "informacionAdicional":
                                                    responseSri.InformacionAdicional = nodo.InnerText;
                                                    break;
                                                case "tipo":
                                                    responseSri.Tipo = nodo.InnerText;
                                                    break;
                                            }
                                }

                                break;
                            }
                    }
                }
                else
                {
                    responseSri.Estado = "NO EXISTE";
                }
            }
            else
            {
                var estadoNoAuth = doc.GetElementsByTagName("estado");
                if (estadoNoAuth.Count > 0) responseSri.Estado = estadoNoAuth[0]?.InnerText;
            }
        }
        catch (Exception ex)
        {
            responseSri.Estado = "ERROR";
            responseSri.Message = "No se pudo verificar la existencia del comprobante: " + ex;
        }

        return responseSri;
    }

    //DESCARGAR XML DESDE EL SRI
    public async Task<string> GetXmlAsync(string claveAcceso, string url)
    {
        var xmlSri = string.Empty;
        try
        {
            var xmlRequest = string.Format(
                @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                            <soapenv:Body>
                                <ec:autorizacionComprobante>
                                   <claveAccesoComprobante>{0}</claveAccesoComprobante>
                                </ec:autorizacionComprobante>
                             </soapenv:Body>
                             </soapenv:Envelope>", claveAcceso);

            using var sriLClient = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
            using var response =
                await sriLClient.PostAsync(url, new StringContent(xmlRequest, Encoding.UTF8, "text/xml"));
            await using var streamResponse = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(streamResponse);
            xmlSri = await streamReader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _ = ex.Message;
        }

        return xmlSri;
    }

    //SRI RECIBE EL XML DE LOS COMPROBANTES
    public async Task<ResponseSri> ReceptionAsync(string xmlSigned, string url)
    {
        var responseSri = new ResponseSri();
        try
        {
            var xml =
                $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.recepcion"">
                            <soapenv:Header/>
                            <soapenv:Body>
                               <ec:validarComprobante>
                                   <xml>{Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlSigned))}</xml>
                                </ec:validarComprobante>
                             </soapenv:Body>
                             </soapenv:Envelope>";


            using var sriService = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
            using var response =
                await sriService.PostAsync(url, new StringContent(xml, Encoding.UTF8, "text/xml"));
            await using var streamResponse = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(streamResponse);
            responseSri.XmlSri = await streamReader.ReadToEndAsync();

            if (DataValidation.IsValidXml(responseSri.XmlSri))
            {
                //OBTIENE DATO DEL XML RESPONSE
                var xdoc = new XmlDocument();
                xdoc.LoadXml(responseSri.XmlSri);

                var xEstado = xdoc.GetElementsByTagName("estado");
                var estadoNode = xEstado.Count > 0 ? xEstado[0] : null;
                responseSri.Estado = estadoNode?.InnerText ?? string.Empty;

                var identificador = xdoc.GetElementsByTagName("identificador");
                var idNode = identificador.Count > 0 ? identificador[0] : null;
                responseSri.Identificador = idNode?.InnerText ?? string.Empty;

                if (responseSri.Estado == "DEVUELTA")
                {
                    xEstado = xdoc.GetElementsByTagName("mensaje");
                    if (xEstado.Count > 0)
                    {
                        var messageNode = xEstado[0] as XmlElement;
                        var nodos = messageNode?.ChildNodes;
                        if (nodos != null)
                            foreach (XmlElement nodo in nodos)
                                switch (nodo.Name)
                                {
                                    case "identificador":
                                        responseSri.Identificador = nodo.InnerText;
                                        break;
                                    case "mensaje":
                                        responseSri.Message = nodo.InnerText;
                                        break;
                                    case "informacionAdicional":
                                        responseSri.InformacionAdicional = nodo.InnerText;
                                        break;
                                    case "tipo":
                                        responseSri.Tipo = nodo.InnerText;
                                        break;
                                }
                    }
                }
            }
            else
            {
                responseSri.Estado = "ERROR";
                responseSri.Message = "SRI no se encuentra en línea";
            }
        }
        catch (Exception)
        {
            responseSri.Estado = "ERROR";
            responseSri.Message = "SRI no se encuentra en línea";
        }

        return responseSri;
    }
}

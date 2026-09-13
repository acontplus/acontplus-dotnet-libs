using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Responses;

namespace Acontplus.Billing.Services.External;

public class WebServiceSri : IWebServiceSri
{
    private const string StatusError = "ERROR";
    private const string TagEstado = "estado";
    private const string TagMensaje = "mensaje";

    //SRI AUTORIZA EL COMPROBANTE
    public async Task<ResponseSri> AuthorizationAsync(string claveAcceso, string url)
    {
        var responseSri = new ResponseSri();
        try
        {
            var xml = BuildAuthorizationXml(claveAcceso);
            responseSri.XmlSri = await PostSoapXmlAsync(url, xml);

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);
            ParseAuthorizationDocument(doc, responseSri);
        }
        catch (Exception)
        {
            responseSri.Estado = StatusError;
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

            responseSri.XmlSri = await PostSoapXmlAsync(url, xml);

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);
        }
        catch (Exception)
        {
            responseSri.Estado = StatusError;
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
            var xml = BuildAuthorizationXml(claveAcceso);
            responseSri.XmlSri = await PostSoapXmlAsync(url, xml);

            var doc = new XmlDocument();
            doc.LoadXml(responseSri.XmlSri);
            ParseExistenceDocument(doc, responseSri);
        }
        catch (Exception ex)
        {
            responseSri.Estado = StatusError;
            responseSri.Message = "No se pudo verificar la existencia del comprobante: " + ex;
        }

        return responseSri;
    }

    //DESCARGAR XML DESDE EL SRI
    public async Task<string> GetXmlAsync(string claveAcceso, string url)
    {
        try
        {
            var xmlRequest = BuildAuthorizationXml(claveAcceso);
            return await PostSoapXmlAsync(url, xmlRequest);
        }
        catch (Exception)
        {
            return string.Empty;
        }
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

            responseSri.XmlSri = await PostSoapXmlAsync(url, xml);

            if (DataValidation.IsValidXml(responseSri.XmlSri))
            {
                var xdoc = new XmlDocument();
                xdoc.LoadXml(responseSri.XmlSri);
                ParseReceptionDocument(xdoc, responseSri);
            }
            else
            {
                responseSri.Estado = StatusError;
                responseSri.Message = "SRI no se encuentra en línea";
            }
        }
        catch (Exception)
        {
            responseSri.Estado = StatusError;
            responseSri.Message = "SRI no se encuentra en línea";
        }

        return responseSri;
    }

    private static void ParseAuthorizationDocument(XmlDocument doc, ResponseSri responseSri)
    {
        var estadoComp = doc.GetElementsByTagName(TagEstado);
        var nlNroCompAuth = doc.GetElementsByTagName("numeroComprobantes");
        var nroCompNode = nlNroCompAuth.Count > 0 ? nlNroCompAuth[0] : null;
        var nroComp = nroCompNode?.InnerText ?? string.Empty;

        var estadoCompNode = estadoComp.Count > 0 ? estadoComp[0] : null;
        responseSri.Estado = nroComp == "0" ? "NO AUTORIZADO" : estadoCompNode?.InnerText ?? string.Empty;

        switch (responseSri.Estado)
        {
            case "AUTORIZADO":
                var codAutorizacion = doc.GetElementsByTagName("numeroAutorizacion");
                responseSri.CodigoAutorizacion = codAutorizacion.Count > 0 ? codAutorizacion[0]?.InnerText : null;
                var xFecha = doc.GetElementsByTagName("fechaAutorizacion");
                responseSri.FechaAutorizacion = xFecha.Count > 0 ? xFecha[0]?.InnerText : null;
                responseSri.Message = "EL COMPROBANTE FUE AUTORIZADO CON ÉXITO";
                break;

            case "EN PROCESO":
                responseSri.Message = "EL COMPROBANTE ESTA EN PROCESO";
                break;

            default:
                var xmessage = doc.GetElementsByTagName(TagMensaje);
                if (xmessage.Count > 0 && xmessage[0] is XmlElement messageNode)
                {
                    PopulateMessageDetails(responseSri, messageNode.ChildNodes);
                }
                break;
        }
    }

    private static void ParseExistenceDocument(XmlDocument doc, ResponseSri responseSri)
    {
        var numeroComprobantes = doc.GetElementsByTagName("numeroComprobantes");
        if (numeroComprobantes.Count > 0)
        {
            if (numeroComprobantes[0]?.InnerText == "1")
            {
                var xEstado = doc.GetElementsByTagName(TagEstado);
                var estadoNode = xEstado.Count > 0 ? xEstado[0] : null;

                if (estadoNode?.InnerText == "AUTORIZADO")
                {
                    responseSri.Estado = estadoNode.InnerText;
                    responseSri.Message = "EL COMPROBANTE  YA FUE AUTORIZADO";
                    var xNumAuto = doc.GetElementsByTagName("numeroAutorizacion");
                    responseSri.CodigoAutorizacion = xNumAuto.Count > 0 ? xNumAuto[0]?.InnerText : null;
                    var xFecha = doc.GetElementsByTagName("fechaAutorizacion");
                    responseSri.FechaAutorizacion = xFecha.Count > 0 ? xFecha[0]?.InnerText : null;
                }
                else
                {
                    responseSri.Estado = estadoNode?.InnerText;
                    var xmessage = doc.GetElementsByTagName(TagMensaje);
                    if (xmessage.Count > 0 && xmessage[0] is XmlElement messageNode)
                    {
                        PopulateMessageDetails(responseSri, messageNode.ChildNodes);
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
            var estadoNoAuth = doc.GetElementsByTagName(TagEstado);
            if (estadoNoAuth.Count > 0)
            {
                responseSri.Estado = estadoNoAuth[0]?.InnerText;
            }
        }
    }

    private static void ParseReceptionDocument(XmlDocument xdoc, ResponseSri responseSri)
    {
        var xEstado = xdoc.GetElementsByTagName(TagEstado);
        var estadoNode = xEstado.Count > 0 ? xEstado[0] : null;
        responseSri.Estado = estadoNode?.InnerText ?? string.Empty;

        var identificador = xdoc.GetElementsByTagName("identificador");
        var idNode = identificador.Count > 0 ? identificador[0] : null;
        responseSri.Identificador = idNode?.InnerText ?? string.Empty;

        if (responseSri.Estado == "DEVUELTA")
        {
            var xMensaje = xdoc.GetElementsByTagName(TagMensaje);
            if (xMensaje.Count > 0 && xMensaje[0] is XmlElement messageNode)
            {
                PopulateMessageDetails(responseSri, messageNode.ChildNodes);
            }
        }
    }

    private static void PopulateMessageDetails(ResponseSri responseSri, XmlNodeList? nodos)
    {
        if (nodos == null) return;

        foreach (XmlElement nodo in nodos)
        {
            switch (nodo.Name)
            {
                case "identificador":
                    responseSri.Identificador = nodo.InnerText;
                    break;
                case TagMensaje:
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

    private static async Task<string> PostSoapXmlAsync(string url, string xml)
    {
        using var sriService = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
        using var response = await sriService.PostAsync(url, new StringContent(xml, Encoding.UTF8, "text/xml"));
        await using var streamResponse = await response.Content.ReadAsStreamAsync();
        using var streamReader = new StreamReader(streamResponse);
        return await streamReader.ReadToEndAsync();
    }

    private static string BuildAuthorizationXml(string claveAcceso) =>
        $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ec=""http://ec.gob.sri.ws.autorizacion"">
                    <soapenv:Body>
                        <ec:autorizacionComprobante>
                           <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
                        </ec:autorizacionComprobante>
                     </soapenv:Body>
                     </soapenv:Envelope>";
}

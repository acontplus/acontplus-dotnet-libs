using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents
{
    public class XmlSriFileService : IXmlSriFileService
    {
        private const string TagFechaEmision = "fechaEmision";
        private const string TagVersionComp = "version";

        public async Task<XmlSriFileModel?> GetAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            string rawXml;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                rawXml = await reader.ReadToEndAsync();
            }

            return await GetAsync(rawXml);
        }

        public async Task<XmlSriFileModel?> GetAsync(string xmlSri)
        {
            if (string.IsNullOrWhiteSpace(xmlSri))
                throw new ArgumentException("XML string is null or empty", nameof(xmlSri));

            // Limpiar el XML usando la nueva función
            var rawXml = XmlValidator.CleanXmlForSqlServer(xmlSri);

            var xmlDocSri = new XmlDocument();
            xmlDocSri.LoadXml(rawXml);

            var nodeAuth = xmlDocSri.GetElementsByTagName("autorizacion")?[0];
            var nodeComp = nodeAuth?.SelectSingleNode("comprobante");

            if (nodeComp == null)
                return null;

            // Decodificar y limpiar el contenido interno del comprobante
            var xmlInterno = WebUtility.HtmlDecode(nodeComp.InnerText);

            // Aplicar todas las sanitizaciones para evitar errores de parsing
            xmlInterno = XmlValidator.CleanXmlForSqlServer(xmlInterno);

            var xmlComprobante = new XmlDocument();
            xmlComprobante.LoadXml(xmlInterno);

            var infoTributariaNode = xmlComprobante.GetElementsByTagName("infoTributaria")?[0];
            if (infoTributariaNode == null)
                return null;

            var claveAcceso = infoTributariaNode.SelectSingleNode("claveAcceso")?.InnerText ?? string.Empty;
            var codDoc = infoTributariaNode.SelectSingleNode("codDoc")?.InnerText ?? string.Empty;
            var fechaAutorizacion = xmlDocSri.SelectSingleNode("//fechaAutorizacion")?.InnerText ?? string.Empty;

            string versionComp, fechaEmision;

            if (string.IsNullOrEmpty(codDoc))
                throw new InvalidOperationException("codDoc no encontrado.");

            if (codDoc == "06") // Guía de remisión usa fecha de autorización
            {
                SetVersionAndFechaEmision(xmlComprobante, codDoc, out versionComp, out _);
                fechaEmision = fechaAutorizacion;
            }
            else
            {
                SetVersionAndFechaEmision(xmlComprobante, codDoc, out versionComp, out fechaEmision);
            }

            // Reemplazar el contenido del nodo <comprobante> con CDATA limpio
            nodeComp.InnerXml = $"<![CDATA[{xmlInterno}]]>";

            // Generar nuevo documento final
            var xmlFinal = new XmlDocument();
            xmlFinal.LoadXml(nodeAuth!.OuterXml);

            return await Task.FromResult(new XmlSriFileModel
            {
                CodDoc = codDoc,
                ClaveAcceso = claveAcceso,
                FechaEmision = fechaEmision,
                VersionComp = versionComp,
                XmlSri = xmlFinal,
                XmlComprobante = xmlComprobante
            });
        }

        private void SetVersionAndFechaEmision(XmlDocument xmlComprobante, string codDoc, out string versionComp,
            out string fechaEmision)
        {
            switch (codDoc)
            {
                case "01":
                    versionComp = GetAttributeValue(xmlComprobante, "factura", TagVersionComp);
                    fechaEmision = GetInnerText(xmlComprobante, "infoFactura", TagFechaEmision);
                    break;
                case "03":
                    versionComp = GetAttributeValue(xmlComprobante, "liquidacionCompra", TagVersionComp);
                    fechaEmision = GetInnerText(xmlComprobante, "infoLiquidacionCompra", TagFechaEmision);
                    break;
                case "04":
                    versionComp = GetAttributeValue(xmlComprobante, "notaCredito", TagVersionComp);
                    fechaEmision = GetInnerText(xmlComprobante, "infoNotaCredito", TagFechaEmision);
                    break;
                case "05":
                    versionComp = GetAttributeValue(xmlComprobante, "notaDebito", TagVersionComp);
                    fechaEmision = GetInnerText(xmlComprobante, "infoNotaDebito", TagFechaEmision);
                    break;
                case "06":
                    versionComp = GetAttributeValue(xmlComprobante, "guiaRemision", TagVersionComp);
                    fechaEmision = string.Empty; // se asignará desde la autorización
                    break;
                case "07":
                    versionComp = GetAttributeValue(xmlComprobante, "comprobanteRetencion", TagVersionComp);
                    fechaEmision = GetInnerText(xmlComprobante, "infoCompRetencion", TagFechaEmision);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported CodDoc: {codDoc}");
            }
        }

        private string GetAttributeValue(XmlDocument xmlDocument, string tagName, string attributeName)
        {
            return xmlDocument.GetElementsByTagName(tagName)[0]?.Attributes?[attributeName]?.Value ??
                   throw new InvalidOperationException($"Attribute '{attributeName}' not found in tag '{tagName}'");
        }

        private string GetInnerText(XmlDocument xmlDocument, string parentTagName, string childTagName)
        {
            return xmlDocument.GetElementsByTagName(parentTagName)[0]?.SelectSingleNode(childTagName)?.InnerText ??
                   throw new InvalidOperationException($"Tag '{childTagName}' not found in '{parentTagName}'");
        }
    }
}


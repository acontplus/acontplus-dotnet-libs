using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Conversion;

public class ComprobanteElectronicoParser : IXmlDocumentParser<ComprobanteElectronico>
{
    private readonly IDictionary<string, IDocumentTypeParser> _documentTypeParsers;
    private readonly IInfoTributariaParser _infoTributariaParser;
    private readonly IInfoAdicionalParser _infoAdicionalParser;

    public ComprobanteElectronicoParser(
        IDictionary<string, IDocumentTypeParser> documentTypeParsers,
        IInfoTributariaParser infoTributariaParser,
        IInfoAdicionalParser infoAdicionalParser)
    {
        _documentTypeParsers = documentTypeParsers ?? throw new ArgumentNullException(nameof(documentTypeParsers));
        _infoTributariaParser = infoTributariaParser ?? throw new ArgumentNullException(nameof(infoTributariaParser));
        _infoAdicionalParser = infoAdicionalParser ?? throw new ArgumentNullException(nameof(infoAdicionalParser));
    }

    public bool TryParse(XmlDocument xmlDocument, out ComprobanteElectronico result, out string errorMessage)
    {
        errorMessage = string.Empty;
        result = new ComprobanteElectronico();

        try
        {
            var nodeAuth = xmlDocument.GetElementsByTagName("autorizacion")[0];
            if (nodeAuth == null)
            {
                errorMessage = "No authorization node found in document";
                return false;
            }

            // Parse authorization info
            result.NumeroAutorizacion = nodeAuth.SelectSingleNode("numeroAutorizacion")?.InnerText ?? string.Empty;
            result.FechaAutorizacion = nodeAuth.SelectSingleNode("fechaAutorizacion")?.InnerText ?? string.Empty;

            // Extract and load the comprobante
            var nodeComp = nodeAuth.SelectSingleNode("comprobante");
            if (nodeComp == null)
            {
                errorMessage = "No comprobante node found in document";
                return false;
            }

            // Sanitizar el XML antes de cargarlo para evitar errores de parsing
            var cleanedXml = XmlValidator.CleanXmlForSqlServer(nodeComp.InnerText);

            var xmlComp = new XmlDocument();
            xmlComp.LoadXml(cleanedXml);

            // Parse basic tributary info
            var nodeInfoTrib = xmlComp.GetElementsByTagName("infoTributaria")[0];
            if (nodeInfoTrib != null)
            {
                result.CodDoc = nodeInfoTrib.SelectSingleNode("codDoc")?.InnerText ?? string.Empty;
                _infoTributariaParser.Parse(nodeInfoTrib, result);
            }
            else
            {
                errorMessage = "No tributary information found in document";
                return false;
            }

            // Find the appropriate parser for the document type
            if (string.IsNullOrEmpty(result.CodDoc) || !_documentTypeParsers.TryGetValue(result.CodDoc, out var documentTypeParser))
            {
                errorMessage = $"Unsupported document type: {result.CodDoc}";
                return false;
            }

            // Parse document-specific content
            if (!documentTypeParser.Parse(xmlComp, result, out errorMessage))
            {
                return false;
            }

            // Parse additional information if present
            var nodeInfoAdicional = xmlComp.GetElementsByTagName("infoAdicional")[0];
            if (nodeInfoAdicional != null)
            {
                _infoAdicionalParser.Parse(nodeInfoAdicional, result);
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error parsing electronic document: {ex.Message}";
            return false;
        }
    }
}

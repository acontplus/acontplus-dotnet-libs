using Acontplus.Billing.Interfaces.Services;
using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents;

/// <summary>
/// Service that delegates electronic document parsing to an <see cref="IXmlDocumentParser{T}"/>.
/// </summary>
/// <param name="parser">The generic XML document parser.</param>
public class ElectronicDocumentService(IXmlDocumentParser<ComprobanteElectronico> parser) : IElectronicDocumentService
{
    private readonly IXmlDocumentParser<ComprobanteElectronico> _parser =
        parser ?? throw new ArgumentNullException(nameof(parser));

    /// <inheritdoc />
    public bool TryParseDocument(XmlDocument xmlSri, out ComprobanteElectronico comprobante, out string errorMessage)
    {
        return _parser.TryParse(xmlSri, out comprobante, out errorMessage);
    }
}
using Acontplus.Billing.Interfaces.Services;

namespace Acontplus.Billing.Services.Conversion;


/// <summary>
/// Factory that registers and instantiates document-specific XML parsers indexed by document type code.
/// </summary>
public class DocumentParserFactory
{
    private readonly IDetailsParser _detailsParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParserFactory"/> class.
    /// </summary>
    /// <param name="detailsParser">The line details parser to inject into document parsers.</param>
    public DocumentParserFactory(IDetailsParser detailsParser)
    {
        _detailsParser = detailsParser ?? throw new ArgumentNullException(nameof(detailsParser));
    }

    /// <summary>
    /// Creates and returns a dictionary of document type parsers keyed by SRI document type code.
    /// </summary>
    /// <returns>A dictionary of document type parsers.</returns>
    public IDictionary<string, IDocumentTypeParser> CreateDocumentParsers()
    {
        return new Dictionary<string, IDocumentTypeParser>
        {
            { "01", new FacturaDocumentParser(_detailsParser) },
            // Add other parser implementations for different document types
            // { "03", new LiquidacionCompraParser() },
            // { "04", new NotaCreditoParser(_detailsParser) },
            // etc.
        };
    }
}
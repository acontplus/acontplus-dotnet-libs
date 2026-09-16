namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Generic contract for parsing an XML document into a strongly-typed model.
/// </summary>
/// <typeparam name="T">The parsed document model type.</typeparam>
public interface IXmlDocumentParser<T> where T : class
{
    /// <summary>
    /// Attempts to parse an XML document into the target model type.
    /// </summary>
    /// <param name="xmlDocument">The source XML document.</param>
    /// <param name="result">The parsed result object if successful.</param>
    /// <param name="errorMessage">Output error message if parsing fails.</param>
    /// <returns><c>true</c> if successfully parsed; otherwise <c>false</c>.</returns>
    bool TryParse(XmlDocument xmlDocument, out T result, out string errorMessage);
}
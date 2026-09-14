using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Service contract for extracting and parsing SRI XML files from uploads or strings.
/// </summary>
public interface IXmlSriFileService
{
    /// <summary>
    /// Parses an uploaded SRI XML file into an <see cref="XmlSriFileModel"/>.
    /// </summary>
    /// <param name="file">The uploaded form file.</param>
    /// <returns>The parsed model, or null if invalid.</returns>
    Task<XmlSriFileModel?> GetAsync(IFormFile file);

    /// <summary>
    /// Parses an SRI XML string into an <see cref="XmlSriFileModel"/>.
    /// </summary>
    /// <param name="xmlSri">The raw XML string.</param>
    /// <returns>The parsed model, or null if invalid.</returns>
    Task<XmlSriFileModel?> GetAsync(string xmlSri);
}

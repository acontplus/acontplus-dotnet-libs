using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

public interface IXmlSriFileService
{
    Task<XmlSriFileModel?> GetAsync(IFormFile file);
    Task<XmlSriFileModel?> GetAsync(string xmlSri);
}

using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Service contract for generating ATS (Anexo Transaccional Simplificado) XML documents.
/// </summary>
public interface IAtsXmlService
{
    /// <summary>
    /// Creates the ATS XML document.
    /// </summary>
    /// <param name="atsData">The data required to generate the ATS XML.</param>
    /// <returns>A byte array representing the generated XML.</returns>
    Task<byte[]> CreateAtsXmlAsync(AtsData atsData);
}
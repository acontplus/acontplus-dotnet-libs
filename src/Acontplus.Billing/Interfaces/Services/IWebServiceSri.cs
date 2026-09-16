using Acontplus.Billing.Models.Responses;

namespace Acontplus.Billing.Interfaces.Services;

/// <summary>
/// Service contract for interacting with Ecuadorian SRI SOAP web services (reception and authorization).
/// </summary>
public interface IWebServiceSri
{
    /// <summary>
    /// Queries the authorization status of an electronic receipt using its access key.
    /// </summary>
    /// <param name="claveAcceso">The 49-digit access key.</param>
    /// <param name="url">The SRI authorization web service URL.</param>
    /// <returns>The SRI authorization response.</returns>
    Task<ResponseSri> AuthorizationAsync(string claveAcceso, string url);

    /// <summary>
    /// Queries batch authorization status using a batch access key.
    /// </summary>
    /// <param name="claveAcceso">The batch access key.</param>
    /// <param name="url">The SRI authorization web service URL.</param>
    /// <returns>The SRI authorization response.</returns>
    Task<ResponseSri> AuthorizationLoteAsync(string claveAcceso, string url);

    /// <summary>
    /// Checks the existence of an authorized receipt in the SRI system.
    /// </summary>
    /// <param name="claveAcceso">The 49-digit access key.</param>
    /// <param name="url">The SRI authorization web service URL.</param>
    /// <returns>The SRI existence response.</returns>
    Task<ResponseSri> CheckExistenceAsync(string claveAcceso, string url);

    /// <summary>
    /// Retrieves the authorized XML string of a receipt from SRI.
    /// </summary>
    /// <param name="claveAcceso">The 49-digit access key.</param>
    /// <param name="url">The SRI authorization web service URL.</param>
    /// <returns>The raw authorized XML string.</returns>
    Task<string> GetXmlAsync(string claveAcceso, string url);

    /// <summary>
    /// Submits a signed electronic document XML to the SRI reception web service.
    /// </summary>
    /// <param name="xmlSigned">The signed XML string or base64 data.</param>
    /// <param name="url">The SRI reception web service URL.</param>
    /// <returns>The SRI reception response.</returns>
    Task<ResponseSri> ReceptionAsync(string xmlSigned, string url);
}
namespace Acontplus.Billing.Interfaces.Services;

/// <summary>Signs XML comprobantes with XAdES-BES format for SRI Ecuador.</summary>
public interface ISriSigner
{
    /// <summary>
    /// Signs an XML comprobante with XAdES-BES (RSA-SHA1) for SRI Ecuador,
    /// replicating the MITyCLibXADES output expected by the SRI web service.
    /// The CPU-bound signing work is offloaded to the thread pool so the calling
    /// thread is never blocked — safe for high-concurrency and background usage.
    /// </summary>
    /// <param name="xmlUnsigned">Unsigned comprobante XML string.</param>
    /// <param name="pfxPassword">PFX/P12 certificate password.</param>
    /// <param name="pfxBytes">Raw bytes of the PFX/P12 certificate file.</param>
    /// <param name="claveAcceso">49-digit SRI access key (clave de acceso).</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>The signed XML string ready for SRI reception.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claveAcceso"/> is not exactly 49 digits,
    /// or when required parameters are null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the certificate does not contain an RSA private key,
    /// or when the XML document has no root element.
    /// </exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when the PFX password is incorrect or the certificate is corrupt.
    /// </exception>
    Task<string> SignAsync(string xmlUnsigned, string pfxPassword, byte[] pfxBytes,
                          string claveAcceso, CancellationToken ct = default);
}

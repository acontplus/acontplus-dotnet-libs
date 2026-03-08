namespace Acontplus.Barcode.Interfaces;

/// <summary>
/// Defines a contract for generating barcode and QR code images.
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// Generates a barcode image as a byte array based on the supplied configuration.
    /// </summary>
    /// <param name="config">The <see cref="BarcodeConfig"/> that controls format, size, colors, and encoding options.</param>
    /// <returns>The raw bytes of the encoded image.</returns>
    /// <exception cref="ArgumentException">Thrown when <see cref="BarcodeConfig.Text"/> is null or empty.</exception>
    byte[] Create(BarcodeConfig config);
}

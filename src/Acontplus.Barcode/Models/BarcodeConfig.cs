namespace Acontplus.Barcode.Models;

/// <summary>
/// Configuration options for barcode generation.
/// </summary>
public class BarcodeConfig
{
    /// <summary>
    /// The text or data to encode into the barcode.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The barcode format to generate. Defaults to <see cref="BarcodeFormat.CODE_128"/>.
    /// </summary>
    public BarcodeFormat Format { get; set; } = BarcodeFormat.CODE_128;

    /// <summary>
    /// Width of the generated barcode image in pixels. Defaults to 300.
    /// </summary>
    public int Width { get; set; } = 300;

    /// <summary>
    /// Height of the generated barcode image in pixels. Defaults to 100.
    /// </summary>
    public int Height { get; set; } = 100;

    /// <summary>
    /// Whether to render a human-readable text label beneath the barcode. Defaults to <see langword="false"/>.
    /// </summary>
    public bool IncludeLabel { get; set; } = false;

    /// <summary>
    /// Quiet zone (margin) in pixels around the barcode. Defaults to 10.
    /// </summary>
    public int Margin { get; set; } = 10;

    /// <summary>
    /// Output image encoding format. Defaults to <see cref="SKEncodedImageFormat.Png"/>.
    /// </summary>
    public SKEncodedImageFormat OutputFormat { get; set; } = SKEncodedImageFormat.Png;

    /// <summary>
    /// Image quality for lossy formats such as JPEG (0–100). Defaults to 100.
    /// </summary>
    public int Quality { get; set; } = 100;

    /// <summary>
    /// Foreground (bar/module) color. Defaults to <see cref="SKColors.Black"/> when <see langword="null"/>.
    /// </summary>
    public SKColor? ForegroundColor { get; set; }

    /// <summary>
    /// Background color. Defaults to <see cref="SKColors.White"/> when <see langword="null"/>.
    /// </summary>
    public SKColor? BackgroundColor { get; set; }

    /// <summary>
    /// Additional format-specific encoding options (e.g. QR error correction level).
    /// Width, Height, Margin, and PureBarcode are always applied from the top-level properties
    /// and will override any values set here.
    /// </summary>
    public EncodingOptions? AdditionalOptions { get; set; }
}

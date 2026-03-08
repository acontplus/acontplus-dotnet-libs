using ZXing.SkiaSharp.Rendering;

namespace Acontplus.Barcode.Utils;

/// <summary>
/// Provides static utility methods for generating barcodes and QR codes.
/// </summary>
public static class BarcodeGen
{
    /// <summary>
    /// Generates a barcode image as a byte array based on the supplied configuration.
    /// </summary>
    /// <param name="config">The <see cref="BarcodeConfig"/> that controls format, size, colors, and encoding options.</param>
    /// <returns>The raw bytes of the encoded image in the format specified by <see cref="BarcodeConfig.OutputFormat"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <see cref="BarcodeConfig.Text"/> is null or empty.</exception>
    public static byte[] Create(BarcodeConfig config)
    {
        if (string.IsNullOrEmpty(config.Text))
            throw new ArgumentException("Text cannot be null or empty", nameof(config));

        // Clone additional options so the caller's instance is never mutated, then apply
        // top-level size/margin settings which always take precedence.
        var options = new EncodingOptions();
        if (config.AdditionalOptions?.Hints is { Count: > 0 } hints)
        {
            foreach (var hint in hints)
                options.Hints[hint.Key] = hint.Value;
        }

        options.Width = config.Width;
        options.Height = config.Height;
        options.Margin = config.Margin;
        options.PureBarcode = !config.IncludeLabel;

        var renderer = new SKBitmapRenderer();
        if (config.ForegroundColor.HasValue)
            renderer.Foreground = config.ForegroundColor.Value;
        if (config.BackgroundColor.HasValue)
            renderer.Background = config.BackgroundColor.Value;

        var writer = new BarcodeWriter<SKBitmap>
        {
            Format = config.Format,
            Options = options,
            Renderer = renderer
        };

        using var barcodeBitmap = writer.Write(config.Text);
        if (barcodeBitmap is null)
            throw new ArgumentException("Failed to generate barcode. The text may be invalid for the specified format.", nameof(config));
        using var image = SKImage.FromBitmap(barcodeBitmap);
        using var data = image.Encode(config.OutputFormat, config.Quality);
        using var memoryStream = new MemoryStream();
        data.SaveTo(memoryStream);

        return memoryStream.ToArray();
    }
}

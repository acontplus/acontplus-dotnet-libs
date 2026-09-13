namespace Acontplus.Infrastructure.Configuration;

/// <summary>
///     Configuration for response compression middleware.
/// </summary>
public class ResponseCompressionConfiguration
{
    /// <summary>
    ///     Enable response compression for HTTPS requests only.
    /// </summary>
    public bool EnableForHttps { get; set; } = true;

    /// <summary>
    ///     Minimum response size in bytes to compress. Responses smaller than this will not be compressed.
    /// </summary>
    public long MinimumResponseSize { get; set; } = 1024;

    /// <summary>
    ///     List of MIME types to compress. If empty, defaults to common compressible types.
    /// </summary>
    public List<string> MimeTypes { get; set; } = [];

    /// <summary>
    ///     Enable Brotli compression provider.
    /// </summary>
    public bool EnableBrotli { get; set; } = true;

    /// <summary>
    ///     Brotli compression level. Options: Fastest, Optimal, NoCompression.
    /// </summary>
    public string BrotliLevel { get; set; } = "Optimal";

    /// <summary>
    ///     Enable Gzip compression provider.
    /// </summary>
    public bool EnableGzip { get; set; } = true;

    /// <summary>
    ///     Gzip compression level. Options: Fastest, Optimal, NoCompression.
    /// </summary>
    public string GzipLevel { get; set; } = "Optimal";
}

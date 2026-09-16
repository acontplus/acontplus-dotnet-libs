namespace Acontplus.Utilities.Services;

/// <summary>
/// Provides factory methods for creating and constructing <see cref="FileModel"/> instances.
/// </summary>
public static class FileModelBuilder
{
    /// <summary>
    /// Creates a <see cref="FileModel"/> from raw byte content.
    /// </summary>
    /// <param name="content">The raw binary content of the file.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="fileName">Optional original file name.</param>
    /// <returns>A populated <see cref="FileModel"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static FileModel Create(byte[] content, string contentType, string? fileName = null)
    {
        return content == null
            ? throw new ArgumentNullException(nameof(content))
            : new FileModel
            {
                Content = content,
                ContentType = contentType ?? "application/octet-stream",
                FileName = fileName
            };
    }

    /// <summary>
    /// Creates a <see cref="FileModel"/> containing both raw bytes and its base64 string representation.
    /// </summary>
    /// <param name="content">The raw binary content of the file.</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="fileName">Optional original file name.</param>
    /// <returns>A populated <see cref="FileModel"/> instance with Base64 encoded data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static FileModel CreateBase64(byte[] content, string contentType, string? fileName = null)
    {
        return content == null
            ? throw new ArgumentNullException(nameof(content))
            : new FileModel
            {
                ContentType = contentType ?? "application/octet-stream",
                FileName = fileName,
                Base64 = Convert.ToBase64String(content)
            };
    }

    /// <summary>
    /// Asynchronously creates a compressed <see cref="FileModel"/> from an uploaded <see cref="IFormFile"/>.
    /// </summary>
    /// <param name="file">The uploaded form file.</param>
    /// <param name="compressor">A delegate that receives the raw file bytes and returns compressed bytes.</param>
    /// <returns>A task representing the asynchronous operation, returning a compressed <see cref="FileModel"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
    public static async Task<FileModel> CreateCompressedAsync(IFormFile file, Func<byte[], byte[]> compressor)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return new FileModel
        {
            FileName = FileExtensions.SanitizeFileName(file.FileName),
            ContentType = file.ContentType,
            Content = compressor(ms.ToArray())
        };
    }
}

namespace Acontplus.Reports.Dtos;

/// <summary>
/// Represents a generated report response containing the file payload, MIME content type, and download filename.
/// </summary>
public class ReportResponse : IDisposable
{
    /// <summary>Gets or sets the rendered binary report file contents.</summary>
    public required byte[] FileContents { get; set; }

    /// <summary>Gets or sets the MIME content type of the rendered report.</summary>
    public required string ContentType { get; set; }

    /// <summary>Gets or sets the file name suggested for download.</summary>
    public required string FileDownloadName { get; set; }

    private bool _disposed = false;

    /// <summary>
    /// Releases the resources used by the <see cref="ReportResponse"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources here
                // Example: clear sensitive data or other cleanup
                FileContents = Array.Empty<byte>();
                ContentType = string.Empty;
                FileDownloadName = string.Empty;
            }

            // Dispose unmanaged resources here

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ReportResponse"/> class.
    /// </summary>
    ~ReportResponse()
    {
        Dispose(false);
    }
}

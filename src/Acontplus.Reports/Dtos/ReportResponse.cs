namespace Acontplus.Reports.Dtos;

public class ReportResponse : IDisposable
{
    public required byte[] FileContents { get; set; }
    public required string ContentType { get; set; }
    public required string FileDownloadName { get; set; }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

    ~ReportResponse()
    {
        Dispose(false);
    }
}

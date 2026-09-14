namespace Acontplus.Reports.Helpers;

/// <summary>
/// Helper methods for loading and working with RDLC report definition files.
/// </summary>
public static class RdlcHelpers
{
    /// <summary>
    /// Loads an RDLC report definition from a file path into a seekable <see cref="MemoryStream"/>.
    /// </summary>
    /// <param name="filePath">The file path to the RDLC report definition.</param>
    /// <returns>A new <see cref="MemoryStream"/> positioned at the beginning.</returns>
    public static MemoryStream LoadReportDefinition(string filePath)
    {
        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fileStream.CopyTo(memoryStream);
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}

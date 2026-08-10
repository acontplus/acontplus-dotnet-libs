namespace Acontplus.Core.Dtos.Common;

/// <summary>Represents a file payload that can be transferred across application boundaries.</summary>
public class FileModel
{
    /// <summary>Original file name including extension (e.g., <c>report.pdf</c>).</summary>
    public string? FileName { get; set; }

    /// <summary>MIME content type (e.g., <c>application/pdf</c>).</summary>
    public string? ContentType { get; set; }

    /// <summary>Raw file content as a byte array.</summary>
    public byte[]? Content { get; set; }

    /// <summary>File content encoded as a Base64 string. Useful for JSON transport.</summary>
    public string? Base64 { get; set; }
}

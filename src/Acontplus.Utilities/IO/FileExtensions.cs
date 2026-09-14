using System.Text.RegularExpressions;

namespace Acontplus.Utilities.IO;

/// <summary>
/// Provides extension methods for file name sanitization and file data conversion.
/// </summary>
public static partial class FileExtensions
{
    [GeneratedRegex(@"[^A-Za-z0-9_. ]+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex InvalidFileNameCharactersRegex();

    /// <summary>
    /// Removes invalid characters from a file name and trims whitespace.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <returns>A sanitized file name containing only valid characters.</returns>
    public static string SanitizeFileName(string fileName)
    {
        var response = InvalidFileNameCharactersRegex().Replace(fileName.Trim(), "");
        return response.Replace(" ", string.Empty);
    }

    /// <summary>
    /// Converts a byte array to a Base64-encoded string.
    /// </summary>
    /// <param name="valueByte">The byte array to convert.</param>
    /// <returns>A Base64-encoded string representation of the byte array.</returns>
    public static string GetBase64FromByte(byte[] valueByte)
    {
        ArgumentNullException.ThrowIfNull(valueByte);

        // Using MemoryStream to avoid unnecessary allocations
        using var memoryStream = new MemoryStream();
        // Write the byte array to the MemoryStream
        memoryStream.Write(valueByte, 0, valueByte.Length);
        memoryStream.Seek(0, SeekOrigin.Begin);

        // Convert to Base64 string
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    /// <summary>
    /// Reads the contents of an <see cref="IFormFile"/> and returns it as a byte array.
    /// </summary>
    /// <param name="file">The uploaded file to read.</param>
    /// <returns>A byte array containing the file's contents.</returns>
    public static byte[] GetBytes(IFormFile file)
    {
        using var ms = new MemoryStream();
        file.CopyTo(ms);
        var fileBytes = ms.ToArray();
        return fileBytes;
    }
}
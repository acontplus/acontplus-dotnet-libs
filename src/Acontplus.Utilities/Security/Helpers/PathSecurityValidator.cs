using System.Security;

namespace Acontplus.Utilities.Security.Helpers;

/// <summary>
/// Provides secure path validation to prevent directory traversal attacks and other path-based security vulnerabilities.
/// This utility should be used whenever user input or external data is used to construct file paths.
/// </summary>
public static class PathSecurityValidator
{
    /// <summary>
    /// Validates that the requested path is within the allowed base directory and doesn't contain path traversal attempts.
    /// </summary>
    /// <param name="baseDirectory">The base directory that contains allowed files</param>
    /// <param name="requestedPath">The requested path to validate (can be relative)</param>
    /// <returns>The fully resolved and validated absolute path</returns>
    /// <exception cref="ArgumentNullException">Thrown when baseDirectory or requestedPath is null</exception>
    /// <exception cref="SecurityException">Thrown when the path contains invalid characters, attempts directory traversal, or resolves outside the base directory</exception>
    /// <remarks>
    /// This method performs comprehensive validation including:
    /// - Null byte detection
    /// - Invalid path character detection
    /// - Relative path component detection (../)
    /// - Absolute path detection (C:\, /etc/)
    /// - UNC path detection (\\server\share)
    /// - Path traversal prevention (ensures resolved path stays within base directory)
    /// </remarks>
    /// <example>
    /// <code>
    /// var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
    /// var userInput = "invoices/2024/invoice.rdlc";
    /// var safePath = PathSecurityValidator.ValidateAndResolvePath(basePath, userInput);
    /// </code>
    /// </example>
    public static string ValidateAndResolvePath(string baseDirectory, string requestedPath)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(requestedPath);

        // Normalize the base directory
        var normalizedBase = Path.GetFullPath(baseDirectory);

        // Remove leading/trailing slashes and normalize separators
        var cleanPath = requestedPath
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Check for null bytes (common in directory traversal attacks)
        if (cleanPath.Contains('\0'))
        {
            throw new SecurityException($"Path contains invalid null characters: {requestedPath}");
        }

        // Check for invalid path characters
        var invalidChars = Path.GetInvalidPathChars();
        if (cleanPath.Any(c => invalidChars.Contains(c)))
        {
            throw new SecurityException($"Path contains invalid characters: {requestedPath}");
        }

        // Split the path and check each component
        var pathComponents = cleanPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        foreach (var component in pathComponents)
        {
            // Check for relative path components
            if (component is "." or "..")
            {
                throw new SecurityException($"Path contains relative directory references (./ or ../): {requestedPath}");
            }

            // Check for drive letters or UNC paths (Windows)
            if (component.Contains(':') || component.StartsWith("\\\\"))
            {
                throw new SecurityException($"Path contains absolute references or UNC paths: {requestedPath}");
            }

            // Check for invalid file name characters
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            if (component.Any(c => invalidFileNameChars.Contains(c)))
            {
                throw new SecurityException($"Path component '{component}' contains invalid file name characters");
            }
        }

        // Combine and resolve the full path
        var fullPath = Path.GetFullPath(Path.Combine(normalizedBase, cleanPath));

        // Ensure the resolved path is within the base directory
        if (!IsPathWithinDirectory(fullPath, normalizedBase))
        {
            throw new SecurityException(
                $"Path traversal detected. Requested path '{requestedPath}' resolves outside the allowed directory");
        }

        return fullPath;
    }

    /// <summary>
    /// Validates that the file extension is in the allowed list.
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <param name="allowedExtensions">Array of allowed extensions (e.g., ".rdlc", ".rdl", ".pdf"). Case-insensitive.</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath or allowedExtensions is null</exception>
    /// <exception cref="SecurityException">Thrown when the extension is not allowed or missing</exception>
    /// <example>
    /// <code>
    /// PathSecurityValidator.ValidateFileExtension("report.rdlc", ".rdlc", ".rdl");
    /// PathSecurityValidator.ValidateFileExtension("document.RDLC", ".rdlc"); // Case-insensitive
    /// </code>
    /// </example>
    public static void ValidateFileExtension(string filePath, params string[] allowedExtensions)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(allowedExtensions);

        if (allowedExtensions.Length == 0)
        {
            return; // No restrictions if no extensions specified
        }

        var extension = Path.GetExtension(filePath);

        if (string.IsNullOrEmpty(extension))
        {
            throw new SecurityException($"File path '{filePath}' has no extension");
        }

        if (!allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            throw new SecurityException(
                $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtensions)}");
        }
    }

    /// <summary>
    /// Validates that a file exists at the specified path and is accessible.
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
    /// <exception cref="SecurityException">Thrown when the file is not accessible</exception>
    public static void ValidateFileExists(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // Try to access the file to ensure it's readable
        try
        {
            using var stream = File.OpenRead(filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new SecurityException($"Access denied to file: {filePath}", ex);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new SecurityException($"Cannot access file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Checks if a path is within a specified directory (prevents path traversal).
    /// </summary>
    /// <param name="path">The path to check (should be absolute)</param>
    /// <param name="directory">The directory that should contain the path (should be absolute)</param>
    /// <returns>True if path is within directory, false otherwise</returns>
    private static bool IsPathWithinDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);

        // Ensure directory path ends with separator for proper comparison
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDirectory += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sanitizes a file name by removing potentially dangerous characters while preserving the extension.
    /// Use this for user-provided file names that will be saved to disk.
    /// </summary>
    /// <param name="fileName">The file name to sanitize</param>
    /// <param name="replacement">Character to replace invalid characters with (default: underscore)</param>
    /// <returns>A sanitized file name safe for use in file systems</returns>
    /// <example>
    /// <code>
    /// var safe = PathSecurityValidator.SanitizeFileName("my../../../report.pdf");
    /// // Result: "my_______report.pdf"
    /// </code>
    /// </example>
    public static string SanitizeFileName(string fileName, char replacement = '_')
    {
        ArgumentNullException.ThrowIfNull(fileName);

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new char[fileName.Length];

        for (int i = 0; i < fileName.Length; i++)
        {
            sanitized[i] = invalidChars.Contains(fileName[i]) ? replacement : fileName[i];
        }

        return new string(sanitized);
    }

    /// <summary>
    /// Validates and combines multiple path segments safely.
    /// Throws if any segment attempts path traversal.
    /// </summary>
    /// <param name="baseDirectory">The base directory</param>
    /// <param name="pathSegments">Path segments to combine</param>
    /// <returns>The validated combined path</returns>
    /// <exception cref="SecurityException">Thrown if path traversal is detected in any segment</exception>
    public static string CombinePathSecurely(string baseDirectory, params string[] pathSegments)
    {
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(pathSegments);

        // Validate each segment individually
        foreach (var segment in pathSegments)
        {
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            if (segment.Contains("..") || segment.Contains('\0'))
            {
                throw new SecurityException($"Path segment contains unsafe characters: {segment}");
            }
        }

        // Combine all segments
        var combined = Path.Combine(new[] { baseDirectory }.Concat(pathSegments).ToArray());

        // Final validation
        return ValidateAndResolvePath(baseDirectory, Path.GetRelativePath(baseDirectory, combined));
    }
}

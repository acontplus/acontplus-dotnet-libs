using System.Security;

namespace Acontplus.Reports.Exceptions;

/// <summary>
/// Exception thrown when report generation fails
/// </summary>
public class ReportGenerationException : Exception
{
    public string? ReportPath { get; }
    public string? ReportFormat { get; }

    public ReportGenerationException(string message) : base(message)
    {
    }

    public ReportGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ReportGenerationException(string message, string reportPath, string reportFormat)
        : base(message)
    {
        ReportPath = reportPath;
        ReportFormat = reportFormat;
    }

    public ReportGenerationException(string message, string reportPath, string reportFormat, Exception innerException)
        : base(message, innerException)
    {
        ReportPath = reportPath;
        ReportFormat = reportFormat;
    }
}

/// <summary>
/// Exception thrown when report size exceeds maximum allowed
/// </summary>
public class ReportSizeExceededException(long reportSize, long maxSize)
    : ReportGenerationException($"Report size ({reportSize} bytes) exceeds maximum allowed size ({maxSize} bytes)")
{
    public long ReportSize { get; } = reportSize;
    public long MaxSize { get; } = maxSize;
}

/// <summary>
/// Exception thrown when report generation times out
/// </summary>
public class ReportTimeoutException : ReportGenerationException
{
    public int TimeoutSeconds { get; }

    public ReportTimeoutException(int timeoutSeconds)
        : base($"Report generation timed out after {timeoutSeconds} seconds")
    {
        TimeoutSeconds = timeoutSeconds;
    }

    public ReportTimeoutException(string reportPath, int timeoutSeconds)
        : base($"Report generation for '{reportPath}' timed out after {timeoutSeconds} seconds")
    {
        TimeoutSeconds = timeoutSeconds;
    }
}

/// <summary>
/// Exception thrown when report file is not found
/// </summary>
public class ReportNotFoundException : ReportGenerationException
{
    public ReportNotFoundException(string reportPath)
        : base($"Report file not found at path: {reportPath}")
    {
    }

    public ReportNotFoundException(string reportPath, Exception innerException)
        : base($"Report file not found at path: {reportPath}", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an invalid or potentially malicious report path is detected
/// </summary>
public class InvalidReportPathException : ReportGenerationException
{
    public InvalidReportPathException(string message)
        : base(message)
    {
    }

    public InvalidReportPathException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an InvalidReportPathException from a SecurityException thrown by PathSecurityValidator
    /// </summary>
    public static InvalidReportPathException FromSecurityException(SecurityException securityException, string? reportPath = null)
    {
        var message = reportPath != null
            ? $"Invalid report path '{reportPath}': {securityException.Message}"
            : securityException.Message;

        return new InvalidReportPathException(message, securityException);
    }
}

using System.Security;

namespace Acontplus.Reports.Exceptions;

/// <summary>
/// Exception thrown when report generation fails.
/// </summary>
public class ReportGenerationException : Exception
{
    /// <summary>Gets the report definition path, if specified.</summary>
    public string? ReportPath { get; }

    /// <summary>Gets the requested rendering format, if specified.</summary>
    public string? ReportFormat { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ReportGenerationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ReportGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerationException"/> class with report path and format context.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="reportPath">The report path being rendered.</param>
    /// <param name="reportFormat">The rendering format requested.</param>
    public ReportGenerationException(string message, string reportPath, string reportFormat)
        : base(message)
    {
        ReportPath = reportPath;
        ReportFormat = reportFormat;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportGenerationException"/> class with context and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="reportPath">The report path being rendered.</param>
    /// <param name="reportFormat">The rendering format requested.</param>
    /// <param name="innerException">The inner exception that caused the failure.</param>
    public ReportGenerationException(string message, string reportPath, string reportFormat, Exception innerException)
        : base(message, innerException)
    {
        ReportPath = reportPath;
        ReportFormat = reportFormat;
    }
}

/// <summary>
/// Exception thrown when report size exceeds maximum allowed size.
/// </summary>
public class ReportSizeExceededException(long reportSize, long maxSize)
    : ReportGenerationException($"Report size ({reportSize} bytes) exceeds maximum allowed size ({maxSize} bytes)")
{
    /// <summary>Gets the size in bytes of the generated report.</summary>
    public long ReportSize { get; } = reportSize;

    /// <summary>Gets the maximum allowed size in bytes.</summary>
    public long MaxSize { get; } = maxSize;
}

/// <summary>
/// Exception thrown when report generation times out.
/// </summary>
public class ReportTimeoutException : ReportGenerationException
{
    /// <summary>Gets the timeout threshold in seconds.</summary>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportTimeoutException"/> class with a specified timeout.
    /// </summary>
    /// <param name="timeoutSeconds">The elapsed seconds when timeout occurred.</param>
    public ReportTimeoutException(int timeoutSeconds)
        : base($"Report generation timed out after {timeoutSeconds} seconds")
    {
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportTimeoutException"/> class with report path and timeout.
    /// </summary>
    /// <param name="reportPath">The report path that timed out.</param>
    /// <param name="timeoutSeconds">The elapsed seconds when timeout occurred.</param>
    public ReportTimeoutException(string reportPath, int timeoutSeconds)
        : base($"Report generation for '{reportPath}' timed out after {timeoutSeconds} seconds")
    {
        TimeoutSeconds = timeoutSeconds;
    }
}

/// <summary>
/// Exception thrown when report file is not found.
/// </summary>
public class ReportNotFoundException : ReportGenerationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportNotFoundException"/> class.
    /// </summary>
    /// <param name="reportPath">The missing report file path.</param>
    public ReportNotFoundException(string reportPath)
        : base($"Report file not found at path: {reportPath}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="reportPath">The missing report file path.</param>
    /// <param name="innerException">The exception that is the cause of the failure.</param>
    public ReportNotFoundException(string reportPath, Exception innerException)
        : base($"Report file not found at path: {reportPath}", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when an invalid or potentially malicious report path is detected.
/// </summary>
public class InvalidReportPathException : ReportGenerationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidReportPathException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidReportPathException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidReportPathException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidReportPathException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an InvalidReportPathException from a SecurityException thrown by PathSecurityValidator.
    /// </summary>
    /// <param name="securityException">The underlying security exception.</param>
    /// <param name="reportPath">The optional invalid report path.</param>
    /// <returns>A new <see cref="InvalidReportPathException"/> instance.</returns>
    public static InvalidReportPathException FromSecurityException(SecurityException securityException, string? reportPath = null)
    {
        var message = reportPath != null
            ? $"Invalid report path '{reportPath}': {securityException.Message}"
            : securityException.Message;

        return new InvalidReportPathException(message, securityException);
    }
}

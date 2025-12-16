namespace Acontplus.S3Application.Configuration;

/// <summary>
/// Configuration options for S3 storage service including performance and resilience settings.
/// </summary>
public class S3StorageOptions
{
    /// <summary>
    /// Configuration section name for S3 storage options.
    /// </summary>
    public const string SectionName = "AWS:S3";

    /// <summary>
    /// Gets or sets the maximum number of requests per second to prevent throttling.
    /// Default is 100 requests/second.
    /// AWS S3 limits: 3,500 PUT/COPY/POST/DELETE and 5,500 GET/HEAD per prefix per second.
    /// </summary>
    public int MaxRequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// Default is 60 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// Default is 3 retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds for exponential backoff between retries.
    /// Default is 500ms.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the AWS region endpoint (e.g., "us-east-1", "eu-west-1").
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the default bucket name for operations.
    /// </summary>
    public string? DefaultBucketName { get; set; }

    /// <summary>
    /// Gets or sets whether to use path-style addressing for S3 buckets.
    /// Default is false (uses virtual-hosted-style).
    /// </summary>
    public bool ForcePathStyle { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable request metrics and logging.
    /// Default is false.
    /// </summary>
    public bool EnableMetrics { get; set; } = false;
}

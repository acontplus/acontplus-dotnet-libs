namespace Acontplus.Persistence.Common.Configuration;

/// <summary>
/// Resilience configuration options for persistence layer (ADO.NET repositories).
/// Provides retry policy, circuit breaker, and timeout settings for database operations.
/// </summary>
public class PersistenceResilienceOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Persistence:Resilience";

    /// <summary>
    /// Retry policy configuration for database operations.
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration for database operations.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Timeout configuration for database operations.
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();

    /// <summary>
    /// Retry policy configuration options for ADO.NET operations.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Enable retry policy. Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of retries. Default: 3.
        /// Aligns with connection string ConnectRetryCount.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Base delay between retries in seconds. Default: 2.
        /// Used as the base for exponential backoff calculation.
        /// </summary>
        public int BaseDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Enable exponential backoff. Default: true.
        /// When enabled, delay increases exponentially: 2s, 4s, 8s, etc.
        /// </summary>
        public bool ExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Maximum delay between retries in seconds. Default: 30.
        /// Caps the exponential backoff to prevent excessive wait times.
        /// </summary>
        public int MaxDelaySeconds { get; set; } = 30;
    }

    /// <summary>
    /// Circuit breaker configuration options for database resilience.
    /// </summary>
    public class CircuitBreakerOptions
    {
        /// <summary>
        /// Enable circuit breaker. Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Number of exceptions before opening circuit. Default: 5.
        /// After this many consecutive failures, circuit opens.
        /// </summary>
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

        /// <summary>
        /// Duration of break in seconds. Default: 30.
        /// How long circuit stays open before allowing test requests.
        /// </summary>
        public int DurationOfBreakSeconds { get; set; } = 30;

        /// <summary>
        /// Sampling duration in seconds. Default: 60.
        /// Time window for tracking failure rate.
        /// </summary>
        public int SamplingDurationSeconds { get; set; } = 60;

        /// <summary>
        /// Minimum throughput before circuit can open. Default: 10.
        /// Prevents circuit from opening due to low traffic spikes.
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;
    }

    /// <summary>
    /// Timeout configuration options for database operations.
    /// </summary>
    public class TimeoutOptions
    {
        /// <summary>
        /// Enable timeout policies. Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default command timeout in seconds. Default: 30.
        /// Used when CommandOptionsDto.CommandTimeout is not specified.
        /// </summary>
        public int DefaultCommandTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Timeout for complex queries in seconds. Default: 60.
        /// Used for queries involving multiple tables or large datasets.
        /// </summary>
        public int ComplexQueryTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Timeout for bulk operations in seconds. Default: 300 (5 minutes).
        /// Used for SqlBulkCopy and PostgreSQL COPY operations.
        /// </summary>
        public int BulkOperationTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Timeout for long-running reports in seconds. Default: 600 (10 minutes).
        /// Used for complex analytics and reporting queries.
        /// </summary>
        public int LongRunningQueryTimeoutSeconds { get; set; } = 600;
    }
}

namespace Acontplus.Logging;

/// <summary>
/// Configuration options for logging functionality.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether local file logging is enabled.
    /// </summary>
    public bool EnableLocalFile { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether buffered writing is enabled for file logging.
    /// </summary>
    public bool Buffered { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether shared file access is enabled. Can be set to true when Buffered is false.
    /// </summary>
    public bool Shared { get; set; }  //If buffered is false, can set shared to true

    /// <summary>
    /// Gets or sets the local file path for log files.
    /// </summary>
    public string LocalFilePath { get; set; } = "logs/log-.log";

    /// <summary>
    /// Gets or sets the rolling interval for log files (e.g., Day, Hour).
    /// </summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>
    /// Gets or sets the maximum number of retained log files. Null indicates no limit.
    /// </summary>
    public int? RetainedFileCountLimit { get; set; } = 7;

    /// <summary>
    /// Gets or sets the maximum file size in bytes before rolling. Null indicates no limit.
    /// </summary>
    public long? FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the log formatter type (Plain or Json).
    /// </summary>
    public string Formatter { get; set; } = "Plain"; // Plain or Json

    /// <summary>
    /// Gets or sets the output template for log messages.
    /// </summary>
    public string? OutputTemplate { get; set; } = "{CustomTimestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Gets or sets a value indicating whether database logging is enabled.
    /// </summary>
    public bool EnableDatabaseLogging { get; set; }

    /// <summary>
    /// Gets or sets the database connection string for logging.
    /// </summary>
    public string? DatabaseConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Elasticsearch logging is enabled.
    /// </summary>
    public bool EnableElasticsearchLogging { get; set; }

    /// <summary>
    /// Gets or sets the Elasticsearch URL.
    /// </summary>
    public string? ElasticsearchUrl { get; set; }

    /// <summary>
    /// Gets or sets the index format for Elasticsearch logs.
    /// </summary>
    public string? ElasticsearchIndexFormat { get; set; } = "logs-{0:yyyy.MM.dd}";

    /// <summary>
    /// Gets or sets the username for Elasticsearch authentication.
    /// </summary>
    public string? ElasticsearchUsername { get; set; }

    /// <summary>
    /// Gets or sets the password for Elasticsearch authentication.
    /// </summary>
    public string? ElasticsearchPassword { get; set; }

    /// <summary>
    /// Gets or sets the time zone ID for logging timestamps. Default is UTC.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC"; // Default to UTC
}

/// <summary>
/// Configuration options for OpenTelemetry observability (tracing, metrics, and logging).
/// OTLP export is configured once at root via <see cref="EnableOtlpExporter"/>, <see cref="OtlpEndpoint"/>,
/// and <see cref="OtlpProtocol"/>. When no Dynatrace exporter is active, a single
/// <c>UseOtlpExporter</c> call covers all three signals. Sub-sections (<see cref="Tracing"/>,
/// <see cref="Metrics"/>) control instrumentation and per-backend exporters only.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the service name for telemetry identification.
    /// </summary>
    public string ServiceName { get; set; } = "MyService";

    /// <summary>
    /// Gets or sets the service version for telemetry identification.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the service namespace for logical grouping.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint URL for all signals (traces, metrics, and logs).
    /// Examples: <c>http://localhost:4317</c> (gRPC) or <c>http://localhost:4318</c> (HTTP/protobuf).
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the OTLP transport protocol for all signals.
    /// Accepted values: <c>grpc</c> (default) or <c>http</c>.
    /// </summary>
    public string OtlpProtocol { get; set; } = "grpc";

    /// <summary>
    /// Gets or sets a value indicating whether to enable OTLP export for all signals (traces, metrics, and logs).
    /// When Dynatrace is not configured, <c>UseOtlpExporter</c> is used — one call covering all signals.
    /// When Dynatrace is configured, per-signal AddOtlpExporter is used instead (they cannot coexist).
    /// </summary>
    public bool EnableOtlpExporter { get; set; }

    /// <summary>
    /// Gets or sets tracing configuration.
    /// </summary>
    public TracingOptions Tracing { get; set; } = new();

    /// <summary>
    /// Gets or sets metrics configuration.
    /// </summary>
    public MetricsOptions Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets logging configuration for OpenTelemetry.
    /// </summary>
    public OTelLoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Configuration options for distributed tracing.
/// </summary>
public class TracingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether tracing is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ASP.NET Core instrumentation is enabled.
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether HTTP client instrumentation is enabled.
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether SQL Client instrumentation is enabled.
    /// </summary>
    public bool EnableSqlClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether console exporter is enabled (for development).
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Dynatrace exporter is enabled.
    /// </summary>
    public bool EnableDynatraceExporter { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace OTLP endpoint URL (e.g., https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/traces).
    /// </summary>
    public string? DynatraceEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace API token for authentication.
    /// </summary>
    public string? DynatraceApiToken { get; set; }

    /// <summary>
    /// Gets or sets additional activity sources to include in tracing.
    /// </summary>
    public List<string> AdditionalSources { get; set; } = new();
}

/// <summary>
/// Configuration options for metrics collection.
/// </summary>
public class MetricsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether metrics are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ASP.NET Core instrumentation is enabled.
    /// </summary>
    public bool EnableAspNetCoreInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether HTTP client instrumentation is enabled.
    /// </summary>
    public bool EnableHttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether runtime instrumentation is enabled.
    /// </summary>
    public bool EnableRuntimeInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether console exporter is enabled (for development).
    /// </summary>
    public bool EnableConsoleExporter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Dynatrace exporter is enabled.
    /// </summary>
    public bool EnableDynatraceExporter { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace OTLP endpoint URL (e.g., https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/metrics).
    /// </summary>
    public string? DynatraceEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace API token for authentication.
    /// </summary>
    public string? DynatraceApiToken { get; set; }

    /// <summary>
    /// Gets or sets additional meters to include in metrics collection.
    /// </summary>
    public List<string> AdditionalMeters { get; set; } = new();
}

/// <summary>
/// Configuration options for OpenTelemetry logging integration.
/// OTLP logging export is handled globally via <see cref="OpenTelemetryOptions.EnableOtlpExporter"/>.
/// Use this class only for Dynatrace-specific logging configuration.
/// </summary>
public class OTelLoggingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Dynatrace exporter is enabled for logs.
    /// </summary>
    public bool EnableDynatraceExporter { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace OTLP endpoint URL (e.g., https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/logs).
    /// </summary>
    public string? DynatraceEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the Dynatrace API token for authentication.
    /// </summary>
    public string? DynatraceApiToken { get; set; }
}

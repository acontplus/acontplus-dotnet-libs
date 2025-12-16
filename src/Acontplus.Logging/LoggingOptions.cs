namespace Acontplus.Logging;

public class LoggingOptions
{
    public bool EnableLocalFile { get; set; } = true;
    public bool Buffered { get; set; } = true;
    public bool Shared { get; set; }  //If buffered is false, can set shared to true
    public string LocalFilePath { get; set; } = "logs/log-.log";
    public string RollingInterval { get; set; } = "Day";
    public int? RetainedFileCountLimit { get; set; } = 7;
    public long? FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024;
    public string Formatter { get; set; } = "Plain"; // Plain or Json
    public string? OutputTemplate { get; set; } = "{CustomTimestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    public bool EnableDatabaseLogging { get; set; }
    public string? DatabaseConnectionString { get; set; }
    public bool EnableElasticsearchLogging { get; set; }
    public string? ElasticsearchUrl { get; set; }
    public string? ElasticsearchIndexFormat { get; set; } = "logs-{0:yyyy.MM.dd}";
    public string? ElasticsearchUsername { get; set; }
    public string? ElasticsearchPassword { get; set; }
    public string TimeZoneId { get; set; } = "UTC"; // Default to UTC
}

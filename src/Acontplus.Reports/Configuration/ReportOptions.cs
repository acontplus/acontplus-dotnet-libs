namespace Acontplus.Reports.Configuration;

/// <summary>
/// Configuration options for report generation services
/// </summary>
public class ReportOptions
{
    /// <summary>
    /// Main directory where RDLC report files are stored
    /// </summary>
    public string MainDirectory { get; set; } = "Reports";

    /// <summary>
    /// External directory for offline or external reports
    /// </summary>
    public string? ExternalDirectory { get; set; }

    /// <summary>
    /// Maximum size in bytes for a single report output (default 100MB)
    /// </summary>
    public long MaxReportSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Timeout for report generation in seconds (default 300 seconds / 5 minutes)
    /// </summary>
    public int ReportGenerationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enable caching of report definitions (default true)
    /// </summary>
    public bool EnableReportDefinitionCache { get; set; } = true;

    /// <summary>
    /// Maximum number of cached report definitions (default 100)
    /// </summary>
    public int MaxCachedReportDefinitions { get; set; } = 100;

    /// <summary>
    /// Time-to-live for cached report definitions in minutes (default 60 minutes)
    /// </summary>
    public int CacheTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Enable memory pooling for better performance (default true)
    /// </summary>
    public bool EnableMemoryPooling { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent report generation tasks. Default: 10
    /// </summary>
    public int MaxConcurrentReports { get; set; } = 10;

    /// <summary>
    /// Maximum number of concurrent print jobs. Default: 5 (printing is slower than generation)
    /// </summary>
    public int MaxConcurrentPrintJobs { get; set; } = 5;

    /// <summary>
    /// Timeout for print jobs in seconds. Default: 180 (3 minutes)
    /// </summary>
    public int PrintJobTimeoutSeconds { get; set; } = 180;

    /// <summary>
    /// Enable detailed logging for report operations (default false)
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Allowed file extensions for report files (default: .rdlc, .rdl)
    /// </summary>
    public string[] AllowedReportExtensions { get; set; } = [".rdlc", ".rdl"];

    /// <summary>
    /// Enable strict path validation to prevent directory traversal attacks (default true)
    /// </summary>
    public bool EnableStrictPathValidation { get; set; } = true;

    // ── QuestPDF ─────────────────────────────────────────────────────────────

    /// <summary>
    /// QuestPDF license tier: Community (free, OSS), Professional, or Enterprise.
    /// Default is Community which is free for projects with under $1M USD annual revenue.
    /// </summary>
    public Dtos.QuestPdfLicenseType QuestPdfLicenseType { get; set; } = Dtos.QuestPdfLicenseType.Community;

    /// <summary>
    /// Maximum number of concurrent QuestPDF generation tasks (default: <see cref="MaxConcurrentReports"/>).
    /// When null, <see cref="MaxConcurrentReports"/> is applied.
    /// </summary>
    public int? MaxConcurrentQuestPdfReports { get; set; }
}

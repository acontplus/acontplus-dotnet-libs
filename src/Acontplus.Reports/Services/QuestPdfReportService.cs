using Acontplus.Reports.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Diagnostics;

namespace Acontplus.Reports.Services;

/// <summary>
/// High-performance QuestPDF dynamic PDF generation service with concurrency control,
/// timeout protection, and full enterprise reporting patterns.
/// </summary>
public sealed class QuestPdfReportService : IQuestPdfReportService, IDisposable
{
    private readonly ILogger<QuestPdfReportService> _logger;
    private readonly ReportOptions _options;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="QuestPdfReportService"/>.
    /// Applies the QuestPDF license at construction time.
    /// </summary>
    public QuestPdfReportService(
        ILogger<QuestPdfReportService> logger,
        IOptions<ReportOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ReportOptions();
        _concurrencyLimiter = new SemaphoreSlim(
            _options.MaxConcurrentReports,
            _options.MaxConcurrentReports);

        ApplyLicense(_options.QuestPdfLicenseType);
    }

    // ── IQuestPdfReportService ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ReportResponse> GenerateAsync(
        QuestPdfReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sw = Stopwatch.StartNew();

        try
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new ReportGenerationException(
                "QuestPDF report generation was cancelled while waiting for the concurrency slot.",
                request.Title, "PDF");
        }

        try
        {
            if (_options.EnableDetailedLogging)
                _logger.LogInformation(
                    "QuestPDF generation started. Title: {Title}, Sections: {Count}",
                    request.Title, request.Sections.Count);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ReportGenerationTimeoutSeconds));

            byte[] pdfBytes;

            try
            {
                // QuestPDF generation is CPU-bound; run on a thread-pool thread
                // so the async caller is not blocked
                pdfBytes = await Task.Run(() =>
                {
                    cts.Token.ThrowIfCancellationRequested();
                    ApplyLicense(request.Settings.LicenseType);
                    var document = new DynamicReportDocument(request);
                    return document.GeneratePdf();
                }, cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ReportTimeoutException(_options.ReportGenerationTimeoutSeconds);
            }

            if (pdfBytes.Length > _options.MaxReportSizeBytes)
                throw new ReportSizeExceededException(pdfBytes.Length, _options.MaxReportSizeBytes);

            sw.Stop();

            if (_options.EnableDetailedLogging)
                _logger.LogInformation(
                    "QuestPDF generation completed. Title: {Title}, Size: {Bytes}B, Elapsed: {Ms}ms",
                    request.Title, pdfBytes.Length, sw.ElapsedMilliseconds);

            return new ReportResponse
            {
                FileContents = pdfBytes,
                ContentType = "application/pdf",
                FileDownloadName = BuildFileName(request)
            };
        }
        catch (ReportGenerationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "QuestPDF generation failed. Title: {Title}, Elapsed: {Ms}ms",
                request.Title, sw.ElapsedMilliseconds);

            throw new ReportGenerationException(
                $"Failed to generate QuestPDF document '{request.Title}': {ex.Message}",
                request.Title, "PDF", ex);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ReportResponse> GenerateFromDataTableAsync(
        string title,
        DataTable data,
        IEnumerable<QuestPdfTableColumn>? columns = null,
        QuestPdfDocumentSettings? settings = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(data);

        var request = new QuestPdfReportRequest
        {
            Title = title,
            FileDownloadName = title,
            Settings = settings ?? new QuestPdfDocumentSettings(),
            Sections =
            [
                new QuestPdfSection
                {
                    Type = QuestPdfSectionType.DataTable,
                    Data = data,
                    Columns = columns?.ToList() ?? [],
                    ShowTotalsRow = columns?.Any(c => c.AggregateType != QuestPdfAggregateType.None) ?? false
                }
            ]
        };

        return await GenerateAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ValidateConfiguration()
    {
        try
        {
            // Generate a minimal one-page document to verify QuestPDF setup
            var probe = new QuestPdfReportRequest
            {
                Title = "Acontplus.Reports — QuestPDF probe",
                Sections =
                [
                    new QuestPdfSection
                    {
                        Type = QuestPdfSectionType.Text,
                        TextBlocks = [new QuestPdfTextBlock { Content = "Configuration OK." }]
                    }
                ]
            };

            var doc = new DynamicReportDocument(probe);
            var bytes = doc.GeneratePdf();

            if (bytes.Length == 0)
                throw new ReportGenerationException("QuestPDF produced an empty document during probe.");

            _logger.LogInformation("QuestPDF configuration validated successfully. Probe size: {Bytes}B", bytes.Length);
        }
        catch (ReportGenerationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReportGenerationException(
                $"QuestPDF configuration validation failed: {ex.Message}",
                "probe", "PDF", ex);
        }
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _concurrencyLimiter.Dispose();
        _disposed = true;
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private static void ApplyLicense(QuestPdfLicenseType licenseType)
    {
        QuestPDF.Settings.License = licenseType switch
        {
            QuestPdfLicenseType.Professional => LicenseType.Professional,
            QuestPdfLicenseType.Enterprise => LicenseType.Enterprise,
            _ => LicenseType.Community
        };
    }

    private static string BuildFileName(QuestPdfReportRequest request)
    {
        var name = request.FileDownloadName ?? request.Title;
        // Sanitize illegal file name characters
        var sanitized = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
        return $"{sanitized}.pdf";
    }
}

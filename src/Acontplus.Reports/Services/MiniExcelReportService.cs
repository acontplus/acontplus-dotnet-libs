using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniExcelLibs;

namespace Acontplus.Reports.Services;

/// <summary>
/// High-performance, stream-based Excel generation service using MiniExcel.
/// Data is streamed directly to the output buffer — no in-memory DOM — making it ideal
/// for large dataset exports.
/// </summary>
public sealed class MiniExcelReportService : IMiniExcelReportService, IDisposable
{
    private readonly ILogger<MiniExcelReportService> _logger;
    private readonly ReportOptions _options;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private bool _disposed;

    /// <summary>
    /// Initialises a new <see cref="MiniExcelReportService"/>.
    /// </summary>
    public MiniExcelReportService(
        ILogger<MiniExcelReportService> logger,
        IOptions<ReportOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ReportOptions();
        _concurrencyLimiter = new SemaphoreSlim(
            _options.MaxConcurrentReports,
            _options.MaxConcurrentReports);
    }

    // ── IMiniExcelReportService ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ReportResponse> GenerateAsync(
        ExcelReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Worksheets.Count == 0)
            throw new ReportGenerationException(
                "At least one worksheet must be provided.",
                request.FileDownloadName, "XLSX");

        await AcquireSlotAsync(cancellationToken, request.FileDownloadName);

        var sw = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ReportGenerationTimeoutSeconds));

            using var stream = new MemoryStream();

            try
            {
                if (request.Worksheets.Count == 1)
                {
                    var ws = request.Worksheets[0];
                    var rows = MapWorksheetToRows(ws);

                    await MiniExcel.SaveAsAsync(
                        stream, rows,
                        printHeader: ws.IncludeHeader,
                        sheetName: ws.Name,
                        cancellationToken: cts.Token).ConfigureAwait(false);
                }
                else
                {
                    // Multi-sheet workbook via Dictionary<sheetName, data>
                    var sheets = request.Worksheets.ToDictionary(
                        ws => ws.Name,
                        ws => (object)MapWorksheetToRows(ws));

                    await MiniExcel.SaveAsAsync(stream, sheets, cancellationToken: cts.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ReportGenerationException(
                    $"MiniExcel report generation timed out after {_options.ReportGenerationTimeoutSeconds} seconds.",
                    request.FileDownloadName, "XLSX");
            }

            stream.Position = 0;
            var bytes = stream.ToArray();

            if (_options.EnableDetailedLogging)
                _logger.LogInformation(
                    "MiniExcel workbook generated. Sheets: {Sheets}, Size: {Size:N0} bytes, Elapsed: {Ms}ms",
                    request.Worksheets.Count, bytes.Length, sw.ElapsedMilliseconds);

            return new ReportResponse
            {
                FileContents = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileDownloadName = EnsureXlsxExtension(request.FileDownloadName)
            };
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    /// <inheritdoc />
    public Task<ReportResponse> GenerateFromDataTableAsync(
        string fileDownloadName,
        DataTable data,
        IEnumerable<ExcelColumnDefinition>? columns = null,
        string worksheetName = "Sheet1",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var request = new ExcelReportRequest
        {
            FileDownloadName = fileDownloadName,
            Worksheets =
            [
                new ExcelWorksheetDefinition
                {
                    Name = worksheetName,
                    Data = data,
                    Columns = columns?.ToList()
                }
            ]
        };

        return GenerateAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReportResponse> GenerateFromObjectsAsync<T>(
        string fileDownloadName,
        IEnumerable<T> data,
        string worksheetName = "Sheet1",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        await AcquireSlotAsync(cancellationToken, fileDownloadName);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ReportGenerationTimeoutSeconds));

            using var stream = new MemoryStream();

            await MiniExcel.SaveAsAsync(
                stream, data,
                sheetName: worksheetName,
                cancellationToken: cts.Token).ConfigureAwait(false);

            stream.Position = 0;

            return new ReportResponse
            {
                FileContents = stream.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileDownloadName = EnsureXlsxExtension(fileDownloadName)
            };
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Projects a <see cref="ExcelWorksheetDefinition"/> to a row-per-record sequence of
    /// <see cref="IDictionary{String,Object}"/>. Hidden columns are excluded and header labels
    /// are applied from <see cref="ExcelColumnDefinition.Header"/>.
    /// </summary>
    private static IEnumerable<IDictionary<string, object?>> MapWorksheetToRows(ExcelWorksheetDefinition ws)
    {
        var colMap = ws.Columns?
            .ToDictionary(c => c.ColumnName, c => c, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ExcelColumnDefinition>(StringComparer.OrdinalIgnoreCase);

        // Determine visible columns in DataTable order
        var visible = ws.Data.Columns
            .Cast<DataColumn>()
            .Where(c => !colMap.TryGetValue(c.ColumnName, out var def) || !def.IsHidden)
            .ToList();

        foreach (DataRow row in ws.Data.Rows)
        {
            var dict = new Dictionary<string, object?>(visible.Count, StringComparer.Ordinal);

            foreach (var col in visible)
            {
                var header = colMap.TryGetValue(col.ColumnName, out var definition)
                             && !string.IsNullOrWhiteSpace(definition.Header)
                    ? definition.Header
                    : col.ColumnName;

                var rawValue = row.IsNull(col) ? null : row[col];

                // Apply format hint if provided (project value to formatted string)
                if (rawValue is not null
                    && definition is not null
                    && !string.IsNullOrEmpty(definition.Format))
                {
                    rawValue = rawValue switch
                    {
                        IFormattable f => f.ToString(definition.Format, System.Globalization.CultureInfo.CurrentCulture),
                        _ => rawValue
                    };
                }

                dict[header] = rawValue;
            }

            yield return dict;
        }
    }

    private async Task AcquireSlotAsync(CancellationToken cancellationToken, string reportName)
    {
        try
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new ReportGenerationException(
                "Excel report generation was cancelled while waiting for a concurrency slot.",
                reportName, "XLSX");
        }
    }

    private static string EnsureXlsxExtension(string name) =>
        name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? name : name + ".xlsx";

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _concurrencyLimiter.Dispose();
        _disposed = true;
    }
}

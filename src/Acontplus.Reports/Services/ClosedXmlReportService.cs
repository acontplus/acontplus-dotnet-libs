using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acontplus.Reports.Services;

/// <summary>
/// Advanced, richly formatted Excel generation service using ClosedXML.
/// Supports corporate header styling, freeze panes, AutoFilter, alternating row shading,
/// aggregate formula totals rows, multi-sheet workbooks, and workbook metadata.
/// </summary>
public sealed class ClosedXmlReportService : IClosedXmlReportService, IDisposable
{
    private readonly ILogger<ClosedXmlReportService> _logger;
    private readonly ReportOptions _options;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private bool _disposed;

    /// <summary>
    /// Initialises a new <see cref="ClosedXmlReportService"/>.
    /// </summary>
    public ClosedXmlReportService(
        ILogger<ClosedXmlReportService> logger,
        IOptions<ReportOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ReportOptions();
        _concurrencyLimiter = new SemaphoreSlim(
            _options.MaxConcurrentReports,
            _options.MaxConcurrentReports);
    }

    // ── IClosedXmlReportService ───────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ReportResponse> GenerateAsync(
        AdvancedExcelReportRequest request,
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

            // ClosedXML is synchronous / CPU-bound — offload to thread pool
            var bytes = await Task.Run(() =>
            {
                cts.Token.ThrowIfCancellationRequested();

                using var workbook = new XLWorkbook();

                // Workbook-level metadata
                workbook.Properties.Author = request.Author ?? string.Empty;
                workbook.Properties.Subject = request.Subject ?? string.Empty;
                workbook.Properties.Company = request.Company ?? string.Empty;
                workbook.Properties.Keywords = request.Keywords ?? string.Empty;

                foreach (var wsDef in request.Worksheets)
                {
                    cts.Token.ThrowIfCancellationRequested();
                    var ws = workbook.Worksheets.Add(wsDef.Name);
                    RenderWorksheet(ws, wsDef);
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }, cts.Token).ConfigureAwait(false);

            if (_options.EnableDetailedLogging)
                _logger.LogInformation(
                    "ClosedXML workbook generated. Sheets: {Sheets}, Size: {Size:N0} bytes, Elapsed: {Ms}ms",
                    request.Worksheets.Count, bytes.Length, sw.ElapsedMilliseconds);

            return new ReportResponse
            {
                FileContents = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileDownloadName = EnsureXlsxExtension(request.FileDownloadName)
            };
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReportGenerationException(
                $"ClosedXML report generation timed out after {_options.ReportGenerationTimeoutSeconds} seconds.",
                request.FileDownloadName, "XLSX", ex);
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
        IEnumerable<AdvancedExcelColumnDefinition>? columns = null,
        string worksheetName = "Sheet1",
        bool autoFilter = true,
        bool freezeHeaderRow = true,
        AdvancedExcelHeaderStyle? headerStyle = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var request = new AdvancedExcelReportRequest
        {
            FileDownloadName = fileDownloadName,
            Worksheets =
            [
                new AdvancedExcelWorksheetDefinition
                {
                    Name = worksheetName,
                    Data = data,
                    Columns = columns?.ToList(),
                    AutoFilter = autoFilter,
                    FreezeHeaderRow = freezeHeaderRow,
                    HeaderStyle = headerStyle
                }
            ]
        };

        return GenerateAsync(request, cancellationToken);
    }

    // ── Worksheet Rendering ───────────────────────────────────────────────────

    private static void RenderWorksheet(IXLWorksheet ws, AdvancedExcelWorksheetDefinition wsDef)
    {
        var headerStyle = wsDef.HeaderStyle ?? AdvancedExcelHeaderStyle.CorporateBlue();

        // Build ordered visible-column list from the DataTable columns
        var colDefs = BuildEffectiveColumns(wsDef.Data, wsDef.Columns);
        var visible = colDefs.Where(c => !c.IsHidden).ToList();

        if (visible.Count == 0)
            return;

        int currentRow = 1;

        // ── Optional title / subtitle rows ────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(wsDef.ReportTitle))
        {
            WriteTitleRow(ws, wsDef.ReportTitle, visible.Count, currentRow,
                wsDef.TitleStyle ?? AdvancedExcelHeaderStyle.Title());
            currentRow++;
        }

        if (!string.IsNullOrWhiteSpace(wsDef.ReportSubTitle))
        {
            WriteSubTitleRow(ws, wsDef.ReportSubTitle, visible.Count, currentRow,
                wsDef.TitleStyle ?? AdvancedExcelHeaderStyle.Title());
            currentRow++;
        }

        // ── Optional group-header band rows ───────────────────────────────────
        if (wsDef.GroupHeaders is { Count: > 0 })
        {
            WriteGroupHeaderRow(ws, wsDef.GroupHeaders, visible.Count, currentRow,
                wsDef.GroupHeaderStyle ?? AdvancedExcelHeaderStyle.GroupHeader());
            currentRow++;
        }

        // ── Column header row ─────────────────────────────────────────────────
        int headerRow = currentRow;
        WriteHeaderRow(ws, visible, headerStyle, headerRow);
        currentRow++;

        // ── Data rows ────────────────────────────────────────────────────────
        int nextRow = WriteDataRows(ws, wsDef.Data, visible, wsDef, startRow: currentRow);

        // ── Aggregate / totals row ────────────────────────────────────────────
        if (wsDef.IncludeAggregateRow && nextRow > headerRow + 1)
        {
            WriteAggregateRow(ws, visible,
                dataStartRow: headerRow + 1,
                dataEndRow: nextRow - 1,
                totalsRow: nextRow);
        }

        // ── Freeze header row ─────────────────────────────────────────────────
        if (wsDef.FreezeHeaderRow)
            ws.SheetView.FreezeRows(headerRow);

        // ── AutoFilter on the header row ──────────────────────────────────────
        if (wsDef.AutoFilter)
            ws.Range(headerRow, 1, headerRow, visible.Count).SetAutoFilter();

        // ── Column widths ─────────────────────────────────────────────────────
        if (wsDef.AutoFitColumns)
            ws.ColumnsUsed().AdjustToContents();

        // Explicit widths override auto-fit
        for (int i = 0; i < visible.Count; i++)
        {
            if (visible[i].Width.HasValue)
                ws.Column(i + 1).Width = visible[i].Width!.Value;
        }
    }

    private static void WriteTitleRow(
        IXLWorksheet ws, string title, int colCount, int row, AdvancedExcelHeaderStyle style)
    {
        var cell = ws.Cell(row, 1);
        cell.Value = (XLCellValue)title;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = style.FontSize + 4;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(style.BackgroundColor);
        cell.Style.Font.FontColor = XLColor.FromHtml(style.FontColor);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        if (colCount > 1)
            ws.Range(row, 1, row, colCount).Merge();
    }

    private static void WriteSubTitleRow(
        IXLWorksheet ws, string subtitle, int colCount, int row, AdvancedExcelHeaderStyle style)
    {
        var cell = ws.Cell(row, 1);
        cell.Value = (XLCellValue)subtitle;
        cell.Style.Font.Bold = false;
        cell.Style.Font.FontSize = style.FontSize + 1;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(style.BackgroundColor);
        cell.Style.Font.FontColor = XLColor.FromHtml(style.FontColor);
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        if (colCount > 1)
            ws.Range(row, 1, row, colCount).Merge();
    }

    private static void WriteGroupHeaderRow(
        IXLWorksheet ws,
        List<AdvancedExcelGroupHeader> groups,
        int colCount,
        int row,
        AdvancedExcelHeaderStyle style)
    {
        foreach (var grp in groups)
        {
            int startCol = Math.Max(1, grp.StartColumnIndex);
            int endCol = Math.Min(colCount, grp.EndColumnIndex);

            var cell = ws.Cell(row, startCol);
            cell.Value = (XLCellValue)grp.Title;
            cell.Style.Font.Bold = style.Bold;
            cell.Style.Font.FontSize = style.FontSize;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(style.BackgroundColor);
            cell.Style.Font.FontColor = XLColor.FromHtml(style.FontColor);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            if (endCol > startCol)
                ws.Range(row, startCol, row, endCol).Merge();
        }
    }

    private static void WriteHeaderRow(
        IXLWorksheet ws,
        List<AdvancedExcelColumnDefinition> visible,
        AdvancedExcelHeaderStyle style,
        int headerRow = 1)
    {
        for (int i = 0; i < visible.Count; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = (XLCellValue)(visible[i].Header ?? visible[i].ColumnName);
            cell.Style.Font.Bold = style.Bold;
            cell.Style.Font.FontSize = style.FontSize;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(style.BackgroundColor);
            cell.Style.Font.FontColor = XLColor.FromHtml(style.FontColor);
            cell.Style.Alignment.Horizontal = MapHorizontalAlignment(style.HorizontalAlignment);
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("1F3864");
        }
    }

    private static int WriteDataRows(
        IXLWorksheet ws,
        DataTable data,
        List<AdvancedExcelColumnDefinition> visible,
        AdvancedExcelWorksheetDefinition wsDef,
        int startRow = 2)
    {
        int rowIndex = startRow;
        var altColor = XLColor.FromHtml(wsDef.AlternatingRowColor);

        foreach (DataRow dataRow in data.Rows)
        {
            // Alternating row shading (even rows)
            if (wsDef.AlternatingRowShading && rowIndex % 2 == 0)
            {
                ws.Row(rowIndex).Style.Fill.BackgroundColor = altColor;
            }

            for (int col = 0; col < visible.Count; col++)
            {
                var colDef = visible[col];
                var cell = ws.Cell(rowIndex, col + 1);
                var raw = dataRow.IsNull(colDef.ColumnName) ? null : dataRow[colDef.ColumnName];

                SetCellValue(cell, raw);

                if (!string.IsNullOrEmpty(colDef.NumberFormat))
                    cell.Style.NumberFormat.Format = colDef.NumberFormat;

                cell.Style.Alignment.Horizontal = MapHorizontalAlignment(colDef.Alignment);

                if (colDef.IsBold)
                    cell.Style.Font.Bold = true;

                if (wsDef.WrapText)
                    cell.Style.Alignment.WrapText = true;
            }

            rowIndex++;
        }

        return rowIndex;
    }

    private static void WriteAggregateRow(
        IXLWorksheet ws,
        List<AdvancedExcelColumnDefinition> visible,
        int dataStartRow,
        int dataEndRow,
        int totalsRow)
    {
        bool hasAny = false;

        for (int i = 0; i < visible.Count; i++)
        {
            var colDef = visible[i];
            if (colDef.AggregateType == ExcelAggregateType.None)
                continue;

            hasAny = true;
            var cell = ws.Cell(totalsRow, i + 1);
            var colLetter = XLHelper.GetColumnLetterFromNumber(i + 1);

            cell.FormulaA1 = colDef.AggregateType switch
            {
                ExcelAggregateType.Sum => $"SUM({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                ExcelAggregateType.Average => $"AVERAGE({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                ExcelAggregateType.Count => $"COUNT({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                ExcelAggregateType.CountA => $"COUNTA({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                ExcelAggregateType.Min => $"MIN({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                ExcelAggregateType.Max => $"MAX({colLetter}{dataStartRow}:{colLetter}{dataEndRow})",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(colDef.NumberFormat))
                cell.Style.NumberFormat.Format = colDef.NumberFormat;

            cell.Style.Font.Bold = true;
            cell.Style.Border.TopBorder = XLBorderStyleValues.Double;
        }

        // Label the first empty cell in the totals row
        if (hasAny && visible.Count > 0 && visible[0].AggregateType == ExcelAggregateType.None)
        {
            var labelCell = ws.Cell(totalsRow, 1);
            labelCell.Value = (XLCellValue)"Total";
            labelCell.Style.Font.Bold = true;
        }
    }

    // ── Column resolution ─────────────────────────────────────────────────────

    /// <summary>
    /// Merges explicit <see cref="AdvancedExcelColumnDefinition"/> overrides with the DataTable
    /// column order. DataTable columns not covered by the list use default settings.
    /// </summary>
    private static List<AdvancedExcelColumnDefinition> BuildEffectiveColumns(
        DataTable data,
        List<AdvancedExcelColumnDefinition>? columnOverrides)
    {
        if (columnOverrides is { Count: > 0 })
        {
            // Return definitions in the order specified, ignoring DataTable order
            return columnOverrides;
        }

        // Auto-generate from DataTable columns with default settings
        return data.Columns
            .Cast<DataColumn>()
            .Select(c => new AdvancedExcelColumnDefinition { ColumnName = c.ColumnName })
            .ToList();
    }

    // ── Value & alignment helpers ─────────────────────────────────────────────

    private static void SetCellValue(IXLCell cell, object? value)
    {
        if (value is null) return;

        cell.Value = value switch
        {
            bool b => (XLCellValue)b,
            sbyte n => (XLCellValue)(double)n,
            byte n => (XLCellValue)(double)n,
            short n => (XLCellValue)(double)n,
            ushort n => (XLCellValue)(double)n,
            int n => (XLCellValue)(double)n,
            uint n => (XLCellValue)(double)n,
            long n => (XLCellValue)(double)n,
            ulong n => (XLCellValue)(double)n,
            float n => (XLCellValue)(double)n,
            double n => (XLCellValue)n,
            decimal n => (XLCellValue)(double)n,
            DateTime dt => (XLCellValue)dt,
            DateOnly d => (XLCellValue)d.ToDateTime(TimeOnly.MinValue),
            TimeSpan ts => (XLCellValue)ts.ToString(),
            string s => (XLCellValue)s,
            _ => (XLCellValue)(value.ToString() ?? string.Empty)
        };
    }

    private static XLAlignmentHorizontalValues MapHorizontalAlignment(ExcelHorizontalAlignment alignment) =>
        alignment switch
        {
            ExcelHorizontalAlignment.Left => XLAlignmentHorizontalValues.Left,
            ExcelHorizontalAlignment.Center => XLAlignmentHorizontalValues.Center,
            ExcelHorizontalAlignment.Right => XLAlignmentHorizontalValues.Right,
            _ => XLAlignmentHorizontalValues.General
        };

    private async Task AcquireSlotAsync(CancellationToken cancellationToken, string reportName)
    {
        try
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new ReportGenerationException(
                "ClosedXML report generation was cancelled while waiting for a concurrency slot.",
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

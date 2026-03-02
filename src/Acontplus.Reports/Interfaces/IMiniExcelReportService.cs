namespace Acontplus.Reports.Interfaces;

/// <summary>
/// High-performance, stream-based Excel report service powered by MiniExcel.
/// Designed for large-dataset exports with minimal memory overhead — data is streamed directly
/// to the output without loading the entire workbook into memory.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When to use MiniExcel:</strong> bulk data exports, large DataTable results, POCO collections,
/// API-driven CSV/Excel downloads where speed and memory efficiency matter more than rich formatting.
/// </para>
/// <para>
/// For richly formatted workbooks with corporate styles, freeze panes, formulas, and aggregates use
/// <see cref="IClosedXmlReportService"/> instead.
/// </para>
/// </remarks>
public interface IMiniExcelReportService
{
    /// <summary>
    /// Generates an Excel workbook (.xlsx) from a fully configured <see cref="ExcelReportRequest"/>.
    /// Supports both single-sheet and multi-sheet workbooks.
    /// </summary>
    /// <param name="request">Workbook definition including one or more worksheet sources.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> with the workbook bytes and MIME metadata.</returns>
    Task<ReportResponse> GenerateAsync(
        ExcelReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience shortcut that wraps a single <see cref="System.Data.DataTable"/>
    /// into a one-sheet workbook.
    /// </summary>
    /// <param name="fileDownloadName">Desired download file name (extension appended if absent).</param>
    /// <param name="data">Source data table.</param>
    /// <param name="columns">
    /// Optional column descriptors for header overrides, format hints, and visibility.
    /// <see langword="null"/> exports all columns with their original names.
    /// </param>
    /// <param name="worksheetName">Sheet tab name (default: <c>"Sheet1"</c>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> with the workbook bytes.</returns>
    Task<ReportResponse> GenerateFromDataTableAsync(
        string fileDownloadName,
        DataTable data,
        IEnumerable<ExcelColumnDefinition>? columns = null,
        string worksheetName = "Sheet1",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a strongly-typed collection to a single-sheet workbook using MiniExcel's native
    /// POCO serialisation. Column headers are derived from public property names unless decorated
    /// with <c>MiniExcelLibs.Attributes.ExcelColumnNameAttribute</c>.
    /// </summary>
    /// <typeparam name="T">POCO type whose public properties become columns.</typeparam>
    /// <param name="fileDownloadName">Desired download file name.</param>
    /// <param name="data">Data collection to export.</param>
    /// <param name="worksheetName">Sheet tab name (default: <c>"Sheet1"</c>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> with the workbook bytes.</returns>
    Task<ReportResponse> GenerateFromObjectsAsync<T>(
        string fileDownloadName,
        IEnumerable<T> data,
        string worksheetName = "Sheet1",
        CancellationToken cancellationToken = default);
}

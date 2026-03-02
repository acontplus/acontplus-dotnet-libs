namespace Acontplus.Reports.Interfaces;

/// <summary>
/// Advanced, richly formatted Excel report service powered by ClosedXML.
/// Supports corporate header styling, freeze panes, AutoFilter, alternating row shading,
/// aggregate/formula totals rows, multi-sheet workbooks, and workbook metadata.
/// </summary>
/// <remarks>
/// <para>
/// <strong>When to use ClosedXML:</strong> invoices, financial statements, management dashboards,
/// any report where presentation and in-cell formulas matter, or when the consumer edits the file.
/// </para>
/// <para>
/// For raw bulk data exports with minimal overhead use <see cref="IMiniExcelReportService"/> instead.
/// </para>
/// </remarks>
public interface IClosedXmlReportService
{
    /// <summary>
    /// Generates a fully formatted Excel workbook (.xlsx) from an
    /// <see cref="AdvancedExcelReportRequest"/>.
    /// </summary>
    /// <param name="request">
    /// Workbook definition including worksheets, header styles, column formatting, and metadata.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> with the workbook bytes and MIME metadata.</returns>
    Task<ReportResponse> GenerateAsync(
        AdvancedExcelReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience shortcut that wraps a single <see cref="System.Data.DataTable"/>
    /// into a formatted single-sheet workbook.
    /// </summary>
    /// <param name="fileDownloadName">Desired download file name (extension appended if absent).</param>
    /// <param name="data">Source data table.</param>
    /// <param name="columns">
    /// Optional per-column rich formatting descriptors.
    /// <see langword="null"/> exports all columns with default styles.
    /// </param>
    /// <param name="worksheetName">Sheet tab name (default: <c>"Sheet1"</c>).</param>
    /// <param name="autoFilter">Enable AutoFilter on the header row (default: <see langword="true"/>).</param>
    /// <param name="freezeHeaderRow">Freeze the header row (default: <see langword="true"/>).</param>
    /// <param name="headerStyle">
    /// Header row style override.
    /// <see langword="null"/> uses <see cref="AdvancedExcelHeaderStyle.CorporateBlue"/>.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> with the workbook bytes.</returns>
    Task<ReportResponse> GenerateFromDataTableAsync(
        string fileDownloadName,
        DataTable data,
        IEnumerable<AdvancedExcelColumnDefinition>? columns = null,
        string worksheetName = "Sheet1",
        bool autoFilter = true,
        bool freezeHeaderRow = true,
        AdvancedExcelHeaderStyle? headerStyle = null,
        CancellationToken cancellationToken = default);
}

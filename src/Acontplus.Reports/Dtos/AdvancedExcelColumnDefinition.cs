namespace Acontplus.Reports.Dtos;

/// <summary>
/// Column descriptor for advanced Excel generation (ClosedXML).
/// Supports rich formatting, column widths, horizontal alignment, bold cells,
/// Excel number format codes, and aggregate totals rows.
/// </summary>
public class AdvancedExcelColumnDefinition
{
    /// <summary>Exact column name as it appears in the source <see cref="System.Data.DataTable"/></summary>
    public required string ColumnName { get; set; }

    /// <summary>
    /// Display header label written to the header row.
    /// Defaults to <see cref="ColumnName"/> when not specified.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Column width in character units.
    /// <see langword="null"/> lets <see cref="AdvancedExcelWorksheetDefinition.AutoFitColumns"/> decide.
    /// Explicit width overrides auto-fit.
    /// </summary>
    public double? Width { get; set; }

    /// <summary>
    /// Excel built-in or custom number format code applied to data cells.
    /// Examples: <c>"#,##0.00"</c>, <c>"$#,##0.00_);[Red]($#,##0.00)"</c>,
    ///           <c>"yyyy-MM-dd"</c>, <c>"0.00%"</c>.
    /// </summary>
    public string? NumberFormat { get; set; }

    /// <summary>Horizontal alignment of data cells (default: <see cref="ExcelHorizontalAlignment.General"/>)</summary>
    public ExcelHorizontalAlignment Alignment { get; set; } = ExcelHorizontalAlignment.General;

    /// <summary>When <see langword="true"/>, data cells in this column are rendered in bold</summary>
    public bool IsBold { get; set; } = false;

    /// <summary>When <see langword="true"/> the column is hidden from the rendered workbook</summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>
    /// Aggregate function applied in the totals row when
    /// <see cref="AdvancedExcelWorksheetDefinition.IncludeAggregateRow"/> is <see langword="true"/>.
    /// Default: <see cref="ExcelAggregateType.None"/>.
    /// </summary>
    public ExcelAggregateType AggregateType { get; set; } = ExcelAggregateType.None;
}

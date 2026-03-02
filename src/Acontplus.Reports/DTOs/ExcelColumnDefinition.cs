namespace Acontplus.Reports.Dtos;

/// <summary>
/// Column descriptor for simple Excel generation (MiniExcel).
/// Controls header override, number formatting, and visibility.
/// </summary>
public class ExcelColumnDefinition
{
    /// <summary>Exact column name as it appears in the source <see cref="System.Data.DataTable"/></summary>
    public required string ColumnName { get; set; }

    /// <summary>
    /// Display header label written to the first row.
    /// Defaults to <see cref="ColumnName"/> when not specified.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Optional .NET composite format string applied when projecting the value
    /// (e.g. <c>"N2"</c>, <c>"C2"</c>, <c>"yyyy-MM-dd"</c>).
    /// Note: MiniExcel renders cells as plain values; for rich in-cell formatting use
    /// <see cref="AdvancedExcelColumnDefinition.NumberFormat"/> with ClosedXML.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>When <see langword="true"/> the column is excluded from the output</summary>
    public bool IsHidden { get; set; } = false;
}

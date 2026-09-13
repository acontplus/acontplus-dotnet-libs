namespace Acontplus.Reports.Dtos;

/// <summary>
/// Options for configuring single-sheet Excel export from a <see cref="System.Data.DataTable"/> via ClosedXML.
/// </summary>
public class ClosedXmlDataTableOptions
{
    /// <summary>
    /// Optional per-column rich formatting descriptors.
    /// When <see langword="null"/>, all columns are exported with default styles.
    /// </summary>
    public IEnumerable<AdvancedExcelColumnDefinition>? Columns { get; set; }

    /// <summary>
    /// Sheet tab name (default: <c>"Sheet1"</c>).
    /// </summary>
    public string WorksheetName { get; set; } = "Sheet1";

    /// <summary>
    /// Enable Excel AutoFilter drop-downs on the header row (default: <see langword="true"/>).
    /// </summary>
    public bool AutoFilter { get; set; } = true;

    /// <summary>
    /// Freeze the header row so it stays visible while scrolling (default: <see langword="true"/>).
    /// </summary>
    public bool FreezeHeaderRow { get; set; } = true;

    /// <summary>
    /// Header row style override.
    /// When <see langword="null"/>, uses <see cref="AdvancedExcelHeaderStyle.CorporateBlue"/>.
    /// </summary>
    public AdvancedExcelHeaderStyle? HeaderStyle { get; set; }
}

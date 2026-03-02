namespace Acontplus.Reports.Dtos;

/// <summary>
/// Defines a single worksheet within an advanced ClosedXML workbook.
/// Supports rich header styling, freeze panes, auto-filter, alternating row shading,
/// aggregate totals rows, and per-column formatting.
/// </summary>
public class AdvancedExcelWorksheetDefinition
{
    /// <summary>Worksheet tab name shown in Excel</summary>
    public required string Name { get; set; }

    /// <summary>Source data to write into this worksheet</summary>
    public required DataTable Data { get; set; }

    /// <summary>
    /// Per-column rich formatting descriptors.
    /// When <see langword="null"/>, all visible DataTable columns are exported with default styles.
    /// </summary>
    public List<AdvancedExcelColumnDefinition>? Columns { get; set; }

    /// <summary>
    /// Header row style.
    /// <see langword="null"/> uses <see cref="AdvancedExcelHeaderStyle.CorporateBlue"/>.
    /// </summary>
    public AdvancedExcelHeaderStyle? HeaderStyle { get; set; }

    /// <summary>Enable Excel AutoFilter drop-downs on the header row (default: <see langword="true"/>)</summary>
    public bool AutoFilter { get; set; } = true;

    /// <summary>Freeze the header row so it stays visible while scrolling (default: <see langword="true"/>)</summary>
    public bool FreezeHeaderRow { get; set; } = true;

    /// <summary>
    /// Auto-fit column widths to their content after data is written (default: <see langword="true"/>).
    /// Explicit <see cref="AdvancedExcelColumnDefinition.Width"/> values override auto-fit.
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Render an aggregate totals row below the data using per-column
    /// <see cref="AdvancedExcelColumnDefinition.AggregateType"/> values (default: <see langword="false"/>).
    /// </summary>
    public bool IncludeAggregateRow { get; set; } = false;

    /// <summary>
    /// Apply alternating row background shading for readability (default: <see langword="true"/>).
    /// </summary>
    public bool AlternatingRowShading { get; set; } = true;

    /// <summary>
    /// Alternating row background colour as a 6-char HTML hex string
    /// (default: <c>"DCE6F1"</c> — light blue tint).
    /// </summary>
    public string AlternatingRowColor { get; set; } = "DCE6F1";

    /// <summary>
    /// Enable text wrapping for all data cells in this worksheet (default: <see langword="false"/>).
    /// </summary>
    public bool WrapText { get; set; } = false;
}

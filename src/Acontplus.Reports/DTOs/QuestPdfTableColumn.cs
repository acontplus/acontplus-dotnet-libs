namespace Acontplus.Reports.Dtos;

/// <summary>
/// Describes a single column within a QuestPDF dynamic table section
/// </summary>
public class QuestPdfTableColumn
{
    /// <summary>Exact column name as it appears in the source DataTable</summary>
    public required string ColumnName { get; set; }

    /// <summary>Display header label (defaults to ColumnName if not set)</summary>
    public string? Header { get; set; }

    /// <summary>
    /// Relative column width. When all columns specify a width the layout
    /// distributes space proportionally. Leave null to auto-distribute evenly.
    /// </summary>
    public float? RelativeWidth { get; set; }

    /// <summary>Text alignment within the column cells (default: Left)</summary>
    public QuestPdfColumnAlignment Alignment { get; set; } = QuestPdfColumnAlignment.Left;

    /// <summary>Optional .NET composite format string applied to the cell value (e.g., "N2", "C2", "yyyy-MM-dd")</summary>
    public string? Format { get; set; }

    /// <summary>Aggregate function rendered in the totals row (default: None)</summary>
    public QuestPdfAggregateType AggregateType { get; set; } = QuestPdfAggregateType.None;

    /// <summary>When true the column is hidden from the rendered output</summary>
    public bool IsHidden { get; set; } = false;

    /// <summary>Optional CSS-style font weight: "bold" renders the cell text bold</summary>
    public bool IsBold { get; set; } = false;
}

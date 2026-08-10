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

    // ── Group / band headers ──────────────────────────────────────────────────

    /// <summary>
    /// When <see langword="true"/> this descriptor defines a group (band) header cell
    /// rendered in a separate row <em>above</em> the normal column header row.
    /// Group headers are used to replicate RDLC <c>ColSpan</c> grouped headers
    /// (e.g. Kardex Entradas / Salidas / Saldo bands).
    /// Group header entries do not correspond to DataTable columns.
    /// </summary>
    public bool IsGroupHeader { get; set; } = false;

    /// <summary>
    /// Number of data columns this header cell spans (default: 1).
    /// Only meaningful when <see cref="IsGroupHeader"/> is <see langword="true"/>.
    /// </summary>
    public int ColumnSpan { get; set; } = 1;
}

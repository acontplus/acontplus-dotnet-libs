using QuestPDF.Infrastructure;

namespace Acontplus.Reports.Dtos;

/// <summary>
/// Describes a single content section within a QuestPDF dynamic report.
/// Sections are rendered in order inside the document body.
/// </summary>
public class QuestPdfSection
{
    /// <summary>Optional heading displayed above section content</summary>
    public string? SectionTitle { get; set; }

    /// <summary>Determines how section content is composed (default: DataTable)</summary>
    public QuestPdfSectionType Type { get; set; } = QuestPdfSectionType.DataTable;

    // ── DataTable section ────────────────────────────────────────────────────

    /// <summary>Data source for DataTable sections</summary>
    public DataTable? Data { get; set; }

    /// <summary>
    /// Column descriptors controlling visibility, ordering, and formatting.
    /// When empty all DataTable columns are rendered in declaration order.
    /// </summary>
    public List<QuestPdfTableColumn> Columns { get; set; } = [];

    /// <summary>Render a totals/aggregate row after the last data row</summary>
    public bool ShowTotalsRow { get; set; } = false;

    // ── Text section ─────────────────────────────────────────────────────────

    /// <summary>Text blocks rendered in a Text section</summary>
    public List<QuestPdfTextBlock> TextBlocks { get; set; } = [];

    // ── KeyValue summary section ──────────────────────────────────────────────

    /// <summary>
    /// Ordered key-value pairs rendered in a two-column summary panel.
    /// Key = label, Value = display text.
    /// </summary>
    public Dictionary<string, string> KeyValues { get; set; } = [];

    // ── Custom section ────────────────────────────────────────────────────────

    /// <summary>
    /// Delegate invoked to compose arbitrary QuestPDF content when Type is Custom.
    /// The parameter is the container passed by QuestPDF.
    /// </summary>
    public Action<IContainer>? CustomComposer { get; set; }

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>Spacing in points added below this section (default: 12)</summary>
    public float PaddingBottom { get; set; } = 12f;

    /// <summary>When true the section always starts at the top of a new page</summary>
    public bool PageBreakBefore { get; set; } = false;
}

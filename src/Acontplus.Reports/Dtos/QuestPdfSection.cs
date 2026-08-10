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

    // ── Image section ─────────────────────────────────────────────────────────

    /// <summary>Raw image bytes (PNG/JPEG) rendered in an <see cref="QuestPdfSectionType.Image"/> section</summary>
    public byte[]? ImageBytes { get; set; }

    /// <summary>Maximum image height in points (default: 80). Set to 0 for no restriction.</summary>
    public float ImageMaxHeight { get; set; } = 80f;

    /// <summary>Maximum image width in points (default: 160). Set to 0 for no restriction.</summary>
    public float ImageMaxWidth { get; set; } = 160f;

    /// <summary>Horizontal alignment of the rendered image (default: Center)</summary>
    public QuestPdfColumnAlignment ImageAlignment { get; set; } = QuestPdfColumnAlignment.Center;

    // ── Barcode section ───────────────────────────────────────────────────────

    /// <summary>
    /// Text to encode into a barcode or QR code. Used when
    /// <see cref="BarcodeBytes"/> is not provided. For SRI access keys pass the
    /// 49-character clave de acceso string.
    /// </summary>
    public string? BarcodeText { get; set; }

    /// <summary>
    /// Pre-generated barcode image bytes. Takes priority over <see cref="BarcodeText"/>
    /// auto-generation. Use this when the barcode was already produced by
    /// <c>RdlcReportService.AddReportParameters</c> and you want to reuse it.
    /// </summary>
    public byte[]? BarcodeBytes { get; set; }

    /// <summary>Barcode visual style: Code128 (1D) or QrCode (2D) (default: Code128)</summary>
    public QuestPdfBarcodeType BarcodeType { get; set; } = QuestPdfBarcodeType.Code128;

    /// <summary>Maximum barcode image height in points (default: 50)</summary>
    public float BarcodeHeight { get; set; } = 50f;

    /// <summary>Maximum barcode image width in points (default: 120). Set to 0 for no restriction.</summary>
    public float BarcodeWidth { get; set; } = 120f;

    /// <summary>Horizontal alignment of the barcode image (default: Left)</summary>
    public QuestPdfColumnAlignment BarcodeAlignment { get; set; } = QuestPdfColumnAlignment.Left;

    /// <summary>When true, the encoded text is rendered as a caption below the barcode image</summary>
    public bool ShowBarcodeCaption { get; set; } = false;

    // ── MasterDetail section ──────────────────────────────────────────────────

    /// <summary>
    /// Column name in <see cref="Data"/> (master table) whose value is used to
    /// filter <see cref="DetailData"/> rows for each master row.
    /// </summary>
    public string? MasterKeyColumn { get; set; }

    /// <summary>
    /// Column name in <see cref="DetailData"/> that must match the master key value.
    /// </summary>
    public string? DetailKeyColumn { get; set; }

    /// <summary>Source DataTable for the detail (child) rows in a MasterDetail section</summary>
    public DataTable? DetailData { get; set; }

    /// <summary>
    /// Column descriptors for the detail sub-table.
    /// When empty, all DataTable columns are auto-generated.
    /// </summary>
    public List<QuestPdfTableColumn> DetailColumns { get; set; } = [];

    /// <summary>Optional heading displayed above each master row's detail sub-table</summary>
    public string? DetailSectionTitle { get; set; }

    /// <summary>Render a totals row at the bottom of each detail sub-table (default: false)</summary>
    public bool ShowDetailTotalsRow { get; set; } = false;

    // ── TwoColumn section ─────────────────────────────────────────────────────

    /// <summary>
    /// The content type to render in the LEFT column of a TwoColumn layout.
    /// Defaults to <see cref="QuestPdfSectionType.DataTable"/>.
    /// The left column uses this section's own data (<see cref="Data"/>, <see cref="TextBlocks"/>, etc.).
    /// </summary>
    public QuestPdfSectionType LeftContentType { get; set; } = QuestPdfSectionType.DataTable;

    /// <summary>
    /// Content section rendered in the RIGHT column of a TwoColumn layout.
    /// Set <see cref="QuestPdfSection.Type"/> on this instance to control its render mode.
    /// </summary>
    public QuestPdfSection? RightSection { get; set; }

    /// <summary>Relative width weight of the left column (default: 1)</summary>
    public float LeftColumnRatio { get; set; } = 1f;

    /// <summary>Relative width weight of the right column (default: 1)</summary>
    public float RightColumnRatio { get; set; } = 1f;

    /// <summary>Gap between left and right columns in points (default: 8)</summary>
    public float TwoColumnGap { get; set; } = 8f;

    // ── InvoiceHeader section ─────────────────────────────────────────────────

    /// <summary>
    /// SRI Ecuador invoice / voucher header definition.
    /// Required when <see cref="Type"/> is <see cref="QuestPdfSectionType.InvoiceHeader"/>.
    /// </summary>
    public QuestPdfInvoiceHeader? InvoiceHeader { get; set; }

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>Spacing in points added below this section (default: 12)</summary>
    public float PaddingBottom { get; set; } = 12f;

    /// <summary>When true the section always starts at the top of a new page</summary>
    public bool PageBreakBefore { get; set; } = false;
}

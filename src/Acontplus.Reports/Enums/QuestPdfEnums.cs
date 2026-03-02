namespace Acontplus.Reports.Enums;

/// <summary>
/// Page size presets for QuestPDF documents
/// </summary>
public enum QuestPdfPageSize
{
    A4,
    A3,
    A5,
    Letter,
    Legal,
    Tabloid,
    Executive,
    /// <summary>Thermal/receipt-style narrow format (80mm wide)</summary>
    Thermal80mm
}

/// <summary>
/// Page orientation for QuestPDF documents
/// </summary>
public enum QuestPdfPageOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Column content alignment within QuestPDF table cells
/// </summary>
public enum QuestPdfColumnAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Type of section rendered inside a QuestPDF document
/// </summary>
public enum QuestPdfSectionType
{
    /// <summary>Render a DataTable as a formatted grid</summary>
    DataTable,
    /// <summary>Render free-form text blocks</summary>
    Text,
    /// <summary>Render a horizontal key-value summary panel</summary>
    KeyValueSummary,
    /// <summary>Render a custom composed element via delegate</summary>
    Custom,
    /// <summary>Render a byte[] image in-line</summary>
    Image,
    /// <summary>Auto-generate a barcode or QR code from text and render it as an image</summary>
    Barcode,
    /// <summary>Render a master DataTable with a filtered detail sub-table per row (master-detail)</summary>
    MasterDetail,
    /// <summary>Render two child sections side by side in two columns</summary>
    TwoColumn,
    /// <summary>Render a first-class SRI Ecuador invoice / voucher header block</summary>
    InvoiceHeader
}

/// <summary>
/// Horizontal cell alignment for numeric totals row
/// </summary>
public enum QuestPdfAggregateType
{
    None,
    Sum,
    Count,
    Average
}

/// <summary>
/// Visual style of barcode/QR code generated in a <see cref="QuestPdfSectionType.Barcode"/> section
/// </summary>
public enum QuestPdfBarcodeType
{
    /// <summary>1D linear barcode (Code 128) — default; used for SRI 49-char claves de acceso</summary>
    Code128,
    /// <summary>2D QR code</summary>
    QrCode
}

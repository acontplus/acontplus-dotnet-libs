namespace Acontplus.Reports.Enums;

/// <summary>
/// Page size presets for QuestPDF documents
/// </summary>
public enum QuestPdfPageSize
{
    /// <summary>ISO A4 format (210 x 297 mm).</summary>
    A4,
    /// <summary>ISO A3 format (297 x 420 mm).</summary>
    A3,
    /// <summary>ISO A5 format (148 x 210 mm).</summary>
    A5,
    /// <summary>North American Letter format (8.5 x 11 in).</summary>
    Letter,
    /// <summary>North American Legal format (8.5 x 14 in).</summary>
    Legal,
    /// <summary>North American Tabloid format (11 x 17 in).</summary>
    Tabloid,
    /// <summary>Executive format (7.25 x 10.5 in).</summary>
    Executive,
    /// <summary>Thermal/receipt-style narrow format (80mm wide)</summary>
    Thermal80mm
}

/// <summary>
/// Page orientation for QuestPDF documents
/// </summary>
public enum QuestPdfPageOrientation
{
    /// <summary>Vertical portrait orientation.</summary>
    Portrait,
    /// <summary>Horizontal landscape orientation.</summary>
    Landscape
}

/// <summary>
/// Column content alignment within QuestPDF table cells
/// </summary>
public enum QuestPdfColumnAlignment
{
    /// <summary>Left alignment.</summary>
    Left,
    /// <summary>Center alignment.</summary>
    Center,
    /// <summary>Right alignment.</summary>
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
    /// <summary>Render two child sections side by subject in two columns</summary>
    TwoColumn,
    /// <summary>Render a first-class SRI Ecuador invoice / voucher header block</summary>
    InvoiceHeader
}

/// <summary>
/// Horizontal cell alignment for numeric totals row
/// </summary>
public enum QuestPdfAggregateType
{
    /// <summary>No aggregate calculation.</summary>
    None,
    /// <summary>Sum of numeric values.</summary>
    Sum,
    /// <summary>Count of items.</summary>
    Count,
    /// <summary>Arithmetic average of values.</summary>
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

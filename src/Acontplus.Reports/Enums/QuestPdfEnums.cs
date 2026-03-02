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
    Custom
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

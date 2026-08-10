namespace Acontplus.Reports.Dtos;

/// <summary>
/// Page layout and typographic settings for a QuestPDF document
/// </summary>
public class QuestPdfDocumentSettings
{
    /// <summary>Standard page size preset (default: A4)</summary>
    public QuestPdfPageSize PageSize { get; set; } = QuestPdfPageSize.A4;

    /// <summary>Page orientation (default: Portrait)</summary>
    public QuestPdfPageOrientation Orientation { get; set; } = QuestPdfPageOrientation.Portrait;

    /// <summary>Top margin in points (default: 30)</summary>
    public float MarginTop { get; set; } = 30f;

    /// <summary>Bottom margin in points (default: 30)</summary>
    public float MarginBottom { get; set; } = 30f;

    /// <summary>Left margin in points (default: 25)</summary>
    public float MarginLeft { get; set; } = 25f;

    /// <summary>Right margin in points (default: 25)</summary>
    public float MarginRight { get; set; } = 25f;

    /// <summary>Default content font family (default: "Helvetica")</summary>
    public string FontFamily { get; set; } = "Helvetica";

    /// <summary>Default body font size in points (default: 9)</summary>
    public float FontSize { get; set; } = 9f;

    /// <summary>Table header font size in points (default: 9)</summary>
    public float TableHeaderFontSize { get; set; } = 9f;

    /// <summary>Document title font size in points (default: 16)</summary>
    public float TitleFontSize { get; set; } = 16f;

    /// <summary>Section heading font size in points (default: 12)</summary>
    public float SectionTitleFontSize { get; set; } = 12f;

    /// <summary>Visual color theme applied throughout the document</summary>
    public QuestPdfColorTheme ColorTheme { get; set; } = new();

    /// <summary>Render a page number footer automatically (default: true)</summary>
    public bool ShowPageNumbers { get; set; } = true;

    /// <summary>Include document generation timestamp in footer (default: false)</summary>
    public bool ShowTimestamp { get; set; } = false;

    /// <summary>Show company/brand watermark behind content (default: false)</summary>
    public bool ShowWatermark { get; set; } = false;

    /// <summary>Watermark text rendered diagonally behind content</summary>
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Font size of the watermark text in points (default: 80).
    /// Reduce for narrow pages or increase for landscape Tabloid reports.
    /// </summary>
    public float WatermarkFontSize { get; set; } = 80f;

    /// <summary>
    /// Hex color of the watermark text (default: <c>#EEEEEE</c> — near-white).
    /// Use a very light grey so overlaid data content remains legible.
    /// </summary>
    public string WatermarkColor { get; set; } = "#EEEEEE";

    /// <summary>QuestPDF license type: Community or Professional/Enterprise (default: Community)</summary>
    public QuestPdfLicenseType LicenseType { get; set; } = QuestPdfLicenseType.Community;
}

/// <summary>
/// QuestPDF license tier configuration
/// </summary>
public enum QuestPdfLicenseType
{
    Community,
    Professional,
    Enterprise
}

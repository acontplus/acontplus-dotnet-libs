namespace Acontplus.Reports.Dtos;

/// <summary>
/// Configuration for the document-level header or footer band
/// </summary>
public class QuestPdfHeaderFooterOptions
{
    /// <summary>Primary text rendered left-aligned in the band</summary>
    public string? LeftText { get; set; }

    /// <summary>Text rendered centered in the band</summary>
    public string? CenterText { get; set; }

    /// <summary>Text rendered right-aligned in the band</summary>
    public string? RightText { get; set; }

    /// <summary>Path to an image file rendered on the left side of the header</summary>
    public string? LogoPath { get; set; }

    /// <summary>Logo image max height in points (default: 30)</summary>
    public float LogoMaxHeight { get; set; } = 30f;

    /// <summary>Background color of the header/footer band as hex</summary>
    public string? BackgroundColor { get; set; }

    /// <summary>Draw a bottom border line on the header band</summary>
    public bool ShowBorderBottom { get; set; } = true;

    /// <summary>Font size for header/footer text (default: 8)</summary>
    public float FontSize { get; set; } = 8f;
}

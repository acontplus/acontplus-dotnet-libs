namespace Acontplus.Reports.Dtos;

/// <summary>
/// A block of formatted text rendered in a text-type section
/// </summary>
public class QuestPdfTextBlock
{
    /// <summary>The text content to render</summary>
    public required string Content { get; set; }

    /// <summary>Font size override (null uses document default)</summary>
    public float? FontSize { get; set; }

    /// <summary>Render content as bold text</summary>
    public bool Bold { get; set; } = false;

    /// <summary>Render content as italic text</summary>
    public bool Italic { get; set; } = false;

    /// <summary>Hex color override for text (null uses theme default)</summary>
    public string? Color { get; set; }

    /// <summary>Spacing in points added below this block</summary>
    public float PaddingBottom { get; set; } = 4f;
}

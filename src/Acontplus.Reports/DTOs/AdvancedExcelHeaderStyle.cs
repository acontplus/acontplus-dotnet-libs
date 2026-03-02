namespace Acontplus.Reports.Dtos;

/// <summary>
/// Styling applied to the header row of an advanced Excel worksheet.
/// Colours are expressed as 6-character HTML hex strings without the leading <c>#</c>.
/// </summary>
public class AdvancedExcelHeaderStyle
{
    /// <summary>
    /// Cell background colour as a 6-char HTML hex string (default: <c>"4472C4"</c> — Microsoft Blue).
    /// </summary>
    public string BackgroundColor { get; set; } = "4472C4";

    /// <summary>
    /// Font colour as a 6-char HTML hex string (default: <c>"FFFFFF"</c> — White).
    /// </summary>
    public string FontColor { get; set; } = "FFFFFF";

    /// <summary>Font size in points (default: <c>11</c>)</summary>
    public double FontSize { get; set; } = 11;

    /// <summary>Bold header text (default: <see langword="true"/>)</summary>
    public bool Bold { get; set; } = true;

    /// <summary>Horizontal alignment of header cells (default: <see cref="ExcelHorizontalAlignment.Center"/>)</summary>
    public ExcelHorizontalAlignment HorizontalAlignment { get; set; } = ExcelHorizontalAlignment.Center;

    // ── Presets ───────────────────────────────────────────────────────────────

    /// <summary>Returns a deep-blue corporate header style (default)</summary>
    public static AdvancedExcelHeaderStyle CorporateBlue() => new();

    /// <summary>Returns a dark-green header style</summary>
    public static AdvancedExcelHeaderStyle DarkGreen() =>
        new() { BackgroundColor = "375623", FontColor = "FFFFFF" };

    /// <summary>Returns a charcoal/dark-grey header style</summary>
    public static AdvancedExcelHeaderStyle DarkGrey() =>
        new() { BackgroundColor = "404040", FontColor = "FFFFFF" };

    /// <summary>Returns a subtle light-blue header suitable for pastel themes</summary>
    public static AdvancedExcelHeaderStyle LightBlue() =>
        new() { BackgroundColor = "BDD7EE", FontColor = "1F3864" };
}

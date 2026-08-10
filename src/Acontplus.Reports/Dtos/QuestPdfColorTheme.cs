namespace Acontplus.Reports.Dtos;

/// <summary>
/// Defines the visual color theme applied to a QuestPDF report.
/// Default values use the official <b>Acontplus brand palette</b> sourced from the
/// AcontplusWeb design system (globals.css).
/// Use <see cref="QuestPdfColorThemes"/> to obtain pre-built presets.
/// </summary>
public class QuestPdfColorTheme
{
    // ── Header ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Header row / page-header background color (hex).
    /// Default: <c>#d61672</c> — Acontplus brand magenta.
    /// </summary>
    public string HeaderBackground { get; set; } = "#d61672";

    /// <summary>
    /// Header row / page-header foreground (text) color (hex).
    /// Default: <c>#FFFFFF</c> — white.
    /// </summary>
    public string HeaderForeground { get; set; } = "#FFFFFF";

    // ── Table rows ───────────────────────────────────────────────────────────

    /// <summary>
    /// Even (non-alternate) data row background color (hex).
    /// Default: <c>#FFFFFF</c> — white.
    /// </summary>
    public string RowBackground { get; set; } = "#FFFFFF";

    /// <summary>
    /// Odd (alternate) data row background color (hex).
    /// Default: <c>#fdf2f8</c> — Acontplus blush / light pink.
    /// </summary>
    public string AlternateRowBackground { get; set; } = "#fdf2f8";

    /// <summary>
    /// Totals row background color (hex).
    /// Default: <c>#fce7f3</c> — light pink, one step deeper than <see cref="AlternateRowBackground"/>.
    /// </summary>
    public string TotalsBackground { get; set; } = "#fce7f3";

    /// <summary>
    /// Totals row text color (hex).
    /// Default: <c>#831843</c> — Acontplus deep wine / dark primary.
    /// </summary>
    public string TotalsTextColor { get; set; } = "#831843";

    // ── Accents ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Primary accent used for section titles, dividers and highlights (hex).
    /// Default: <c>#d61672</c> — Acontplus brand magenta.
    /// </summary>
    public string AccentColor { get; set; } = "#d61672";

    /// <summary>
    /// Secondary accent for charts, badges, call-out cells (hex).
    /// Default: <c>#ffa901</c> — Acontplus brand amber.
    /// </summary>
    public string SecondaryAccentColor { get; set; } = "#ffa901";

    // ── Text ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Default body text color (hex).
    /// Default: <c>#252525</c> — Acontplus near-black foreground.
    /// </summary>
    public string TextColor { get; set; } = "#252525";

    /// <summary>
    /// Muted / secondary text color used for dates, subtitles, footnotes (hex).
    /// Default: <c>#8c8c8c</c> — Acontplus muted foreground.
    /// </summary>
    public string MutedTextColor { get; set; } = "#8c8c8c";

    /// <summary>
    /// Key-value label (key column) text color (hex).
    /// Default: <c>#be185d</c> — Acontplus primary-dark.
    /// </summary>
    public string KvKeyColor { get; set; } = "#be185d";

    /// <summary>
    /// Page footer text color (hex).
    /// Default: <c>#8c8c8c</c> — Acontplus muted foreground.
    /// </summary>
    public string FooterTextColor { get; set; } = "#8c8c8c";

    // ── Borders ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Default border / separator color (hex).
    /// Default: <c>#eaeaea</c> — Acontplus border.
    /// </summary>
    public string BorderColor { get; set; } = "#eaeaea";

    // ── Status ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Success indicator color (hex).
    /// Default: <c>#10b981</c> — Acontplus success emerald.
    /// </summary>
    public string SuccessColor { get; set; } = "#10b981";

    /// <summary>
    /// Warning indicator color (hex).
    /// Default: <c>#f59e0b</c> — Acontplus warning amber.
    /// </summary>
    public string WarningColor { get; set; } = "#f59e0b";

    /// <summary>
    /// Error / danger indicator color (hex).
    /// Default: <c>#ef4444</c> — Acontplus error red.
    /// </summary>
    public string ErrorColor { get; set; } = "#ef4444";
}

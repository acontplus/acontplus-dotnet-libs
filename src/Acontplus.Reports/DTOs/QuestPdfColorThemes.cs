namespace Acontplus.Reports.Dtos;

/// <summary>
/// Factory class that provides ready-to-use <see cref="QuestPdfColorTheme"/> presets.<br/>
/// Use these as a starting point and then override individual properties as needed.<br/><br/>
/// Available presets:
/// <list type="bullet">
///   <item><see cref="AcontplusDefault"/> — official Acontplus brand (magenta / pink).</item>
///   <item><see cref="AcontplusAmber"/> — Acontplus amber / warm gold variant.</item>
///   <item><see cref="Corporate"/> — conservative navy-blue for enterprise documents.</item>
///   <item><see cref="Ocean"/> — deep teal / cyan palette.</item>
///   <item><see cref="Monochrome"/> — slate grey, zero color distraction.</item>
/// </list>
/// </summary>
public static class QuestPdfColorThemes
{
    // ── Acontplus brand ───────────────────────────────────────────────────────

    /// <summary>
    /// Official <b>Acontplus brand theme</b> — magenta header, blush alternates,
    /// deep-wine totals. Matches the AcontplusWeb design system.
    /// </summary>
    public static QuestPdfColorTheme AcontplusDefault() => new()
    {
        HeaderBackground = "#d61672",   // --acontplus-primary
        HeaderForeground = "#FFFFFF",
        RowBackground = "#FFFFFF",
        AlternateRowBackground = "#fdf2f8", // --acontplus-light
        TotalsBackground = "#fce7f3",   // rose-100
        TotalsTextColor = "#831843",   // --acontplus-dark
        AccentColor = "#d61672",   // --acontplus-primary
        SecondaryAccentColor = "#ffa901",   // --acontplus-accent
        TextColor = "#252525",   // --foreground
        MutedTextColor = "#8c8c8c",   // --muted-foreground
        KvKeyColor = "#be185d",   // --acontplus-primary-dark
        FooterTextColor = "#8c8c8c",   // --muted-foreground
        BorderColor = "#eaeaea",   // --border
        SuccessColor = "#10b981",   // --acontplus-success
        WarningColor = "#f59e0b",   // --acontplus-warning
        ErrorColor = "#ef4444",   // --acontplus-error
    };

    /// <summary>
    /// <b>Acontplus Amber</b> — the brand amber / warm-gold palette.
    /// Great for invoices, financial summaries and seasonal reports.
    /// </summary>
    public static QuestPdfColorTheme AcontplusAmber() => new()
    {
        HeaderBackground = "#ffa901",   // --acontplus-accent
        HeaderForeground = "#252525",   // dark text on amber header
        RowBackground = "#FFFFFF",
        AlternateRowBackground = "#fffbeb", // amber-50
        TotalsBackground = "#fef3c7",   // amber-100
        TotalsTextColor = "#92400e",   // amber-800
        AccentColor = "#d61672",   // --acontplus-primary (cross-accent)
        SecondaryAccentColor = "#ffc303",   // --acontplus-accent-light
        TextColor = "#252525",
        MutedTextColor = "#8c8c8c",
        KvKeyColor = "#b45309",   // amber-700
        FooterTextColor = "#8c8c8c",
        BorderColor = "#fde68a",   // amber-200
        SuccessColor = "#10b981",
        WarningColor = "#f59e0b",
        ErrorColor = "#ef4444",
    };

    // ── Enterprise / neutral ──────────────────────────────────────────────────

    /// <summary>
    /// <b>Corporate</b> — classic navy-blue enterprise theme.
    /// Conservative and widely accepted for legal, financial and government reports.
    /// </summary>
    public static QuestPdfColorTheme Corporate() => new()
    {
        HeaderBackground = "#1E3A5F",   // deep navy
        HeaderForeground = "#FFFFFF",
        RowBackground = "#FFFFFF",
        AlternateRowBackground = "#F5F7FA", // slate-50
        TotalsBackground = "#E8EEFA",   // indigo-50
        TotalsTextColor = "#1E3A5F",
        AccentColor = "#2E86AB",   // sky-700
        SecondaryAccentColor = "#FFB703",   // warm amber
        TextColor = "#1A1A2E",
        MutedTextColor = "#6B7280",   // gray-500
        KvKeyColor = "#1E3A5F",
        FooterTextColor = "#6B7280",
        BorderColor = "#D0D7E3",
        SuccessColor = "#16A34A",   // green-600
        WarningColor = "#D97706",   // amber-600
        ErrorColor = "#DC2626",   // red-600
    };

    /// <summary>
    /// <b>Ocean</b> — deep teal / cyan palette inspired by coastal blue.
    /// Fresh and modern; suitable for tech products and dashboards.
    /// </summary>
    public static QuestPdfColorTheme Ocean() => new()
    {
        HeaderBackground = "#0077B6",   // deep sky-blue
        HeaderForeground = "#FFFFFF",
        RowBackground = "#FFFFFF",
        AlternateRowBackground = "#E0F7FA", // cyan-50
        TotalsBackground = "#B2EBF2",   // cyan-100
        TotalsTextColor = "#00363D",   // dark teal
        AccentColor = "#0096C7",   // sky-500
        SecondaryAccentColor = "#00B4D8",   // cyan-400
        TextColor = "#023E58",
        MutedTextColor = "#607D8B",   // blue-grey-500
        KvKeyColor = "#0077B6",
        FooterTextColor = "#607D8B",
        BorderColor = "#B2EBF2",
        SuccessColor = "#00897B",   // teal-600
        WarningColor = "#F57C00",   // orange-700
        ErrorColor = "#E53935",   // red-600
    };

    /// <summary>
    /// <b>Monochrome</b> — slate-grey, zero colour distraction.
    /// Best for formal reports, legal documents or when colour printing is unavailable.
    /// </summary>
    public static QuestPdfColorTheme Monochrome() => new()
    {
        HeaderBackground = "#334155",   // slate-700 (--secondary-foreground)
        HeaderForeground = "#FFFFFF",
        RowBackground = "#FFFFFF",
        AlternateRowBackground = "#F8FAFC", // slate-50
        TotalsBackground = "#E2E8F0",   // slate-200
        TotalsTextColor = "#0F172A",   // slate-900
        AccentColor = "#475569",   // slate-600
        SecondaryAccentColor = "#94A3B8",   // slate-400
        TextColor = "#1E293B",   // slate-800
        MutedTextColor = "#94A3B8",
        KvKeyColor = "#334155",
        FooterTextColor = "#94A3B8",
        BorderColor = "#CBD5E1",   // slate-300
        SuccessColor = "#16A34A",
        WarningColor = "#D97706",
        ErrorColor = "#DC2626",
    };
}

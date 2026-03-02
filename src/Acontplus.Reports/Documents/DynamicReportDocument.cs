using System.Globalization;
using System.Security;
using Acontplus.Utilities.Security.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Acontplus.Reports.Documents;

/// <summary>
/// QuestPDF <see cref="IDocument"/> implementation that composes a fully dynamic,
/// data-driven PDF from a <see cref="QuestPdfReportRequest"/>.
/// </summary>
internal sealed class DynamicReportDocument : IDocument
{
    private readonly QuestPdfReportRequest _request;

    public DynamicReportDocument(QuestPdfReportRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    // ── IDocument ────────────────────────────────────────────────────────────

    public DocumentMetadata GetMetadata() => new()
    {
        Title = _request.Title,
        Author = _request.Author ?? "Acontplus.Reports",
        Subject = _request.Subject ?? _request.Title,
        Creator = "Acontplus.Reports (QuestPDF)",
        CreationDate = DateTimeOffset.UtcNow
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        var s = _request.Settings;

        container.Page(page =>
        {
            ApplyPageSize(page, s);

            page.MarginTop(s.MarginTop);
            page.MarginBottom(s.MarginBottom);
            page.MarginLeft(s.MarginLeft);
            page.MarginRight(s.MarginRight);

            page.DefaultTextStyle(t => t
                .FontFamily(s.FontFamily)
                .FontSize(s.FontSize)
                .FontColor(s.ColorTheme.TextColor));

            page.Header().Element(ComposePageHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposePageFooter);
        });
    }

    // ── Page Header ──────────────────────────────────────────────────────────

    private void ComposePageHeader(IContainer container)
    {
        var header = _request.GlobalHeader;
        var theme = _request.Settings.ColorTheme;

        if (header is null)
        {
            // Default minimal title header
            container
                .BorderBottom(1)
                .BorderColor(theme.BorderColor)
                .PaddingBottom(6)
                .Column(col =>
                {
                    col.Item()
                        .Text(_request.Title)
                        .FontSize(_request.Settings.TitleFontSize)
                        .Bold()
                        .FontColor(theme.AccentColor);

                    if (!string.IsNullOrWhiteSpace(_request.SubTitle))
                        col.Item()
                            .Text(_request.SubTitle)
                            .FontSize(_request.Settings.SectionTitleFontSize)
                            .FontColor(theme.TextColor)
                            .Italic();
                });
            return;
        }

        var bg = header.BackgroundColor ?? "transparent";
        var hasBg = bg != "transparent";

        var wrapper = hasBg
            ? container.Background(bg).Padding(8)
            : container.PaddingBottom(6);

        if (header.ShowBorderBottom && !hasBg)
            wrapper = container.BorderBottom(1).BorderColor(theme.BorderColor).PaddingBottom(6);

        wrapper.Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(header.LogoPath) && IsValidLogoPath(header.LogoPath) && File.Exists(header.LogoPath))
            {
                row.AutoItem()
                    .MaxHeight(header.LogoMaxHeight)
                    .PaddingRight(8)
                    .Image(header.LogoPath);
            }

            row.RelativeItem().Column(col =>
            {
                if (!string.IsNullOrWhiteSpace(header.LeftText))
                    col.Item()
                        .Text(header.LeftText)
                        .FontSize(header.FontSize)
                        .FontColor(hasBg ? "#FFFFFF" : theme.TextColor);
            });

            if (!string.IsNullOrWhiteSpace(header.CenterText))
                row.RelativeItem()
                    .AlignCenter()
                    .Text(header.CenterText)
                    .FontSize(header.FontSize)
                    .FontColor(hasBg ? "#FFFFFF" : theme.TextColor);

            if (!string.IsNullOrWhiteSpace(header.RightText))
                row.AutoItem()
                    .AlignRight()
                    .Text(header.RightText)
                    .FontSize(header.FontSize)
                    .FontColor(hasBg ? "#FFFFFF" : theme.TextColor);
        });
    }

    // ── Content ──────────────────────────────────────────────────────────────

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(0);

            foreach (var section in _request.Sections)
            {
                if (section.PageBreakBefore)
                    col.Item().PageBreak();

                col.Item().Element(c => ComposeSection(c, section));

                if (section.PaddingBottom > 0)
                    col.Item().Height(section.PaddingBottom);
            }
        });
    }

    private void ComposeSection(IContainer container, QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;

        container.Column(col =>
        {
            // Section heading
            if (!string.IsNullOrWhiteSpace(section.SectionTitle))
            {
                col.Item()
                    .BorderBottom(1)
                    .BorderColor(theme.AccentColor)
                    .PaddingBottom(3)
                    .PaddingTop(4)
                    .Text(section.SectionTitle)
                    .FontSize(_request.Settings.SectionTitleFontSize)
                    .Bold()
                    .FontColor(theme.AccentColor);

                col.Item().Height(6);
            }

            col.Item().Element(c =>
            {
                switch (section.Type)
                {
                    case QuestPdfSectionType.Text:
                        ComposeTextSection(c, section);
                        break;
                    case QuestPdfSectionType.KeyValueSummary:
                        ComposeKeyValueSection(c, section);
                        break;
                    case QuestPdfSectionType.Custom:
                        InvokeCustomSection(c, section);
                        break;
                    default:
                        ComposeDataTableSection(c, section);
                        break;
                }
            });
        });
    }

    // ── Section: DataTable ───────────────────────────────────────────────────

    private void ComposeDataTableSection(IContainer container, QuestPdfSection section)
    {
        var dt = section.Data;
        var theme = _request.Settings.ColorTheme;
        var s = _request.Settings;

        if (dt is null || dt.Rows.Count == 0)
        {
            container.Text("No data available.").Italic().FontColor(theme.MutedTextColor);
            return;
        }

        // Resolve visible columns
        var columns = ResolveColumns(dt, section.Columns);

        container.Table(table =>
        {
            // Column definitions
            table.ColumnsDefinition(def =>
            {
                var totalWeight = columns.Sum(c => c.RelativeWidth ?? 1f);
                foreach (var col in columns)
                    def.RelativeColumn(col.RelativeWidth ?? (1f / columns.Count * totalWeight));
            });

            // Header row (called once; iterate columns inside the delegate)
            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    header.Cell()
                        .Background(theme.HeaderBackground)
                        .Padding(5)
                        .AlignElement(col.Alignment)
                        .Text(col.Header ?? col.ColumnName)
                        .FontSize(s.TableHeaderFontSize)
                        .Bold()
                        .FontColor(theme.HeaderForeground);
                }
            });

            // Data rows
            var rowIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                var isAlternate = rowIndex % 2 == 1;
                var bg = isAlternate ? theme.AlternateRowBackground : theme.RowBackground;

                foreach (var col in columns)
                {
                    var rawValue = row[col.ColumnName];
                    var displayValue = FormatCellValue(rawValue, col.Format);

                    var cell = table.Cell()
                        .Background(bg)
                        .BorderBottom(1)
                        .BorderColor(theme.BorderColor)
                        .Padding(4);

                    var textElem = cell
                        .AlignElement(col.Alignment)
                        .Text(displayValue)
                        .FontSize(s.FontSize)
                        .FontColor(theme.TextColor);

                    if (col.IsBold)
                        textElem.Bold();
                }

                rowIndex++;
            }

            // Optional totals row
            if (section.ShowTotalsRow && columns.Any(c => c.AggregateType != QuestPdfAggregateType.None))
            {
                foreach (var col in columns)
                {
                    var agg = ComputeAggregate(dt, col);

                    table.Cell()
                        .Background(theme.TotalsBackground)
                        .Padding(4)
                        .AlignElement(col.Alignment)
                        .Text(agg)
                        .FontSize(s.FontSize)
                        .Bold()
                        .FontColor(theme.TotalsTextColor);
                }
            }
        });
    }

    // ── Section: Text ────────────────────────────────────────────────────────

    private void ComposeTextSection(IContainer container, QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;

        container.Column(col =>
        {
            foreach (var block in section.TextBlocks)
            {
                col.Item()
                    .PaddingBottom(block.PaddingBottom)
                    .Element(c =>
                    {
                        var txt = c.Text(block.Content)
                            .FontSize(block.FontSize ?? _request.Settings.FontSize)
                            .FontColor(block.Color ?? theme.TextColor);

                        if (block.Bold) txt.Bold();
                        if (block.Italic) txt.Italic();
                    });
            }
        });
    }

    // ── Section: Key-Value ───────────────────────────────────────────────────

    private void ComposeKeyValueSection(IContainer container, QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;
        var s = _request.Settings;

        container.Table(table =>
        {
            table.ColumnsDefinition(def =>
            {
                def.RelativeColumn(2);
                def.RelativeColumn(3);
            });

            var rowIndex = 0;
            foreach (var kv in section.KeyValues)
            {
                var bg = rowIndex % 2 == 1 ? theme.AlternateRowBackground : theme.RowBackground;

                table.Cell()
                    .Background(bg)
                    .Padding(4)
                    .Text(kv.Key)
                    .FontSize(s.FontSize)
                    .Bold()
                    .FontColor(theme.KvKeyColor);

                table.Cell()
                    .Background(bg)
                    .Padding(4)
                    .Text(kv.Value)
                    .FontSize(s.FontSize)
                    .FontColor(theme.TextColor);

                rowIndex++;
            }
        });
    }

    // ── Section: Custom ──────────────────────────────────────────────────────

    private static void InvokeCustomSection(IContainer container, QuestPdfSection section)
    {
        if (section.CustomComposer is not null)
            section.CustomComposer(container);
        else
            container.Text("Custom section composer not configured.").Italic();
    }

    // ── Page Footer ──────────────────────────────────────────────────────────

    private void ComposePageFooter(IContainer container)
    {
        var footer = _request.GlobalFooter;
        var theme = _request.Settings.ColorTheme;
        var s = _request.Settings;

        if (footer is null)
        {
            if (!s.ShowPageNumbers && !s.ShowTimestamp)
                return;

            container
                .BorderTop(1)
                .BorderColor(theme.BorderColor)
                .PaddingTop(4)
                .Row(row =>
                {
                    if (s.ShowTimestamp)
                        row.RelativeItem()
                            .Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm UTC"))
                            .FontSize(7)
                            .FontColor(theme.FooterTextColor);

                    if (s.ShowPageNumbers)
                        row.RelativeItem()
                            .AlignRight()
                            .Text(text =>
                            {
                                text.Span("Page ").FontSize(7).FontColor(theme.FooterTextColor);
                                text.CurrentPageNumber().FontSize(7).FontColor(theme.FooterTextColor);
                                text.Span(" of ").FontSize(7).FontColor(theme.FooterTextColor);
                                text.TotalPages().FontSize(7).FontColor(theme.FooterTextColor);
                            });
                });
            return;
        }

        // Custom footer
        var hasBg = !string.IsNullOrWhiteSpace(footer.BackgroundColor);
        var wrapper = hasBg
            ? container.Background(footer.BackgroundColor!).Padding(6)
            : (IContainer)container.BorderTop(1).BorderColor(theme.BorderColor).PaddingTop(4);

        wrapper.Row(row =>
        {
            if (!string.IsNullOrWhiteSpace(footer.LeftText))
                row.RelativeItem()
                    .Text(footer.LeftText)
                    .FontSize(footer.FontSize)
                    .FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);

            if (!string.IsNullOrWhiteSpace(footer.CenterText))
                row.RelativeItem()
                    .AlignCenter()
                    .Text(footer.CenterText)
                    .FontSize(footer.FontSize)
                    .FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);

            if (!string.IsNullOrWhiteSpace(footer.RightText) || s.ShowPageNumbers)
                row.AutoItem()
                    .AlignRight()
                    .Text(text =>
                    {
                        if (!string.IsNullOrWhiteSpace(footer.RightText))
                            text.Span(footer.RightText!).FontSize(footer.FontSize).FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);

                        if (s.ShowPageNumbers)
                        {
                            text.Span("  Page ").FontSize(footer.FontSize).FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);
                            text.CurrentPageNumber().FontSize(footer.FontSize).FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);
                            text.Span("/").FontSize(footer.FontSize).FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);
                            text.TotalPages().FontSize(footer.FontSize).FontColor(hasBg ? "#FFFFFF" : theme.FooterTextColor);
                        }
                    });
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void ApplyPageSize(PageDescriptor page, QuestPdfDocumentSettings s)
    {
        var size = s.PageSize switch
        {
            QuestPdfPageSize.A3 => PageSizes.A3,
            QuestPdfPageSize.A5 => PageSizes.A5,
            QuestPdfPageSize.Letter => PageSizes.Letter,
            QuestPdfPageSize.Legal => PageSizes.Legal,
            QuestPdfPageSize.Tabloid => PageSizes.Tabloid,
            QuestPdfPageSize.Executive => PageSizes.Executive,
            QuestPdfPageSize.Thermal80mm => new PageSize(226.77f, 841.89f), // 80mm × A4 height
            _ => PageSizes.A4
        };

        page.Size(s.Orientation == QuestPdfPageOrientation.Landscape ? size.Landscape() : size);
    }

    private static List<QuestPdfTableColumn> ResolveColumns(DataTable dt, List<QuestPdfTableColumn> requested)
    {
        if (requested.Count == 0)
        {
            // Auto-generate columns from DataTable schema
            return dt.Columns
                .Cast<DataColumn>()
                .Select(c => new QuestPdfTableColumn { ColumnName = c.ColumnName })
                .ToList();
        }

        // Use explicit definitions, preserving order and hiding hidden ones
        return requested
            .Where(c => !c.IsHidden && dt.Columns.Contains(c.ColumnName))
            .ToList();
    }

    private static string FormatCellValue(object? value, string? format)
    {
        if (value is null or DBNull) return string.Empty;
        if (string.IsNullOrWhiteSpace(format)) return value.ToString() ?? string.Empty;

        return value switch
        {
            IFormattable f => f.ToString(format, CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string ComputeAggregate(DataTable dt, QuestPdfTableColumn col)
    {
        if (col.AggregateType == QuestPdfAggregateType.None) return string.Empty;
        if (!dt.Columns.Contains(col.ColumnName)) return string.Empty;

        var values = dt.Rows
            .Cast<DataRow>()
            .Select(r => r[col.ColumnName])
            .Where(v => v is not null and not DBNull);

        return col.AggregateType switch
        {
            QuestPdfAggregateType.Count => dt.Rows.Count.ToString(),
            QuestPdfAggregateType.Sum => SumValues(values, col.Format),
            QuestPdfAggregateType.Average => AvgValues(values, col.Format),
            _ => string.Empty
        };
    }

    private static string SumValues(IEnumerable<object> values, string? format)
    {
        try
        {
            var sum = values.Sum(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));
            return FormatCellValue(sum, format);
        }
        catch { return string.Empty; }
    }

    private static string AvgValues(IEnumerable<object> values, string? format)
    {
        try
        {
            var list = values.Select(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture)).ToList();
            if (list.Count == 0) return string.Empty;
            var avg = list.Sum() / list.Count;
            return FormatCellValue(avg, format);
        }
        catch { return string.Empty; }
    }

    /// <summary>
    /// Validates that <paramref name="path"/> is a safe, non-traversal image path.
    /// Prevents CWE-22 path traversal attacks by rejecting null bytes, relative
    /// directory components, and non-image file extensions.
    /// </summary>
    private static bool IsValidLogoPath(string path)
    {
        try
        {
            // Reject null bytes and relative traversal sequences
            if (path.Contains('\0') || path.Contains(".."))
                return false;

            // Only allow common image extensions
            PathSecurityValidator.ValidateFileExtension(path, ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp");

            // Canonicalize the path; GetFullPath will throw on invalid input
            var fullPath = Path.GetFullPath(path);

            // The canonicalized path must be rooted and must equal itself (no hidden traversal)
            return Path.IsPathRooted(fullPath);
        }
        catch (SecurityException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Internal extension to apply horizontal alignment to a QuestPDF container
/// </summary>
internal static class ContainerAlignExtensions
{
    internal static IContainer AlignElement(this IContainer container, QuestPdfColumnAlignment alignment) =>
        alignment switch
        {
            QuestPdfColumnAlignment.Center => container.AlignCenter(),
            QuestPdfColumnAlignment.Right => container.AlignRight(),
            _ => container.AlignLeft()
        };
}

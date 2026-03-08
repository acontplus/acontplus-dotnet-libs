using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Security;
using ZXing;

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

            if (s.ShowWatermark && !string.IsNullOrWhiteSpace(s.WatermarkText))
                page.Foreground()
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(s.WatermarkText)
                    .FontSize(s.WatermarkFontSize)
                    .FontColor(s.WatermarkColor);
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
            if (header.LogoBytes is { Length: > 0 })
            {
                row.AutoItem()
                    .MaxHeight(header.LogoMaxHeight)
                    .PaddingRight(8)
                    .Image(new MemoryStream(header.LogoBytes)).FitHeight();
            }
            else if (!string.IsNullOrWhiteSpace(header.LogoPath) && IsValidLogoPath(header.LogoPath) && File.Exists(header.LogoPath))
            {
                row.AutoItem()
                    .MaxHeight(header.LogoMaxHeight)
                    .PaddingRight(8)
                    .Image(header.LogoPath).FitHeight();
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

            col.Item().Element(c => ComposeSectionContent(c, section, section.Type));
        });
    }

    /// <summary>
    /// Dispatches to the correct compose method based on <paramref name="renderType"/>.
    /// Called by <see cref="ComposeSection"/> and recursively by
    /// <see cref="ComposeTwoColumnSection"/> for each column's content.
    /// </summary>
    private void ComposeSectionContent(
        IContainer container, QuestPdfSection section, QuestPdfSectionType renderType)
    {
        switch (renderType)
        {
            case QuestPdfSectionType.Text:
                ComposeTextSection(container, section);
                break;
            case QuestPdfSectionType.KeyValueSummary:
                ComposeKeyValueSection(container, section);
                break;
            case QuestPdfSectionType.Image:
                ComposeImageSection(container, section);
                break;
            case QuestPdfSectionType.Barcode:
                ComposeBarcodeSection(container, section);
                break;
            case QuestPdfSectionType.MasterDetail:
                ComposeMasterDetailSection(container, section);
                break;
            case QuestPdfSectionType.TwoColumn:
                ComposeTwoColumnSection(container, section);
                break;
            case QuestPdfSectionType.InvoiceHeader:
                ComposeInvoiceHeaderSection(container, section);
                break;
            case QuestPdfSectionType.Custom:
                InvokeCustomSection(container, section);
                break;
            default:
                ComposeDataTableSection(container, section);
                break;
        }
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

        // Separate band (group) header descriptors from regular data columns
        var allColumns = ResolveColumns(dt, section.Columns);
        var groupHeaders = allColumns.Where(c => c.IsGroupHeader).ToList();
        var columns = allColumns.Where(c => !c.IsGroupHeader).ToList();

        if (columns.Count == 0)
        {
            container.Text("No columns defined.").Italic().FontColor(theme.MutedTextColor);
            return;
        }

        container.Table(table =>
        {
            // Column definitions — data columns only, not band descriptors
            table.ColumnsDefinition(def =>
            {
                var totalWeight = columns.Sum(c => c.RelativeWidth ?? 1f);
                foreach (var col in columns)
                    def.RelativeColumn(col.RelativeWidth ?? (1f / columns.Count * totalWeight));
            });

            // Header — optional band row first, then individual column headers
            table.Header(header =>
            {
                // Band / group header row (e.g. Kardex: Entradas | Salidas | Saldo)
                if (groupHeaders.Count > 0)
                {
                    foreach (var grp in groupHeaders)
                    {
                        var span = (uint)Math.Max(1, Math.Min(grp.ColumnSpan, columns.Count));
                        header.Cell()
                            .ColumnSpan(span)
                            .Background(theme.AccentColor)
                            .Border(1)
                            .BorderColor(theme.HeaderBackground)
                            .Padding(4)
                            .AlignCenter()
                            .Text(grp.Header ?? string.Empty)
                            .FontSize(s.TableHeaderFontSize)
                            .Bold()
                            .FontColor("#FFFFFF");
                    }
                }

                // Normal column header row
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

    // ── Section: Image ───────────────────────────────────────────────────────

    private void ComposeImageSection(IContainer container, QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;

        if (section.ImageBytes is not { Length: > 0 })
        {
            container.Text("Image not available.").Italic().FontColor(theme.MutedTextColor);
            return;
        }

        IContainer aligned = section.ImageAlignment switch
        {
            QuestPdfColumnAlignment.Center => container.AlignCenter(),
            QuestPdfColumnAlignment.Right => container.AlignRight(),
            _ => container.AlignLeft()
        };

        IContainer sized = section.ImageMaxWidth > 0 ? aligned.MaxWidth(section.ImageMaxWidth) : aligned;

        if (section.ImageMaxHeight > 0)
            sized.MaxHeight(section.ImageMaxHeight).Image(new MemoryStream(section.ImageBytes));
        else
            sized.Image(new MemoryStream(section.ImageBytes));
    }

    // ── Section: Barcode ─────────────────────────────────────────────────────

    private void ComposeBarcodeSection(IContainer container, QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;
        byte[] barcodeBytes;

        if (section.BarcodeBytes is { Length: > 0 })
        {
            barcodeBytes = section.BarcodeBytes;
        }
        else if (!string.IsNullOrWhiteSpace(section.BarcodeText))
        {
            try
            {
                var cfg = new BarcodeConfig
                {
                    Text = section.BarcodeText,
                    Format = section.BarcodeType == QuestPdfBarcodeType.QrCode
                                       ? BarcodeFormat.QR_CODE
                                       : BarcodeFormat.CODE_128,
                    Width = section.BarcodeWidth > 0 ? (int)section.BarcodeWidth * 3 : 900,
                    Height = section.BarcodeHeight > 0 ? (int)section.BarcodeHeight * 3 : 150,
                    IncludeLabel = section.ShowBarcodeCaption
                };
                barcodeBytes = BarcodeGen.Create(cfg);
            }
            catch
            {
                container.Text("Barcode generation failed.").Italic().FontColor(theme.MutedTextColor);
                return;
            }
        }
        else
        {
            container.Text("No barcode data.").Italic().FontColor(theme.MutedTextColor);
            return;
        }

        IContainer aligned = section.BarcodeAlignment switch
        {
            QuestPdfColumnAlignment.Center => container.AlignCenter(),
            QuestPdfColumnAlignment.Right => container.AlignRight(),
            _ => container.AlignLeft()
        };

        IContainer sized = section.BarcodeWidth > 0
            ? aligned.MaxWidth(section.BarcodeWidth).MaxHeight(section.BarcodeHeight)
            : aligned.MaxHeight(section.BarcodeHeight);

        sized.Image(new MemoryStream(barcodeBytes));
    }

    // ── Section: MasterDetail ────────────────────────────────────────────────

    private void ComposeMasterDetailSection(IContainer container, QuestPdfSection section)
    {
        var dt = section.Data;
        var theme = _request.Settings.ColorTheme;

        if (dt is null || dt.Rows.Count == 0)
        {
            container.Text("No data available.").Italic().FontColor(theme.MutedTextColor);
            return;
        }

        var allMasterCols = ResolveColumns(dt, section.Columns);
        var masterCols = allMasterCols.Where(c => !c.IsGroupHeader).ToList();

        container.Column(col =>
        {
            foreach (DataRow masterRow in dt.Rows)
                col.Item().Element(c => ComposeMasterRow(c, masterRow, masterCols, section));
        });
    }

    private void ComposeMasterRow(
        IContainer container,
        DataRow masterRow,
        List<QuestPdfTableColumn> masterCols,
        QuestPdfSection section)
    {
        var theme = _request.Settings.ColorTheme;
        var s = _request.Settings;

        container.Column(col =>
        {
            // Master row as a colored header band with key+value pairs
            col.Item()
                .Background(theme.HeaderBackground)
                .Padding(5)
                .Row(row =>
                {
                    foreach (var mc in masterCols)
                    {
                        var val = FormatCellValue(masterRow[mc.ColumnName], mc.Format);
                        row.RelativeItem().Column(c2 =>
                        {
                            c2.Item().Text(mc.Header ?? mc.ColumnName)
                                .FontSize(7).Bold().FontColor(theme.HeaderForeground);
                            c2.Item().Text(val)
                                .FontSize(s.FontSize).FontColor(theme.HeaderForeground);
                        });
                    }
                });

            // Filtered detail sub-table
            if (section.DetailData is { Rows.Count: > 0 }
                && !string.IsNullOrWhiteSpace(section.MasterKeyColumn)
                && !string.IsNullOrWhiteSpace(section.DetailKeyColumn))
            {
                var masterKey = masterRow[section.MasterKeyColumn]?.ToString();
                var filtered = section.DetailData.Rows.Cast<DataRow>()
                    .Where(r => r[section.DetailKeyColumn]?.ToString() == masterKey)
                    .ToList();

                if (filtered.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(section.DetailSectionTitle))
                        col.Item().PaddingLeft(8).PaddingTop(2)
                            .Text(section.DetailSectionTitle)
                            .FontSize(7).Italic().FontColor(theme.MutedTextColor);

                    var detailSection = new QuestPdfSection
                    {
                        Data = CreateFilteredTable(section.DetailData, filtered),
                        Columns = section.DetailColumns,
                        ShowTotalsRow = section.ShowDetailTotalsRow
                    };

                    col.Item().PaddingLeft(8).Element(c => ComposeDataTableSection(c, detailSection));
                }
            }

            col.Item().Height(4); // gap between master rows
        });
    }

    private static DataTable CreateFilteredTable(DataTable original, List<DataRow> rows)
    {
        var clone = original.Clone(); // same schema, no rows
        foreach (var r in rows)
            clone.ImportRow(r);
        return clone;
    }

    // ── Section: TwoColumn ───────────────────────────────────────────────────

    private void ComposeTwoColumnSection(IContainer container, QuestPdfSection section)
    {
        container.Row(row =>
        {
            row.RelativeItem(section.LeftColumnRatio)
               .PaddingRight(section.TwoColumnGap / 2)
               .Element(c => ComposeSectionContent(c, section, section.LeftContentType));

            if (section.RightSection is not null)
                row.RelativeItem(section.RightColumnRatio)
                   .PaddingLeft(section.TwoColumnGap / 2)
                   .Element(c => ComposeSectionContent(c, section.RightSection, section.RightSection.Type));
        });
    }

    // ── Section: InvoiceHeader ───────────────────────────────────────────────

    private void ComposeInvoiceHeaderSection(IContainer container, QuestPdfSection section)
    {
        if (section.InvoiceHeader is null)
        {
            container.Text("InvoiceHeader not configured.").Italic();
            return;
        }

        var inv = section.InvoiceHeader;
        var theme = _request.Settings.ColorTheme;
        var s = _request.Settings;

        container.Column(mainCol =>
        {
            // Row 1: company info (left) + SRI authorization box (right)
            mainCol.Item().Row(row =>
            {
                row.RelativeItem(inv.LeftPanelRatio)
                   .PaddingRight(4)
                   .Element(c => ComposeInvoiceCompanyBlock(c, inv, theme, s));

                row.RelativeItem(inv.RightPanelRatio)
                   .Element(c => ComposeInvoiceAuthBox(c, inv, theme, s));
            });

            // Row 2: buyer information
            mainCol.Item().PaddingTop(4)
                   .Element(c => ComposeInvoiceBuyerBlock(c, inv, theme, s));
        });
    }

    private static void ComposeInvoiceCompanyBlock(
        IContainer container, QuestPdfInvoiceHeader inv,
        QuestPdfColorTheme theme, QuestPdfDocumentSettings s)
    {
        container.Column(col =>
        {
            if (inv.LogoBytes is { Length: > 0 })
                col.Item().MaxHeight(inv.LogoMaxHeight).MaxWidth(inv.LogoMaxHeight * 4).AlignLeft()
                   .Image(new MemoryStream(inv.LogoBytes)).FitArea();
            else if (!string.IsNullOrWhiteSpace(inv.LogoPath) && File.Exists(inv.LogoPath))
                col.Item().MaxHeight(inv.LogoMaxHeight).MaxWidth(inv.LogoMaxHeight * 4).AlignLeft()
                   .Image(inv.LogoPath).FitArea();

            if (!string.IsNullOrWhiteSpace(inv.CompanyName))
                col.Item().Text(inv.CompanyName)
                   .FontSize(inv.FontSize + 1).Bold().FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.TradeName))
                col.Item().Text(inv.TradeName)
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.CompanyAddress))
                col.Item().Text(inv.CompanyAddress)
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.BranchAddress))
                col.Item().Text(inv.BranchAddress)
                   .FontSize(inv.FontSize).FontColor(theme.MutedTextColor);

            if (!string.IsNullOrWhiteSpace(inv.CompanyPhone))
                col.Item().Text($"Tel: {inv.CompanyPhone}")
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.CompanyEmail))
                col.Item().Text(inv.CompanyEmail)
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.CompanyActivity))
                col.Item().Text(inv.CompanyActivity)
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.ContribuyenteEspecial))
                col.Item().Text($"Contribuyente Especial: {inv.ContribuyenteEspecial}")
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.ObligadoContabilidad))
                col.Item().Text($"Obligado a llevar Contabilidad: {inv.ObligadoContabilidad}")
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.ContribuyenteRimpe))
                col.Item().Text($"Contribuyente RIMPE: {inv.ContribuyenteRimpe}")
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);

            if (!string.IsNullOrWhiteSpace(inv.AgenteRetencion))
                col.Item().Text($"Agente de Retenci\u00f3n: {inv.AgenteRetencion}")
                   .FontSize(inv.FontSize).FontColor(theme.TextColor);
        });
    }

    private static void ComposeInvoiceAuthBox(
        IContainer container, QuestPdfInvoiceHeader inv,
        QuestPdfColorTheme theme, QuestPdfDocumentSettings s)
    {
        container
            .Border(1)
            .BorderColor(inv.AuthBoxBorderColor)
            .Padding(5)
            .Column(col =>
            {
                InvoiceAuthRow(col, "RUC:", inv.Ruc, inv.FontSize, theme);

                if (!string.IsNullOrWhiteSpace(inv.DocumentType))
                    col.Item().AlignCenter().PaddingBottom(2)
                       .Text(inv.DocumentType)
                       .FontSize(inv.FontSize + 1).Bold().FontColor(theme.TextColor);

                InvoiceAuthRow(col, "N\u00famero:", inv.DocumentNumber, inv.FontSize, theme);
                InvoiceAuthRow(col, "N\u00ba Autorizaci\u00f3n:", inv.AuthorizationNumber, inv.FontSize, theme);
                InvoiceAuthRow(col, "Fecha Autorizaci\u00f3n:", inv.AuthorizationDate, inv.FontSize, theme);
                InvoiceAuthRow(col, "Ambiente:", inv.Environment, inv.FontSize, theme);
                InvoiceAuthRow(col, "Tipo de Emisi\u00f3n:", inv.EmissionType, inv.FontSize, theme);

                if (!string.IsNullOrWhiteSpace(inv.AccessKey))
                {
                    col.Item().PaddingTop(3).Text("Clave de Acceso:")
                       .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                    col.Item().Text(inv.AccessKey)
                       .FontSize(inv.FontSize - 2).FontColor(theme.MutedTextColor);
                }
            });
    }

    private static void InvoiceAuthRow(
        ColumnDescriptor col, string label, string? value,
        float fontSize, QuestPdfColorTheme theme)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().Row(r =>
        {
            r.RelativeItem(2).Text(label).FontSize(fontSize - 1).Bold().FontColor(theme.TextColor);
            r.RelativeItem(3).Text(value).FontSize(fontSize - 1).FontColor(theme.TextColor);
        });
    }

    private static void ComposeInvoiceBuyerBlock(
        IContainer container, QuestPdfInvoiceHeader inv,
        QuestPdfColorTheme theme, QuestPdfDocumentSettings s)
    {
        container
            .Border(1)
            .BorderColor(inv.AuthBoxBorderColor)
            .Padding(5)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(inv.BuyerName))
                        row.RelativeItem(3).Column(c2 =>
                        {
                            c2.Item().Text("Raz\u00f3n Social / Nombres:")
                               .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                            c2.Item().Text(inv.BuyerName)
                               .FontSize(inv.FontSize).FontColor(theme.TextColor);
                        });

                    if (!string.IsNullOrWhiteSpace(inv.EmissionDate))
                        row.RelativeItem(1).Column(c2 =>
                        {
                            c2.Item().Text("Fecha de Emisi\u00f3n:")
                               .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                            c2.Item().Text(inv.EmissionDate)
                               .FontSize(inv.FontSize).FontColor(theme.TextColor);
                        });
                });

                col.Item().Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(inv.BuyerIdentification))
                        row.RelativeItem().Column(c2 =>
                        {
                            c2.Item().Text("Identificaci\u00f3n:")
                               .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                            c2.Item().Text(inv.BuyerIdentification)
                               .FontSize(inv.FontSize).FontColor(theme.TextColor);
                        });

                    if (!string.IsNullOrWhiteSpace(inv.DeliveryReference))
                        row.RelativeItem().Column(c2 =>
                        {
                            c2.Item().Text("Gu\u00eda de Remisi\u00f3n:")
                               .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                            c2.Item().Text(inv.DeliveryReference)
                               .FontSize(inv.FontSize).FontColor(theme.TextColor);
                        });
                });

                if (!string.IsNullOrWhiteSpace(inv.BuyerAddress))
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c2 =>
                        {
                            c2.Item().Text("Direcci\u00f3n:")
                               .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                            c2.Item().Text(inv.BuyerAddress)
                               .FontSize(inv.FontSize).FontColor(theme.TextColor);
                        });
                    });

                foreach (var kv in inv.ExtraFields)
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(kv.Key)
                           .FontSize(inv.FontSize - 1).Bold().FontColor(theme.TextColor);
                        row.RelativeItem(3).Text(kv.Value)
                           .FontSize(inv.FontSize).FontColor(theme.TextColor);
                    });
            });
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

        // Group-header descriptors have no matching DataTable column; regular columns must exist
        return requested
            .Where(c => c.IsGroupHeader || (!c.IsHidden && dt.Columns.Contains(c.ColumnName)))
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

namespace Acontplus.Reports.Dtos;

/// <summary>
/// Top-level request model for QuestPDF dynamic report generation
/// </summary>
public class QuestPdfReportRequest
{
    /// <summary>Document title displayed in the report header and PDF metadata</summary>
    public required string Title { get; set; }

    /// <summary>Optional sub-title rendered below the main title</summary>
    public string? SubTitle { get; set; }

    /// <summary>Author name stored in the PDF metadata</summary>
    public string? Author { get; set; }

    /// <summary>Subject stored in the PDF metadata</summary>
    public string? Subject { get; set; }

    /// <summary>Desired file download name (without extension) returned in ReportResponse</summary>
    public string? FileDownloadName { get; set; }

    /// <summary>Page layout and theme configuration</summary>
    public QuestPdfDocumentSettings Settings { get; set; } = new();

    /// <summary>Global page header rendered on every page</summary>
    public QuestPdfHeaderFooterOptions? GlobalHeader { get; set; }

    /// <summary>
    /// Global page footer rendered on every page.
    /// When null and Settings.ShowPageNumbers is true, a default page-number footer is generated.
    /// </summary>
    public QuestPdfHeaderFooterOptions? GlobalFooter { get; set; }

    /// <summary>Ordered list of content sections composed into the document body</summary>
    public List<QuestPdfSection> Sections { get; set; } = [];
}

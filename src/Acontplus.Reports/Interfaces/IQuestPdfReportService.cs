namespace Acontplus.Reports.Interfaces;

/// <summary>
/// Service for generating dynamic PDF documents using QuestPDF.
/// Supports data-driven table reports, multi-section layouts, key-value summaries,
/// rich text blocks, and fully custom composed content.
/// </summary>
public interface IQuestPdfReportService
{
    /// <summary>
    /// Generates a PDF from a fully configured <see cref="QuestPdfReportRequest"/>.
    /// </summary>
    /// <param name="request">Complete document definition including sections, settings, and headers.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> containing the PDF bytes and content metadata.</returns>
    Task<ReportResponse> GenerateAsync(
        QuestPdfReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload that wraps a single <see cref="DataTable"/> in a report.
    /// </summary>
    /// <param name="title">Document title.</param>
    /// <param name="data">Source data table.</param>
    /// <param name="columns">Optional column descriptors (visibility, formatting, aggregates).</param>
    /// <param name="settings">Optional page and theme settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="ReportResponse"/> containing the PDF bytes.</returns>
    Task<ReportResponse> GenerateFromDataTableAsync(
        string title,
        DataTable data,
        IEnumerable<QuestPdfTableColumn>? columns = null,
        QuestPdfDocumentSettings? settings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all required QuestPDF dependencies and licence settings are
    /// properly configured without generating a full document.
    /// Throws <see cref="ReportGenerationException"/> on misconfiguration.
    /// </summary>
    void ValidateConfiguration();
}

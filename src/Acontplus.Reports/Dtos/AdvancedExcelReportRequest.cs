namespace Acontplus.Reports.Dtos;

/// <summary>
/// Request model for advanced, richly formatted Excel workbook generation via ClosedXML.
/// Supports multi-sheet workbooks with full corporate styling, freeze panes, auto-filter,
/// aggregate formula rows, and workbook-level metadata.
/// </summary>
public class AdvancedExcelReportRequest
{
    /// <summary>
    /// Desired file name for the download (extension <c>.xlsx</c> is appended automatically if omitted).
    /// </summary>
    public required string FileDownloadName { get; set; }

    /// <summary>Author name stored in workbook document properties</summary>
    public string? Author { get; set; }

    /// <summary>Subject stored in workbook document properties</summary>
    public string? Subject { get; set; }

    /// <summary>Company name stored in workbook document properties</summary>
    public string? Company { get; set; }

    /// <summary>Keywords stored in workbook document properties</summary>
    public string? Keywords { get; set; }

    /// <summary>One or more worksheet definitions that make up the workbook</summary>
    public List<AdvancedExcelWorksheetDefinition> Worksheets { get; set; } = [];
}

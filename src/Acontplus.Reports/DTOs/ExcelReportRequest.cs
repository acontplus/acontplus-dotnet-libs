namespace Acontplus.Reports.Dtos;

/// <summary>
/// Request model for high-performance, stream-based Excel workbook generation via MiniExcel.
/// Supports single and multi-sheet workbooks from <see cref="System.Data.DataTable"/> or POCO sources.
/// </summary>
public class ExcelReportRequest
{
    /// <summary>
    /// Desired file name for the download (extension <c>.xlsx</c> is appended automatically if omitted).
    /// </summary>
    public required string FileDownloadName { get; set; }

    /// <summary>One or more worksheet definitions that make up the workbook</summary>
    public List<ExcelWorksheetDefinition> Worksheets { get; set; } = [];
}

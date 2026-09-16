namespace Acontplus.Reports.Dtos;

/// <summary>
/// Properties describing an RDLC report definition and requested rendering format.
/// </summary>
public class ReportPropsDto
{
    /// <summary>Gets or sets the file system or resource path to the RDLC report definition.</summary>
    public required string ReportPath { get; set; }

    /// <summary>Gets or sets the display name or output file name of the report.</summary>
    public string? ReportName { get; set; }

    /// <summary>Gets or sets the rendering output format (e.g. PDF, EXCEL, WORD).</summary>
    public required string ReportFormat { get; set; }
}

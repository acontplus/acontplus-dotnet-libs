namespace Acontplus.Reports.Dtos;

/// <summary>
/// Request DTO containing data sources and report parameters for printing an RDLC report.
/// </summary>
public class RdlcPrintRequestDto
{
    /// <summary>Gets or sets data sources for the RDLC report, keyed by dataset name.</summary>
    public required Dictionary<string, List<Dictionary<string, string>>> DataSources { get; set; }

    /// <summary>Gets or sets parameters to be passed to the RDLC report.</summary>
    public required Dictionary<string, string> ReportParams { get; set; }
}

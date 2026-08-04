namespace Acontplus.Reports.Dtos;

public class RdlcPrintRequestDto
{
    public required Dictionary<string, List<Dictionary<string, string>>> DataSources { get; set; }
    public required Dictionary<string, string> ReportParams { get; set; }
}

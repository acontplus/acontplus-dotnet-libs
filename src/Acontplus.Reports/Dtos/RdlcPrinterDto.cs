namespace Acontplus.Reports.Dtos;

public class RdlcPrinterDto
{
    public short Copies { get; set; }
    public string? DeviceInfo { get; set; }
    public string? FileName { get; set; }
    public string? Format { get; set; }
    public string? LogoDirectory { get; set; }
    public string? LogoName { get; set; }
    public required string PrinterName { get; set; }
    public string? ReportsDirectory { get; set; }
}

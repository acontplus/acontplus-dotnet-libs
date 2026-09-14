namespace Acontplus.Reports.Dtos;

/// <summary>
/// Data transfer object containing printer and print job configuration for RDLC reports.
/// </summary>
public class RdlcPrinterDto
{
    /// <summary>Gets or sets the number of copies to print.</summary>
    public short Copies { get; set; }

    /// <summary>Gets or sets device information XML string for formatting the output.</summary>
    public string? DeviceInfo { get; set; }

    /// <summary>Gets or sets the output file name.</summary>
    public string? FileName { get; set; }

    /// <summary>Gets or sets the rendering format.</summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets the directory path containing the company logo.</summary>
    public string? LogoDirectory { get; set; }

    /// <summary>Gets or sets the file name of the company logo.</summary>
    public string? LogoName { get; set; }

    /// <summary>Gets or sets the name of the target printer.</summary>
    public required string PrinterName { get; set; }

    /// <summary>Gets or sets the directory path containing the RDLC report definition.</summary>
    public string? ReportsDirectory { get; set; }
}

namespace Acontplus.Reports.Interfaces;

public interface IRdlcPrinterService
{
    /// <summary>
    /// Prints a report asynchronously with support for cancellation and timeout
    /// </summary>
    Task<bool> PrintAsync(RdlcPrinterDto rdlcPrinter, RdlcPrintRequestDto printRequest, CancellationToken cancellationToken = default);
}

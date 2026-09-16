namespace Acontplus.Reports.Interfaces;

/// <summary>
/// Service contract for printing RDLC reports directly to physical or network printers.
/// </summary>
public interface IRdlcPrinterService
{
    /// <summary>
    /// Prints a report asynchronously with support for cancellation and timeout
    /// </summary>
    Task<bool> PrintAsync(RdlcPrinterDto rdlcPrinter, RdlcPrintRequestDto printRequest, CancellationToken cancellationToken = default);
}

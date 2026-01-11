using Acontplus.Utilities.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.Versioning;
using System.Security;

namespace Acontplus.Reports.Services;

[SupportedOSPlatform("windows6.1")]
public class RdlcPrinterService : IRdlcPrinterService
{
    private readonly ILogger<RdlcPrinterService> _logger;
    private readonly ReportOptions _options;
    private readonly SemaphoreSlim _printSemaphore;
    private readonly ReportDefinitionCache _cache;

    public RdlcPrinterService(
        ILogger<RdlcPrinterService> logger,
        IOptions<ReportOptions> options,
        ReportDefinitionCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _printSemaphore = new SemaphoreSlim(_options.MaxConcurrentPrintJobs);
    }

    public async Task<bool> PrintAsync(RdlcPrinterDto rdlcPrinter, RdlcPrintRequestDto printRequest, CancellationToken cancellationToken = default)
    {
        if (rdlcPrinter == null) throw new ArgumentNullException(nameof(rdlcPrinter));
        if (printRequest == null) throw new ArgumentNullException(nameof(printRequest));

        var printJobId = Guid.NewGuid().ToString("N")[..8];

        if (_options.EnableDetailedLogging)
        {
            _logger.LogInformation(
                "Starting print job {PrintJobId} for printer {PrinterName}, copies: {Copies}, format: {Format}",
                printJobId, rdlcPrinter.PrinterName, rdlcPrinter.Copies, rdlcPrinter.Format);
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Wait for available print slot
            await _printSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Create timeout cancellation token
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.PrintJobTimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var result = await PrintInternalAsync(rdlcPrinter, printRequest, printJobId, linkedCts.Token);

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (_options.EnableDetailedLogging)
                {
                    _logger.LogInformation(
                        "Print job {PrintJobId} completed successfully in {Duration}ms",
                        printJobId, duration);
                }

                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning(
                    "Print job {PrintJobId} timed out after {Duration}ms (limit: {Timeout}s)",
                    printJobId, duration, _options.PrintJobTimeoutSeconds);
                throw new ReportTimeoutException(_options.PrintJobTimeoutSeconds);
            }
            finally
            {
                _printSemaphore.Release();
            }
        }
        catch (Exception ex) when (ex is not ReportGenerationException)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex,
                "Print job {PrintJobId} failed after {Duration}ms: {ErrorMessage}",
                printJobId, duration, ex.Message);
            throw new ReportGenerationException($"Print job {printJobId} failed: {ex.Message}", ex);
        }
    }

    private async Task<bool> PrintInternalAsync(
        RdlcPrinterDto rdlcPrinter,
        RdlcPrintRequestDto printRequest,
        string printJobId,
        CancellationToken cancellationToken)
    {
        // Resource Management: Properly track all disposable resources
        List<Stream>? streams = null;
        PrintDocument? printDoc = null;
        List<DataTable>? dataTablesToDispose = null;

        try
        {
            streams = new List<Stream>();
            using var lr = new LocalReport();

            // Validate and construct secure report path - always enforce security
            string reportPath;
            if (!_options.EnableStrictPathValidation)
            {
                // CWE-22 Prevention: Path traversal attacks must be prevented in all environments
                _logger.LogCritical("SECURITY ERROR: Strict path validation is disabled. This is a critical security vulnerability (CWE-22).");
                throw new SecurityException(
                    "Strict path validation must be enabled to prevent path traversal attacks (CWE-22). " +
                    "Set ReportOptions.EnableStrictPathValidation = true in your configuration.");
            }

            // Ensure ReportsDirectory and FileName are provided
            if (string.IsNullOrWhiteSpace(rdlcPrinter.ReportsDirectory))
            {
                throw new InvalidReportPathException("ReportsDirectory cannot be null or empty");
            }
            if (string.IsNullOrWhiteSpace(rdlcPrinter.FileName))
            {
                throw new InvalidReportPathException("FileName cannot be null or empty");
            }

            try
            {
                var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rdlcPrinter.ReportsDirectory);
                reportPath = PathSecurityValidator.ValidateAndResolvePath(baseDirectory, rdlcPrinter.FileName);

                // Validate file extension
                if (_options.AllowedReportExtensions.Length > 0)
                {
                    PathSecurityValidator.ValidateFileExtension(reportPath, _options.AllowedReportExtensions);
                }
            }
            catch (SecurityException ex)
            {
                throw InvalidReportPathException.FromSecurityException(ex, rdlcPrinter.FileName);
            }

            await LoadReportDefinitionAsync(lr, reportPath, cancellationToken);

            // Add data sources
            // Resource Management: Track DataTables for proper disposal
            dataTablesToDispose = new List<DataTable>();
            if (printRequest.DataSources != null)
            {
                foreach (var item in printRequest.DataSources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dataTable = DataConverters.JsonToDataTable(
                        JsonExtensions.SerializeOptimized(item.Value));
                    dataTablesToDispose.Add(dataTable);
                    lr.DataSources.Add(new ReportDataSource(item.Key, dataTable));
                }
            }

            // Set parameters
            await SetReportParametersAsync(lr, rdlcPrinter, printRequest, cancellationToken);

            // Render to streams
            lr.Render(rdlcPrinter.Format, rdlcPrinter.DeviceInfo, (_, _, _, _, _) =>
            {
                var stream = new MemoryStream();
                streams.Add(stream);
                return stream;
            }, out _);

            foreach (var stream in streams)
            {
                stream.Position = 0;
            }

            if (streams == null || streams.Count == 0)
            {
                throw new ReportGenerationException("No streams generated for printing");
            }

            // Validate printer
            printDoc = new PrintDocument();
            printDoc.PrinterSettings.PrinterName = rdlcPrinter.PrinterName;

            if (!printDoc.PrinterSettings.IsValid)
            {
                _logger.LogWarning(
                    "Print job {PrintJobId}: Printer '{PrinterName}' is not valid or not found",
                    printJobId, rdlcPrinter.PrinterName);
                return false;
            }

            // Configure printer settings for thermal/matricial printers
            ConfigurePrinterSettings(printDoc, rdlcPrinter, printJobId);

            // Set up print handler
            var currentPage = 0;
            var hasErrors = false;

            printDoc.PrintPage += (sender, e) =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (currentPage < streams.Count && e.Graphics != null)
                    {
                        using var pageImage = new Metafile(streams[currentPage]);

                        // Optimize for thermal printers - use bounds properly
                        var bounds = e.MarginBounds;
                        if (e.PageSettings.Landscape)
                        {
                            bounds = new System.Drawing.Rectangle(
                                e.PageBounds.X,
                                e.PageBounds.Y,
                                e.PageBounds.Height,
                                e.PageBounds.Width);
                        }

                        e.Graphics.DrawImage(pageImage, bounds);
                        currentPage++;
                        e.HasMorePages = currentPage < streams.Count;
                    }
                    else
                    {
                        e.HasMorePages = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Print job {PrintJobId}: Error rendering page {PageNumber}",
                        printJobId, currentPage + 1);
                    hasErrors = true;
                    e.HasMorePages = false;
                }
            };

            printDoc.EndPrint += (sender, e) =>
            {
                try
                {
                    // Note: Stream cleanup is handled in the finally block
                    // This event just logs completion
                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogInformation(
                            "Print job {PrintJobId}: Printed {PageCount} pages",
                            printJobId, currentPage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Print job {PrintJobId}: Error during EndPrint event",
                        printJobId);
                }
            };

            printDoc.QueryPageSettings += (sender, e) =>
            {
                // Allow dynamic page settings for thermal printers
                if (e.PageSettings != null && rdlcPrinter.Format == "Image")
                {
                    // Thermal printers often need specific settings
                    e.PageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
                }
            };

            // Execute print
            printDoc.Print();

            return !hasErrors;
        }
        finally
        {
            // Resource Management: Ensure all resources are properly disposed
            // Dispose streams
            if (streams != null)
            {
                foreach (var stream in streams)
                {
                    try
                    {
                        stream?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing stream in print job {PrintJobId}", printJobId);
                    }
                }
                streams.Clear();
            }

            // Dispose DataTables
            if (dataTablesToDispose != null)
            {
                foreach (var dt in dataTablesToDispose)
                {
                    try
                    {
                        dt?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing DataTable in print job {PrintJobId}", printJobId);
                    }
                }
                dataTablesToDispose.Clear();
            }

            // Dispose PrintDocument
            try
            {
                printDoc?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing PrintDocument in print job {PrintJobId}", printJobId);
            }
        }
    }

    private void ConfigurePrinterSettings(PrintDocument printDoc, RdlcPrinterDto rdlcPrinter, string printJobId)
    {
        // Set printer-specific settings
        // Resource Management: PrinterSettings is managed by PrintDocument, no separate disposal needed
        printDoc.PrinterSettings.PrinterName = rdlcPrinter.PrinterName;
        printDoc.PrinterSettings.Copies = rdlcPrinter.Copies;

        // Optimize for thermal/matricial printers
        if (IsLikelyThermalPrinter(rdlcPrinter.PrinterName))
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation(
                    "Print job {PrintJobId}: Detected thermal printer, applying optimizations",
                    printJobId);
            }

            // Thermal printers typically work best with specific settings
            printDoc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);

            // Some thermal printers require raw mode
            printDoc.PrinterSettings.DefaultPageSettings.Color = false;
        }
        else if (IsLikelyMatricialPrinter(rdlcPrinter.PrinterName))
        {
            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation(
                    "Print job {PrintJobId}: Detected matricial printer, applying optimizations",
                    printJobId);
            }

            // Matricial printers work best with draft quality
            printDoc.DefaultPageSettings.Color = false;
        }
    }

    private static bool IsLikelyThermalPrinter(string printerName)
    {
        if (string.IsNullOrEmpty(printerName)) return false;

        var thermalKeywords = new[] { "thermal", "zebra", "tsc", "datamax", "sato", "godex", "pos", "receipt", "bixolon", "citizen" };
        var nameLower = printerName.ToLowerInvariant();
        return thermalKeywords.Any(keyword => nameLower.Contains(keyword));
    }

    private static bool IsLikelyMatricialPrinter(string printerName)
    {
        if (string.IsNullOrEmpty(printerName)) return false;

        var matricialKeywords = new[] { "epson", "lx", "fx", "dfx", "matrix", "dot", "impact", "okidata", "oki" };
        var nameLower = printerName.ToLowerInvariant();
        return matricialKeywords.Any(keyword => nameLower.Contains(keyword));
    }

    private async Task LoadReportDefinitionAsync(LocalReport lr, string reportPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(reportPath))
        {
            throw new ReportNotFoundException($"Report file not found: {reportPath}");
        }

        if (_options.EnableReportDefinitionCache)
        {
            var reportStream = await _cache.GetOrAddAsync(reportPath, async (key) =>
            {
                using var fileStream = new FileStream(key, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            });

            lr.LoadReportDefinition(reportStream);
        }
        else
        {
            using var fileStream = new FileStream(reportPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            lr.LoadReportDefinition(fileStream);
        }
    }

    private async Task SetReportParametersAsync(
        LocalReport lr,
        RdlcPrinterDto rdlcPrinter,
        RdlcPrintRequestDto printRequest,
        CancellationToken cancellationToken)
    {
        var reportParams = lr.GetParameters();

        if (reportParams.Count > 0 && printRequest.ReportParams != null)
        {
            foreach (var item in printRequest.ReportParams)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item.Key == "logo")
                {
                    var logoPath = await FindLogoPathAsync(rdlcPrinter, cancellationToken);
                    if (!string.IsNullOrEmpty(logoPath))
                    {
                        var logoBytes = await File.ReadAllBytesAsync(logoPath, cancellationToken);
                        lr.SetParameters(new ReportParameter(item.Key, Convert.ToBase64String(logoBytes)));
                    }
                }
                else
                {
                    lr.SetParameters(new ReportParameter(item.Key, item.Value));
                }
            }
        }
    }

    private async Task<string?> FindLogoPathAsync(RdlcPrinterDto rdlcPrinter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rdlcPrinter.LogoDirectory) || !Directory.Exists(rdlcPrinter.LogoDirectory))
        {
            return null;
        }

        var fileEntries = await Task.Run(
            () => Directory.GetFiles(rdlcPrinter.LogoDirectory),
            cancellationToken);

        foreach (var entry in fileEntries)
        {
            var fileName = Path.GetFileNameWithoutExtension(entry);
            if (fileName == rdlcPrinter.LogoName)
            {
                return entry;
            }
        }

        return null;
    }
}

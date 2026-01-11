using Acontplus.Utilities.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security;
using System.Web;
using static System.Enum;

namespace Acontplus.Reports.Services
{
    /// <summary>
    /// High-performance RDLC report generation service with async support, caching, and concurrency control
    /// </summary>
    public class RdlcReportService : IRdlcReportService, IDisposable
    {
        private readonly ILogger<RdlcReportService> _logger;
        private readonly ReportOptions _options;
        private readonly ConcurrentDictionary<string, Lazy<MemoryStream>> _reportCache = new();
        private readonly SemaphoreSlim _concurrencyLimiter;
        private bool _disposed;

        public RdlcReportService(ILogger<RdlcReportService> logger, IOptions<ReportOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new ReportOptions();
            _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentReports, _options.MaxConcurrentReports);
        }

        /// <inheritdoc />
        public async Task<ReportResponse> GetReportAsync(DataSet parameters, DataSet data, bool externalDirectory = false, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            ReportPropsDto? reportProps = null;

            try
            {
                // Enforce concurrency limit
                await _concurrencyLimiter.WaitAsync(cancellationToken);

                try
                {
                    reportProps = DataTableMapper.MapDataRowToModel<ReportPropsDto>(
                        parameters.Tables["ReportProps"]?.Rows[0]
                        ?? throw new ArgumentException("ReportProps table is required in parameters DataSet"));

                    if (_options.EnableDetailedLogging)
                    {
                        _logger.LogInformation("Starting report: {Path}, Format: {Format}",
                            reportProps.ReportPath, reportProps.ReportFormat);
                    }

                    using var lr = new LocalReport();
                    var reportPath = GetReportPath(reportProps, externalDirectory);

                    var reportDefinitionStream = _reportCache.GetOrAdd(reportPath, path =>
                    {
                        return new Lazy<MemoryStream>(() => RdlcHelpers.LoadReportDefinition(path));
                    }).Value;

                    reportDefinitionStream.Seek(0, SeekOrigin.Begin);
                    lr.LoadReportDefinition(reportDefinitionStream);

                    await Task.Run(() => AddDataSources(lr, parameters, data), cancellationToken);
                    await Task.Run(() => AddReportParameters(lr, parameters, data), cancellationToken);

                    // Generate with timeout
                    var reportTask = Task.Run(() =>
                        lr.Render(reportProps.ReportFormat, null, out _, out _, out _, out _, out _),
                        cancellationToken);

                    var timeoutTask = Task.Delay(
                        TimeSpan.FromSeconds(_options.ReportGenerationTimeoutSeconds),
                        cancellationToken);

                    if (await Task.WhenAny(reportTask, timeoutTask) == timeoutTask)
                    {
                        throw new ReportTimeoutException(reportPath, _options.ReportGenerationTimeoutSeconds);
                    }

                    var fileReport = await reportTask;

                    if (fileReport.Length > _options.MaxReportSizeBytes)
                    {
                        throw new ReportSizeExceededException(fileReport.Length, _options.MaxReportSizeBytes);
                    }

                    var response = BuildReportResponse(reportProps, fileReport);

                    stopwatch.Stop();
                    _logger.LogInformation("Report generated: {Path}, {Size} bytes, {Duration}ms",
                        reportProps.ReportPath, fileReport.Length, stopwatch.ElapsedMilliseconds);

                    return response;
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Report cancelled: {Path}", reportProps?.ReportPath ?? "Unknown");
                throw;
            }
            catch (ReportGenerationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {Path}", reportProps?.ReportPath ?? "Unknown");
                throw new ReportGenerationException("Report generation failed",
                    reportProps?.ReportPath ?? "Unknown",
                    reportProps?.ReportFormat ?? "Unknown", ex);
            }
        }

        /// <inheritdoc />
        [Obsolete("Use GetReportAsync for better performance and scalability")]
        public ReportResponse GetReport(DataSet parameters, DataSet data, bool externalDirectory = false)
        {
            return GetReportAsync(parameters, data, externalDirectory).GetAwaiter().GetResult();
        }

        private string GetReportPath(ReportPropsDto reportProps, bool offline)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(reportProps.ReportPath))
            {
                throw new InvalidReportPathException("Report path cannot be null or empty");
            }

            string baseDirectory;
            string requestedPath;

            if (offline && !string.IsNullOrEmpty(_options.ExternalDirectory))
            {
                baseDirectory = _options.ExternalDirectory;
                requestedPath = reportProps.ReportPath;
            }
            else
            {
                baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), _options.MainDirectory);
                requestedPath = reportProps.ReportPath;
            }

            // Always apply strict path validation - this is a security requirement
            string resolvedPath;
            if (!_options.EnableStrictPathValidation)
            {
                // CWE-22 Prevention: Path traversal attacks must be prevented in all environments
                _logger.LogCritical("SECURITY ERROR: Strict path validation is disabled. This is a critical security vulnerability (CWE-22).");
                throw new SecurityException(
                    "Strict path validation must be enabled to prevent path traversal attacks (CWE-22). " +
                    "Set ReportOptions.EnableStrictPathValidation = true in your configuration.");
            }

            try
            {
                resolvedPath = PathSecurityValidator.ValidateAndResolvePath(baseDirectory, requestedPath);

                // Validate file extension
                if (_options.AllowedReportExtensions.Length > 0)
                {
                    PathSecurityValidator.ValidateFileExtension(resolvedPath, _options.AllowedReportExtensions);
                }
            }
            catch (SecurityException ex)
            {
                throw InvalidReportPathException.FromSecurityException(ex, reportProps.ReportPath);
            }

            // Log the resolved path for security auditing
            if (_options.EnableDetailedLogging)
            {
                _logger.LogInformation("Resolved report path: {ResolvedPath} (requested: {RequestedPath})",
                    resolvedPath, reportProps.ReportPath);
            }

            return resolvedPath;
        }

        private void AddDataSources(LocalReport lr, DataSet parameters, DataSet data)
        {
            if (parameters.Tables.Contains("DataSources"))
            {
                var dataSources = parameters.Tables["DataSources"];
                if (dataSources != null)
                {
                    foreach (DataRow row in dataSources.Rows)
                    {
                        lr.DataSources.Add(new ReportDataSource(row["dataSource"].ToString(),
                            data.Tables[row.Field<int>("position")]));
                    }
                }
            }
            else
            {
                var dataSourceNames = lr.GetDataSourceNames();
                foreach (var dataSourceName in dataSourceNames)
                {
                    lr.DataSources.Add(new ReportDataSource(dataSourceName, data.Tables[dataSourceName]));
                }
            }
        }

        private void AddReportParameters(LocalReport lr, DataSet parameters, DataSet data)
        {
            var firstDataSource = data.Tables[0];
            if (firstDataSource.Columns.Contains("codigoAutorizacion"))
            {
                var barcodeConfig = new BarcodeConfig
                {
                    Text = firstDataSource.Rows[0].Field<string>("codigoAutorizacion") ?? string.Empty
                };
                var byteBarcode = BarcodeGen.Create(barcodeConfig);
                lr.SetParameters(new ReportParameter("barcode", Convert.ToBase64String(
                    byteBarcode,
                    0,
                    byteBarcode.Length)));
                lr.SetParameters(new ReportParameter("mimeTypeBarcode", "image/png"));
            }

            if (parameters.Tables.Contains("ReportParams") && parameters.Tables["ReportParams"]!.Rows.Count > 0)
            {
                foreach (DataRow item in parameters.Tables["ReportParams"]!.Rows)
                {
                    var paramValue = "";
                    if (Convert.ToBoolean(item["isPicture"]))
                    {
                        paramValue = item.Field<bool>("isCompressed")
                            ? FileExtensions.GetBase64FromByte(
                                CompressionUtils.DecompressGZip((byte[])item["paramValue"]))
                            : FileExtensions.GetBase64FromByte((byte[])item["paramValue"]);
                        lr.SetParameters(new ReportParameter(item["paramName"].ToString(), paramValue));
                    }
                    else
                    {
                        var paramBytes = item.Field<byte[]>("paramValue");
                        if (paramBytes != null)
                        {
                            paramValue = Encoding.UTF8.GetString(paramBytes);
                        }
                    }

                    lr.SetParameters(new ReportParameter(item["paramName"].ToString(), paramValue));
                }
            }
        }

        private ReportResponse BuildReportResponse(ReportPropsDto reportProps, byte[] fileReport)
        {
            TryParse(reportProps.ReportFormat.ToUpper(), out FileFormats.FileContentType fc);
            TryParse(reportProps.ReportFormat.ToUpper(), out FileFormats.FileExtension fe);

            var response = new ReportResponse
            {
                FileContents = fileReport,
                ContentType = fc.DisplayName(),
                FileDownloadName = HttpUtility.UrlEncode(reportProps.ReportName + fe.DisplayName(), Encoding.UTF8)
            };
            return response;
        }

        public async Task<ReportResponse> GetErrorAsync()
        {
            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
            var filePath = Path.Combine(baseDirectory, "NotFound.pdf");

            // Ensure the file exists and is safe to access
            if (!File.Exists(filePath))
            {
                _logger.LogError("Error report file not found at: {FilePath}", filePath);

                // Return a minimal error response
                return new ReportResponse
                {
                    FileContents = Encoding.UTF8.GetBytes("Report not found"),
                    ContentType = "text/plain",
                    FileDownloadName = "Error.txt"
                };
            }

            var fileContents = await File.ReadAllBytesAsync(filePath);
            return new ReportResponse
            {
                FileContents = fileContents,
                ContentType = "application/pdf",
                FileDownloadName = "Not Found.pdf"
            };
        }

        private void CleanupCache()
        {
            foreach (var lazyMemoryStream in _reportCache.Values)
            {
                if (lazyMemoryStream.IsValueCreated)
                {
                    lazyMemoryStream.Value.Dispose();
                }
            }

            _reportCache.Clear();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                CleanupCache();
                _concurrencyLimiter?.Dispose();
            }

            _disposed = true;
        }

        ~RdlcReportService()
        {
            Dispose(false);
        }
    }
}

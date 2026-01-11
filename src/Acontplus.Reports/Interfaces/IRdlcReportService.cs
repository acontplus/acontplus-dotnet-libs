namespace Acontplus.Reports.Interfaces
{
    /// <summary>
    /// Service for generating RDLC reports with support for multiple formats and data sources
    /// </summary>
    public interface IRdlcReportService
    {
        /// <summary>
        /// Generates a report asynchronously from the provided parameters and data
        /// </summary>
        /// <param name="parameters">DataSet containing report parameters, data sources configuration, and report properties</param>
        /// <param name="data">DataSet containing the actual data for the report</param>
        /// <param name="externalDirectory">Whether to use external directory for report files</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <returns>ReportResponse containing the generated report content</returns>
        Task<ReportResponse> GetReportAsync(DataSet parameters, DataSet data, bool externalDirectory = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a default error report (typically a "Not Found" PDF)
        /// </summary>
        /// <returns>ReportResponse containing the error report</returns>
        Task<ReportResponse> GetErrorAsync();

        /// <summary>
        /// Legacy synchronous method - prefer using GetReportAsync for better performance
        /// </summary>
        [Obsolete("Use GetReportAsync for better performance and scalability")]
        ReportResponse GetReport(DataSet parameters, DataSet data, bool externalDirectory = false);
    }
}

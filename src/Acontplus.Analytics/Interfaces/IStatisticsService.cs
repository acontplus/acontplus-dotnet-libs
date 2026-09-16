namespace Acontplus.Analytics.Interfaces;

/// <summary>
/// Generic statistics service interface - Implement this in any service for consistent analytics
/// </summary>
/// <typeparam name="TDashboard">Dashboard statistics DTO (can extend BaseDashboardStatsDto)</typeparam>
/// <typeparam name="TRealTime">Real-time statistics DTO (can extend BaseRealTimeStatsDto)</typeparam>
/// <typeparam name="TAggregated">Aggregated statistics DTO (can extend BaseAggregatedStatsDto)</typeparam>
/// <typeparam name="TTrend">Trend analysis DTO (can extend BaseTrendDto)</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters",
    Justification = "Generic statistics abstraction requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2436",
    Justification = "Generic statistics abstraction requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
public interface IStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>
    where TDashboard : class
    where TRealTime : class
    where TAggregated : class
    where TTrend : class
{
    /// <summary>
    /// Get comprehensive dashboard statistics for a date range
    /// </summary>
    /// <param name="filterRequest">Filter parameters (startDate, endDate, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics or error</returns>
    Task<Result<TDashboard, DomainError>> GetDashboardStatsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time operational statistics
    /// </summary>
    /// <param name="filterRequest">Optional filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time statistics or error</returns>
    Task<Result<TRealTime, DomainError>> GetRealTimeStatsAsync(
        FilterRequest? filterRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated statistics grouped by time period
    /// </summary>
    /// <param name="filterRequest">Filter parameters (startDate, endDate, groupBy, metricType, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of aggregated statistics or error</returns>
    Task<Result<List<TAggregated>, DomainError>> GetAggregatedStatsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get trend analysis (metric specified in FilterRequest.Filters JSON as 'Metric' property)
    /// </summary>
    /// <param name="filterRequest">Filter parameters including Metric in Filters JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trend data points or error</returns>
    Task<Result<List<TTrend>, DomainError>> GetTrendsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default);
}

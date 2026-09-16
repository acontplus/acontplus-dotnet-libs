using Acontplus.Core.Abstractions.Persistence;

namespace Acontplus.Analytics.Services;

/// <summary>
/// Generic statistics service implementation with dynamic stored procedure support
/// </summary>
/// <typeparam name="TDashboard">Dashboard statistics DTO type</typeparam>
/// <typeparam name="TRealTime">Real-time statistics DTO type</typeparam>
/// <typeparam name="TAggregated">Aggregated statistics DTO type</typeparam>
/// <typeparam name="TTrend">Trend analysis DTO type</typeparam>
/// <param name="adoRepository">ADO repository for database operations</param>
/// <param name="dashboardSpName">Stored procedure name for dashboard stats</param>
/// <param name="realTimeSpName">Stored procedure name for real-time stats</param>
/// <param name="aggregatedSpName">Stored procedure name for aggregated stats</param>
/// <param name="trendsSpName">Stored procedure name for trend stats</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters",
    Justification = "Generic statistics abstraction requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2436",
    Justification = "Generic statistics abstraction requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
public class StatisticsService<TDashboard, TRealTime, TAggregated, TTrend>(
    IAdoRepository adoRepository,
    string dashboardSpName,
    string realTimeSpName,
    string aggregatedSpName,
    string trendsSpName)
    : Interfaces.IStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>
    where TDashboard : class
    where TRealTime : class
    where TAggregated : class
    where TTrend : class
{
    private readonly IAdoRepository _adoRepository = adoRepository ?? throw new ArgumentNullException(nameof(adoRepository));
    private readonly string _dashboardSpName = dashboardSpName ?? throw new ArgumentNullException(nameof(dashboardSpName));
    private readonly string _realTimeSpName = realTimeSpName ?? throw new ArgumentNullException(nameof(realTimeSpName));
    private readonly string _aggregatedSpName = aggregatedSpName ?? throw new ArgumentNullException(nameof(aggregatedSpName));
    private readonly string _trendsSpName = trendsSpName ?? throw new ArgumentNullException(nameof(trendsSpName));

    /// <summary>
    /// Get comprehensive dashboard statistics for a date range
    /// </summary>
    public async Task<Result<TDashboard, DomainError>> GetDashboardStatsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandOptions = new CommandOptionsDto { CommandTimeout = 30 };

            var results = await _adoRepository.GetFilteredFromStoredProcedureAsync<TDashboard>(
                _dashboardSpName,
                filterRequest,
                commandOptions,
                cancellationToken
            );

            var result = results.FirstOrDefault();

            if (result == null)
            {
                var error = DomainError.NotFound("DASHBOARD_STATS_NOT_FOUND",
                    "No dashboard statistics were found for the specified date range.");
                return Result<TDashboard, DomainError>.Failure(error);
            }

            return Result<TDashboard, DomainError>.Success(result);
        }
        catch (Exception ex)
        {
            var error = DomainError.Internal("DASHBOARD_STATS_ERROR",
                $"An error occurred while retrieving dashboard statistics. {ex.Message}");
            return Result<TDashboard, DomainError>.Failure(error);
        }
    }

    /// <summary>
    /// Get real-time operational statistics
    /// </summary>
    public async Task<Result<TRealTime, DomainError>> GetRealTimeStatsAsync(
        FilterRequest? filterRequest = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            filterRequest ??= new FilterRequest();
            var commandOptions = new CommandOptionsDto { CommandTimeout = 30 };

            var results = await _adoRepository.GetFilteredFromStoredProcedureAsync<TRealTime>(
                _realTimeSpName,
                filterRequest,
                commandOptions,
                cancellationToken
            );

            var result = results.FirstOrDefault();

            if (result == null)
            {
                var error = DomainError.NotFound("REALTIME_STATS_NOT_FOUND",
                    "No real-time statistics were found.");
                return Result<TRealTime, DomainError>.Failure(error);
            }

            return Result<TRealTime, DomainError>.Success(result);
        }
        catch (Exception ex)
        {
            var error = DomainError.Internal("REALTIME_STATS_ERROR",
                $"An error occurred while retrieving real-time statistics. {ex.Message}");
            return Result<TRealTime, DomainError>.Failure(error);
        }
    }

    /// <summary>
    /// Get aggregated statistics grouped by time period
    /// </summary>
    public async Task<Result<List<TAggregated>, DomainError>> GetAggregatedStatsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandOptions = new CommandOptionsDto { CommandTimeout = 30 };

            var result = await _adoRepository.GetFilteredFromStoredProcedureAsync<TAggregated>(
                _aggregatedSpName,
                filterRequest,
                commandOptions,
                cancellationToken
            );

            if (result == null || result.Count == 0)
            {
                var error = DomainError.NotFound("AGGREGATED_STATS_NOT_FOUND",
                    "No aggregated statistics were found for the specified parameters.");
                return Result<List<TAggregated>, DomainError>.Failure(error);
            }

            return Result<List<TAggregated>, DomainError>.Success(result);
        }
        catch (Exception ex)
        {
            var error = DomainError.Internal("AGGREGATED_STATS_ERROR",
                $"An error occurred while retrieving aggregated statistics. {ex.Message}");
            return Result<List<TAggregated>, DomainError>.Failure(error);
        }
    }

    /// <summary>
    /// Get trend analysis data
    /// </summary>
    public async Task<Result<List<TTrend>, DomainError>> GetTrendsAsync(
        FilterRequest filterRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var commandOptions = new CommandOptionsDto { CommandTimeout = 30 };

            var result = await _adoRepository.GetFilteredFromStoredProcedureAsync<TTrend>(
                _trendsSpName,
                filterRequest,
                commandOptions,
                cancellationToken
            );

            if (result == null || result.Count == 0)
            {
                var error = DomainError.NotFound("TRENDS_NOT_FOUND",
                    "No trend data found for the specified parameters.");
                return Result<List<TTrend>, DomainError>.Failure(error);
            }

            return Result<List<TTrend>, DomainError>.Success(result);
        }
        catch (Exception ex)
        {
            var error = DomainError.Internal("TRENDS_ERROR",
                $"An error occurred while retrieving trend data. {ex.Message}");
            return Result<List<TTrend>, DomainError>.Failure(error);
        }
    }
}

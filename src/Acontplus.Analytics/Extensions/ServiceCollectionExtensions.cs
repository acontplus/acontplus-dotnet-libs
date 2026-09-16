using Acontplus.Core.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.Analytics.Extensions;

/// <summary>
/// Extension methods for registering analytics services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a statistics service with custom stored procedure names
    /// </summary>
    /// <typeparam name="TDashboard">Dashboard DTO type</typeparam>
    /// <typeparam name="TRealTime">Real-time DTO type</typeparam>
    /// <typeparam name="TAggregated">Aggregated DTO type</typeparam>
    /// <typeparam name="TTrend">Trend DTO type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="dashboardSpName">Dashboard stored procedure name</param>
    /// <param name="realTimeSpName">Real-time stored procedure name</param>
    /// <param name="aggregatedSpName">Aggregated stored procedure name</param>
    /// <param name="trendsSpName">Trends stored procedure name</param>
    /// <returns>Service collection for chaining</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters",
        Justification = "Generic statistics registration requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2436",
        Justification = "Generic statistics registration requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
    public static IServiceCollection AddStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>(
        this IServiceCollection services,
        string dashboardSpName,
        string realTimeSpName,
        string aggregatedSpName,
        string trendsSpName)
        where TDashboard : class
        where TRealTime : class
        where TAggregated : class
        where TTrend : class
    {
        services.AddScoped<Interfaces.IStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>>(sp =>
        {
            var adoRepository = sp.GetRequiredService<IAdoRepository>();
            return new Services.StatisticsService<TDashboard, TRealTime, TAggregated, TTrend>(
                adoRepository,
                dashboardSpName,
                realTimeSpName,
                aggregatedSpName,
                trendsSpName);
        });

        return services;
    }

    /// <summary>
    /// Adds a statistics service with module-based stored procedure naming convention
    /// </summary>
    /// <typeparam name="TDashboard">Dashboard DTO type</typeparam>
    /// <typeparam name="TRealTime">Real-time DTO type</typeparam>
    /// <typeparam name="TAggregated">Aggregated DTO type</typeparam>
    /// <typeparam name="TTrend">Trend DTO type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="moduleName">Module name prefix (e.g., "Restaurant.Statistics")</param>
    /// <returns>Service collection for chaining</returns>
    /// <example>
    /// services.AddStatisticsService&lt;DashboardDto, RealTimeDto, AggregatedDto, TrendDto&gt;("Restaurant.Statistics");
    /// // Creates SPs: Restaurant.StatisticsGetDashboard, Restaurant.StatisticsGetRealTime, etc.
    /// </example>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2436:Types and methods should not have too many generic parameters",
        Justification = "Generic statistics registration requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "S2436",
        Justification = "Generic statistics registration requires distinct type parameters for dashboard, real-time, aggregated, and trend models.")]
    public static IServiceCollection AddStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>(
        this IServiceCollection services,
        string moduleName)
        where TDashboard : class
        where TRealTime : class
        where TAggregated : class
        where TTrend : class
    {
        return services.AddStatisticsService<TDashboard, TRealTime, TAggregated, TTrend>(
            $"{moduleName}GetDashboard",
            $"{moduleName}GetRealTime",
            $"{moduleName}GetAggregated",
            $"{moduleName}GetTrends");
    }
}

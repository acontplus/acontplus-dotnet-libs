using System.Diagnostics.CodeAnalysis;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Repositories;
using Dapper;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.SqlServer.Repositories;

/// <summary>
/// Dapper-based repository implementation for SQL Server.
/// Provides simplified data access with automatic object mapping and SQL Server optimizations.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2077",
    Justification = "Dynamic SQL for pagination; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
[SuppressMessage("Security", "S2077:FormattingSQLQueriesIsSecuritySensitive",
    Justification = "Dynamic SQL for pagination; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
public class DapperRepository(
    IConfiguration configuration,
    ILogger<DapperRepository> logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : BaseDapperRepository(configuration, logger, resilienceOptions)
{
    /// <inheritdoc />
    protected override string ProviderName => "SQL Server";

    /// <inheritdoc />
    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    /// <inheritdoc />
    protected override bool IsTransientException(Exception exception) =>
        exception is SqlException sqlEx && SqlServerExceptionHandler.IsTransientException(sqlEx);

    /// <inheritdoc />
    protected override string SanitizeIdentifier(string identifier) => $"[{identifier}]";

    /// <inheritdoc />
    protected override string BuildPagedSql(string sql, string orderByClause, PaginationRequest pagination, DynamicParameters parameters)
    {
        parameters.Add("@PageIndex", pagination.PageIndex);
        parameters.Add("@PageSize", pagination.PageSize);

        return $@"{sql}
{orderByClause}
OFFSET @PageIndex * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY";
    }

    /// <inheritdoc />
    protected override (CommandDefinition Command, Func<List<T>, int> TotalCountExtractor) CreatePagedStoredProcedureCommand<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout,
        CancellationToken cancellationToken)
    {
        var dynamicParams = BuildDynamicParameters(pagination);
        dynamicParams.Add("@PageIndex", pagination.PageIndex);
        dynamicParams.Add("@PageSize", pagination.PageSize);
        dynamicParams.Add("@SortColumn", pagination.SortBy);
        dynamicParams.Add("@SortDirection", pagination.SortDirection.ToString());
        dynamicParams.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = CreateCommandDefinition(
            storedProcedureName,
            dynamicParams,
            commandTimeout,
            CommandType.StoredProcedure,
            cancellationToken);

        return (command, _ => dynamicParams.Get<int>("@TotalCount"));
    }

    /// <inheritdoc />
    protected override CommandDefinition CreateFilteredStoredProcedureCommand(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout,
        CancellationToken cancellationToken)
    {
        var dynamicParams = BuildDynamicParameters(filter);
        dynamicParams.Add("@SortColumn", filter.SortBy);
        dynamicParams.Add("@SortDirection", filter.SortDirection.ToString());

        return CreateCommandDefinition(
            storedProcedureName,
            dynamicParams,
            commandTimeout,
            CommandType.StoredProcedure,
            cancellationToken);
    }
}

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
    public override async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(pagination);
                dynamicParams.Add("@PageIndex", pagination.PageIndex);
                dynamicParams.Add("@PageSize", pagination.PageSize);
                dynamicParams.Add("@SortColumn", pagination.SortBy);
                dynamicParams.Add("@SortDirection", pagination.SortDirection.ToString());
                dynamicParams.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var commandDefinition = new CommandDefinition(
                    storedProcedureName,
                    dynamicParams,
                    CurrentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    CommandType.StoredProcedure,
                    cancellationToken: ct);

                var items = (await connection.QueryAsync<T>(commandDefinition)).ToList();
                var totalCount = dynamicParams.Get<int>("@TotalCount");

                return new PagedResult<T>(
                    items,
                    totalCount,
                    pagination.PageIndex,
                    pagination.PageSize);
            }, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(filter);
                dynamicParams.Add("@SortColumn", filter.SortBy);
                dynamicParams.Add("@SortDirection", filter.SortDirection.ToString());

                var commandDefinition = new CommandDefinition(
                    storedProcedureName,
                    dynamicParams,
                    CurrentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    CommandType.StoredProcedure,
                    cancellationToken: ct);

                return await connection.QueryAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }
}

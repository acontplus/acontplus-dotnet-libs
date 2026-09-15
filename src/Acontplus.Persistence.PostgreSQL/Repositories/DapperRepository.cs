using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Repositories;
using Dapper;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.PostgreSQL.Repositories;

/// <summary>
/// Dapper-based repository implementation for PostgreSQL.
/// Provides simplified data access with automatic object mapping and PostgreSQL optimizations.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2077",
    Justification = "Dynamic SQL for pagination and sanitized procedure names; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
[SuppressMessage("Security", "S2077:FormattingSQLQueriesIsSecuritySensitive",
    Justification = "Dynamic SQL for pagination and sanitized procedure names; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
public partial class DapperRepository(
    IConfiguration configuration,
    ILogger<DapperRepository> logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : BaseDapperRepository(configuration, logger, resilienceOptions)
{
    /// <inheritdoc />
    protected override string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    protected override DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    /// <inheritdoc />
    protected override bool IsTransientException(Exception exception) =>
        exception is NpgsqlException npgEx && PostgresExceptionHandler.IsTransientException(npgEx);

    /// <inheritdoc />
    protected override string SanitizeIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc />
    protected override string DefaultOrderByClause => "ORDER BY 1";

    /// <inheritdoc />
    protected override string GenerateCountQuery(string sql) => $"SELECT COUNT(*) FROM ({sql}) AS count_query";

    /// <inheritdoc />
    protected override string BuildPagedSql(string sql, string orderByClause, PaginationRequest pagination, DynamicParameters parameters)
    {
        var offset = pagination.PageIndex * pagination.PageSize;
        parameters.Add("@Offset", offset);
        parameters.Add("@Limit", pagination.PageSize);

        return $@"{sql}
{orderByClause}
LIMIT @Limit OFFSET @Offset";
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
                dynamicParams.Add("@p_page_index", pagination.PageIndex);
                dynamicParams.Add("@p_page_size", pagination.PageSize);
                dynamicParams.Add("@p_sort_column", pagination.SortBy);
                dynamicParams.Add("@p_sort_direction", pagination.SortDirection.ToString().ToLowerInvariant());

                var sql = $"SELECT * FROM {SanitizeFunctionName(storedProcedureName)}(@p_page_index, @p_page_size, @p_sort_column, @p_sort_direction)";

                var commandDefinition = new CommandDefinition(
                    sql,
                    dynamicParams,
                    CurrentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    cancellationToken: ct);

                var items = (await connection.QueryAsync<T>(commandDefinition)).ToList();
                var totalCount = items.Count;

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
                dynamicParams.Add("@p_sort_column", filter.SortBy);
                dynamicParams.Add("@p_sort_direction", filter.SortDirection.ToString().ToLowerInvariant());

                var sql = $"SELECT * FROM {SanitizeFunctionName(storedProcedureName)}(@p_sort_column, @p_sort_direction)";

                var commandDefinition = new CommandDefinition(
                    sql,
                    dynamicParams,
                    CurrentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    cancellationToken: ct);

                return await connection.QueryAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_.]*$")]
    private static partial Regex SafeFunctionNameRegex();

    private static string SanitizeFunctionName(string functionName)
    {
        if (!SafeFunctionNameRegex().IsMatch(functionName))
        {
            throw new ArgumentException($"Invalid function name: {functionName}");
        }
        return functionName;
    }
}

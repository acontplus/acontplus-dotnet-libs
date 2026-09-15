using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Resilience;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.Common.Repositories;

/// <summary>
/// Base Dapper repository implementation providing common query execution,
/// connection lifecycle management, transaction support, and resilience policies.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2077",
    Justification = "Dynamic SQL for pagination; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
[SuppressMessage("Security", "S2077:FormattingSQLQueriesIsSecuritySensitive",
    Justification = "Dynamic SQL for pagination; all filter values and pagination arguments are bound via Dapper DynamicParameters.")]
public abstract partial class BaseDapperRepository(
    IConfiguration configuration,
    ILogger logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : IDapperRepository
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<string, string> _connectionStrings = new();
    private readonly ILogger _logger = logger;
    private readonly PersistenceResilienceOptions _resilienceOptions = resilienceOptions?.Value ?? new PersistenceResilienceOptions();
    private DbConnection? _currentConnection;
    private DbTransaction? _currentTransaction;

    /// <summary>
    /// Gets the active transaction, if any.
    /// </summary>
    protected DbTransaction? CurrentTransaction => _currentTransaction;
    private AsyncRetryPolicy? _retryPolicy;

    /// <summary>
    /// Gets the provider-specific database name for logging diagnostics.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Creates a provider-specific <see cref="DbConnection"/>.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A new <see cref="DbConnection"/> instance.</returns>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Determines whether the specified exception represents a transient database error.
    /// </summary>
    /// <param name="exception">The caught exception.</param>
    /// <returns><c>true</c> if transient; otherwise, <c>false</c>.</returns>
    protected abstract bool IsTransientException(Exception exception);

    /// <summary>
    /// Sanitizes an identifier with provider-specific quotes or brackets.
    /// </summary>
    /// <param name="identifier">The identifier name.</param>
    /// <returns>The quoted identifier.</returns>
    protected abstract string SanitizeIdentifier(string identifier);

    /// <summary>
    /// Builds the provider-specific paginated SQL query.
    /// </summary>
    /// <param name="sql">The base SQL query.</param>
    /// <param name="orderByClause">The resolved ORDER BY clause.</param>
    /// <param name="pagination">The pagination request.</param>
    /// <param name="parameters">Dynamic parameters collection to add pagination arguments to.</param>
    /// <returns>The combined paginated SQL string.</returns>
    protected abstract string BuildPagedSql(string sql, string orderByClause, PaginationRequest pagination, DynamicParameters parameters);

    /// <summary>
    /// Default ORDER BY clause when none is explicitly requested.
    /// </summary>
    protected virtual string DefaultOrderByClause => "ORDER BY (SELECT NULL)";

    /// <summary>
    /// Lazy-loaded retry policy based on configuration.
    /// </summary>
    protected AsyncRetryPolicy RetryPolicy =>
        _retryPolicy ??= PersistenceResilienceHelper.CreateRetryPolicy(_resilienceOptions, IsTransientException, _logger, ProviderName, "Dapper");

    /// <summary>
    /// Gets the default command timeout in seconds from configuration.
    /// </summary>
    protected int DefaultTimeout => _resilienceOptions.Timeout.DefaultCommandTimeoutSeconds;

    #region Query Methods

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                return await connection.QueryAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                return await connection.QueryFirstOrDefaultAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                return await connection.QuerySingleOrDefaultAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    #endregion

    #region Execute Methods

    /// <inheritdoc />
    public virtual async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                return await connection.ExecuteAsync(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                return await connection.ExecuteScalarAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    #endregion

    #region Multiple Result Sets

    /// <inheritdoc />
    public virtual Task<(IEnumerable<T1> First, IEnumerable<T2> Second)> QueryMultipleAsync<T1, T2>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        ExecuteQueryMultipleAsync(
            sql,
            parameters,
            commandTimeout,
            commandType,
            async multi =>
            {
                var first = await multi.ReadAsync<T1>();
                var second = await multi.ReadAsync<T2>();
                return (first, second);
            },
            cancellationToken);

    /// <inheritdoc />
    public virtual Task<(IEnumerable<T1> First, IEnumerable<T2> Second, IEnumerable<T3> Third)> QueryMultipleAsync<T1, T2, T3>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default) =>
        ExecuteQueryMultipleAsync(
            sql,
            parameters,
            commandTimeout,
            commandType,
            async multi =>
            {
                var first = await multi.ReadAsync<T1>();
                var second = await multi.ReadAsync<T2>();
                var third = await multi.ReadAsync<T3>();
                return (first, second, third);
            },
            cancellationToken);

    private async Task<TReturn> ExecuteQueryMultipleAsync<TReturn>(
        string sql,
        object? parameters,
        int? commandTimeout,
        CommandType? commandType,
        Func<SqlMapper.GridReader, Task<TReturn>> readFunc,
        CancellationToken cancellationToken)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: ct);

                using var multi = await connection.QueryMultipleAsync(commandDefinition);
                return await readFunc(multi);
            }, ct);
        }, cancellationToken);
    }

    #endregion

    #region Paged Query Methods

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var countSql = GenerateCountQuery(sql);
        return await GetPagedAsync<T>(sql, countSql, pagination, commandTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        string countSql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(pagination);
                var orderByClause = BuildOrderByClause(pagination);
                var pagedSql = BuildPagedSql(sql, orderByClause, pagination, dynamicParams);
                var combinedSql = $"{countSql}; {pagedSql}";

                var commandDefinition = new CommandDefinition(
                    combinedSql,
                    dynamicParams,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    cancellationToken: ct);

                using var multi = await connection.QueryMultipleAsync(commandDefinition);
                var totalCount = await multi.ReadSingleAsync<int>();
                var items = (await multi.ReadAsync<T>()).ToList();

                return new PagedResult<T>(
                    items,
                    totalCount,
                    pagination.PageIndex,
                    pagination.PageSize);
            }, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates provider-specific command definition and total count extractor for stored procedure paging.
    /// </summary>
    protected abstract (CommandDefinition Command, Func<List<T>, int> TotalCountExtractor) CreatePagedStoredProcedureCommand<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var (commandDefinition, totalCountExtractor) = CreatePagedStoredProcedureCommand<T>(
                    storedProcedureName, pagination, commandTimeout, ct);

                var items = (await connection.QueryAsync<T>(commandDefinition)).ToList();
                var totalCount = totalCountExtractor(items);

                return new PagedResult<T>(
                    items,
                    totalCount,
                    pagination.PageIndex,
                    pagination.PageSize);
            }, ct);
        }, cancellationToken);
    }

    #endregion

    #region Filtered Query Methods

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetFilteredAsync<T>(
        string sql,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(filter);
                var orderByClause = BuildOrderByClause(filter);
                var filteredSql = $"{sql} {orderByClause}";

                var commandDefinition = new CommandDefinition(
                    filteredSql,
                    dynamicParams,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    cancellationToken: ct);

                return await connection.QueryAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates provider-specific command definition for filtered stored procedure execution.
    /// </summary>
    protected abstract CommandDefinition CreateFilteredStoredProcedureCommand(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = CreateFilteredStoredProcedureCommand(
                    storedProcedureName, filter, commandTimeout, ct);

                return await connection.QueryAsync<T>(commandDefinition);
            }, ct);
        }, cancellationToken);
    }

    #endregion

    #region Transaction Support

    /// <inheritdoc />
    public void SetTransaction(DbTransaction transaction) => _currentTransaction = transaction;

    /// <inheritdoc />
    public void SetConnection(DbConnection connection) => _currentConnection = connection;

    /// <inheritdoc />
    public void ClearTransaction()
    {
        _currentTransaction = null;
        _currentConnection = null;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a connection for the operation. Returns a tuple with the connection and whether it should be disposed.
    /// </summary>
    protected async Task<(DbConnection Connection, bool ShouldDispose)> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_currentConnection != null)
        {
            if (_currentConnection.State != ConnectionState.Open)
            {
                await _currentConnection.OpenAsync(cancellationToken);
            }
            return (_currentConnection, ShouldDispose: false);
        }

        var connectionString = GetConnectionString();
        var connection = CreateConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return (connection, ShouldDispose: true);
    }

    /// <summary>
    /// Executes an operation with proper connection lifecycle management.
    /// </summary>
    protected async Task<TResult> ExecuteWithConnectionAsync<TResult>(
        Func<DbConnection, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        var (connection, shouldDispose) = await GetConnectionAsync(cancellationToken);
        try
        {
            return await operation(connection);
        }
        finally
        {
            if (shouldDispose)
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Retrieves the connection string from configuration by name.
    /// </summary>
    protected string GetConnectionString(string connectionName = "DefaultConnection")
    {
        return _connectionStrings.GetOrAdd(connectionName, name =>
        {
            var connectionString = _configuration.GetConnectionString(name);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string '{name}' not found in configuration.");
            }
            return connectionString;
        });
    }

    /// <summary>
    /// Builds dynamic parameters from pagination filter dictionary and search term.
    /// </summary>
    protected static DynamicParameters BuildDynamicParameters(PaginationRequest pagination)
    {
        var parameters = new DynamicParameters();

        if (pagination.Filters != null)
        {
            foreach (var filter in pagination.Filters)
            {
                parameters.Add($"@{filter.Key}", filter.Value);
            }
        }

        if (!string.IsNullOrEmpty(pagination.SearchTerm))
        {
            parameters.Add("@SearchTerm", $"%{pagination.SearchTerm}%");
        }

        return parameters;
    }

    /// <summary>
    /// Builds dynamic parameters from filter request dictionary and search term.
    /// </summary>
    protected static DynamicParameters BuildDynamicParameters(FilterRequest filter)
    {
        var parameters = new DynamicParameters();

        if (filter.Filters != null)
        {
            foreach (var f in filter.Filters)
            {
                parameters.Add($"@{f.Key}", f.Value);
            }
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            parameters.Add("@SearchTerm", $"%{filter.SearchTerm}%");
        }

        return parameters;
    }

    /// <summary>
    /// Builds the ORDER BY clause for pagination requests.
    /// </summary>
    protected virtual string BuildOrderByClause(PaginationRequest pagination)
    {
        if (string.IsNullOrEmpty(pagination.SortBy))
        {
            return DefaultOrderByClause;
        }

        var direction = pagination.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sanitizedColumn = SanitizeColumnName(pagination.SortBy);
        return $"ORDER BY {sanitizedColumn} {direction}";
    }

    /// <summary>
    /// Builds the ORDER BY clause for filter requests.
    /// </summary>
    protected virtual string BuildOrderByClause(FilterRequest filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
        {
            return string.Empty;
        }

        var direction = filter.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sanitizedColumn = SanitizeColumnName(filter.SortBy);
        return $"ORDER BY {sanitizedColumn} {direction}";
    }

    /// <summary>
    /// Generates a count query wrapping the original SQL query.
    /// </summary>
    protected virtual string GenerateCountQuery(string sql) => $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex SafeColumnNameRegex();

    /// <summary>
    /// Sanitizes the specified column name against injection.
    /// </summary>
    protected virtual string SanitizeColumnName(string columnName)
    {
        if (!SafeColumnNameRegex().IsMatch(columnName))
        {
            throw new ArgumentException($"Invalid column name: {columnName}");
        }
        return SanitizeIdentifier(columnName);
    }

    #endregion
}

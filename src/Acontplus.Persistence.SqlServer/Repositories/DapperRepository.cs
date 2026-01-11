using Acontplus.Core.Enums;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.SqlServer.Mapping;
using Dapper;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Acontplus.Persistence.SqlServer.Repositories;

/// <summary>
/// Dapper-based repository implementation for SQL Server.
/// Provides simplified data access with automatic object mapping.
/// </summary>
/// <remarks>
/// This implementation uses Dapper for lightweight ORM functionality with:
/// <list type="bullet">
/// <item><description>Automatic type mapping</description></item>
/// <item><description>Retry policies with exponential backoff</description></item>
/// <item><description>Transaction support via Unit of Work</description></item>
/// <item><description>SQL Server-specific optimizations</description></item>
/// </list>
/// </remarks>
public partial class DapperRepository : IDapperRepository
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, string> _connectionStrings = new();
    private readonly ILogger<DapperRepository> _logger;
    private readonly PersistenceResilienceOptions _resilienceOptions;
    private DbConnection? _currentConnection;
    private DbTransaction? _currentTransaction;
    private AsyncRetryPolicy? _retryPolicy;

    /// <summary>
    /// Lazy-loaded retry policy based on configuration.
    /// </summary>
    private AsyncRetryPolicy RetryPolicy
    {
        get
        {
            if (_retryPolicy != null)
                return _retryPolicy;

            if (!_resilienceOptions.RetryPolicy.Enabled)
            {
                _retryPolicy = Policy
                    .Handle<SqlException>(ex => false)
                    .RetryAsync(0);
                return _retryPolicy;
            }

            var maxRetries = _resilienceOptions.RetryPolicy.MaxRetries;
            var baseDelay = TimeSpan.FromSeconds(_resilienceOptions.RetryPolicy.BaseDelaySeconds);
            var maxDelay = TimeSpan.FromSeconds(_resilienceOptions.RetryPolicy.MaxDelaySeconds);
            var exponentialBackoff = _resilienceOptions.RetryPolicy.ExponentialBackoff;

            _retryPolicy = Policy
                .Handle<SqlException>(SqlServerExceptionHandler.IsTransientException)
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    maxRetries,
                    retryAttempt =>
                    {
                        if (exponentialBackoff)
                        {
                            var calculatedDelay = TimeSpan.FromSeconds(
                                _resilienceOptions.RetryPolicy.BaseDelaySeconds * Math.Pow(2, retryAttempt - 1));
                            return calculatedDelay > maxDelay ? maxDelay : calculatedDelay;
                        }
                        return baseDelay;
                    },
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "[Dapper Repository] Retry {RetryCount}/{MaxRetries} after {Delay}ms for SQL Server operation",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalMilliseconds);
                    });

            return _retryPolicy;
        }
    }

    /// <summary>
    /// Gets the default command timeout from configuration.
    /// </summary>
    private int DefaultTimeout => _resilienceOptions.Timeout.DefaultCommandTimeoutSeconds;

    /// <summary>
    /// Constructor for DapperRepository with resilience configuration.
    /// </summary>
    public DapperRepository(
        IConfiguration configuration,
        ILogger<DapperRepository> logger,
        IOptions<PersistenceResilienceOptions> resilienceOptions)
    {
        _configuration = configuration;
        _logger = logger;
        _resilienceOptions = resilienceOptions?.Value ?? new PersistenceResilienceOptions();
    }

    #region Query Methods

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                return await connection.QueryAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                return await connection.QueryFirstOrDefaultAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                return await connection.QuerySingleOrDefaultAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    #endregion

    #region Execute Methods

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteAsync(commandDefinition);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                return await connection.ExecuteScalarAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    #endregion

    #region Multiple Result Sets

    /// <inheritdoc />
    public async Task<(IEnumerable<T1> First, IEnumerable<T2> Second)> QueryMultipleAsync<T1, T2>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                using var multi = await connection.QueryMultipleAsync(commandDefinition);
                var first = await multi.ReadAsync<T1>();
                var second = await multi.ReadAsync<T2>();
                return (first, second);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<T1> First, IEnumerable<T2> Second, IEnumerable<T3> Third)> QueryMultipleAsync<T1, T2, T3>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var commandDefinition = new CommandDefinition(
                    sql,
                    parameters,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    commandType,
                    cancellationToken: cancellationToken);

                using var multi = await connection.QueryMultipleAsync(commandDefinition);
                var first = await multi.ReadAsync<T1>();
                var second = await multi.ReadAsync<T2>();
                var third = await multi.ReadAsync<T3>();
                return (first, second, third);
            }, cancellationToken);
        });
    }

    #endregion

    #region Paged Query Methods

    /// <inheritdoc />
    public async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        var countSql = GenerateCountQuery(sql);
        return await GetPagedAsync<T>(sql, countSql, pagination, commandTimeout, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        string countSql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(pagination);
                dynamicParams.Add("@PageIndex", pagination.PageIndex);
                dynamicParams.Add("@PageSize", pagination.PageSize);

                var orderByClause = BuildOrderByClause(pagination);

                // SQL Server OFFSET-FETCH pagination
                var pagedSql = $@"{sql}
{orderByClause}
OFFSET @PageIndex * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY";

                var combinedSql = $"{countSql}; {pagedSql}";

                var commandDefinition = new CommandDefinition(
                    combinedSql,
                    dynamicParams,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    cancellationToken: cancellationToken);

                using var multi = await connection.QueryMultipleAsync(commandDefinition);
                var totalCount = await multi.ReadSingleAsync<int>();
                var items = (await multi.ReadAsync<T>()).ToList();

                return new PagedResult<T>(
                    items,
                    totalCount,
                    pagination.PageIndex,
                    pagination.PageSize);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
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
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    CommandType.StoredProcedure,
                    cancellationToken: cancellationToken);

                var items = (await connection.QueryAsync<T>(commandDefinition)).ToList();
                var totalCount = dynamicParams.Get<int>("@TotalCount");

                return new PagedResult<T>(
                    items,
                    totalCount,
                    pagination.PageIndex,
                    pagination.PageSize);
            }, cancellationToken);
        });
    }

    #endregion

    #region Filtered Query Methods

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetFilteredAsync<T>(
        string sql,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
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
                    cancellationToken: cancellationToken);

                return await connection.QueryAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        return await RetryPolicy.ExecuteAsync(async () =>
        {
            return await ExecuteWithConnectionAsync(async connection =>
            {
                var dynamicParams = BuildDynamicParameters(filter);
                dynamicParams.Add("@SortColumn", filter.SortBy);
                dynamicParams.Add("@SortDirection", filter.SortDirection.ToString());

                var commandDefinition = new CommandDefinition(
                    storedProcedureName,
                    dynamicParams,
                    _currentTransaction,
                    commandTimeout ?? DefaultTimeout,
                    CommandType.StoredProcedure,
                    cancellationToken: cancellationToken);

                return await connection.QueryAsync<T>(commandDefinition);
            }, cancellationToken);
        });
    }

    #endregion

    #region Transaction Support

    /// <inheritdoc />
    public void SetTransaction(DbTransaction transaction)
    {
        _currentTransaction = transaction;
    }

    /// <inheritdoc />
    public void SetConnection(DbConnection connection)
    {
        _currentConnection = connection;
    }

    /// <inheritdoc />
    public void ClearTransaction()
    {
        _currentTransaction = null;
        _currentConnection = null;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets a connection for the operation. Returns a tuple with the connection and whether it should be disposed.
    /// When using a shared connection (from UnitOfWork), it should NOT be disposed by the caller.
    /// When creating a new connection, it SHOULD be disposed by the caller.
    /// </summary>
    private async Task<(DbConnection Connection, bool ShouldDispose)> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_currentConnection != null)
        {
            if (_currentConnection.State != ConnectionState.Open)
            {
                await _currentConnection.OpenAsync(cancellationToken);
            }
            // Shared connection from UnitOfWork - DO NOT dispose
            return (_currentConnection, ShouldDispose: false);
        }

        var connectionString = GetConnectionString();
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        // New connection created - caller should dispose
        return (connection, ShouldDispose: true);
    }

    /// <summary>
    /// Executes an operation with proper connection lifecycle management.
    /// Automatically handles connection disposal for owned connections only.
    /// </summary>
    private async Task<TResult> ExecuteWithConnectionAsync<TResult>(
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

    private string GetConnectionString(string connectionName = "DefaultConnection")
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

    private static DynamicParameters BuildDynamicParameters(PaginationRequest pagination)
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

    private static DynamicParameters BuildDynamicParameters(FilterRequest filter)
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

    private static string BuildOrderByClause(PaginationRequest pagination)
    {
        if (string.IsNullOrEmpty(pagination.SortBy))
        {
            return "ORDER BY (SELECT NULL)";
        }

        var direction = pagination.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sanitizedColumn = SanitizeColumnName(pagination.SortBy);
        return $"ORDER BY {sanitizedColumn} {direction}";
    }

    private static string BuildOrderByClause(FilterRequest filter)
    {
        if (string.IsNullOrEmpty(filter.SortBy))
        {
            return string.Empty;
        }

        var direction = filter.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
        var sanitizedColumn = SanitizeColumnName(filter.SortBy);
        return $"ORDER BY {sanitizedColumn} {direction}";
    }

    private static string GenerateCountQuery(string sql)
    {
        return $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex SafeColumnNameRegex();

    private static string SanitizeColumnName(string columnName)
    {
        if (!SafeColumnNameRegex().IsMatch(columnName))
        {
            throw new ArgumentException($"Invalid column name: {columnName}");
        }
        return $"[{columnName}]";
    }

    #endregion
}

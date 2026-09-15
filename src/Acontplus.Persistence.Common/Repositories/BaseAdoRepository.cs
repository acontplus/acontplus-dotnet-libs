using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Mapping;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.Common.Repositories;

/// <summary>
/// Provides provider-agnostic ADO.NET data access operations with retry policy,
/// connection management, and transaction sharing.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2139",
    Justification = "Repository methods log detailed diagnostic context before rethrowing database exceptions.")]
public abstract class BaseAdoRepository(
    IConfiguration configuration,
    ILogger logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : IAdoRepository
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<string, string> _connectionStrings = new();
    private readonly ILogger _logger = logger;
    private readonly PersistenceResilienceOptions _resilienceOptions = resilienceOptions?.Value ?? new PersistenceResilienceOptions();
    private DbConnection? _currentConnection;
    private DbTransaction? _currentTransaction;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);
    private AsyncRetryPolicy? _retryPolicy;

    /// <summary>
    /// Gets the current active transaction, if any.
    /// </summary>
    protected DbTransaction? CurrentTransaction => _currentTransaction;

    /// <summary>
    /// Gets the current active connection, if any.
    /// </summary>
    protected DbConnection? CurrentConnection => _currentConnection;

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;

    /// <summary>
    /// Gets the provider-specific database name for diagnostic logging.
    /// </summary>
    protected abstract string ProviderName { get; }

    /// <summary>
    /// Creates a provider-specific <see cref="DbConnection"/>.
    /// </summary>
    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>
    /// Determines whether the specified exception represents a transient database error.
    /// </summary>
    protected abstract bool IsTransientException(Exception exception);

    /// <summary>
    /// Determines whether the specified exception is a provider-specific database error.
    /// </summary>
    protected abstract bool IsProviderException(Exception exception);

    /// <summary>
    /// Handles provider-specific database exceptions (maps and rethrows domain exceptions).
    /// </summary>
    protected abstract void HandleProviderException(Exception exception, string operationName);

    /// <summary>
    /// Creates a provider-specific <see cref="DbDataAdapter"/>.
    /// </summary>
    protected abstract DbDataAdapter CreateDataAdapter(DbCommand command);

    /// <summary>
    /// Quotes or escapes an identifier for the specific database engine.
    /// </summary>
    protected abstract string SanitizeIdentifier(string identifier);

    /// <summary>
    /// Builds the provider-specific paginated SQL query (OFFSET/FETCH or LIMIT/OFFSET).
    /// </summary>
    protected abstract string BuildPagedSql(string sql, PaginationRequest pagination);

    /// <summary>
    /// Adds output parameter for table names if requested.
    /// </summary>
    protected abstract void AddTableNamesOutputParameter(DbCommand command, CommandOptionsDto options);

    /// <summary>
    /// Lazy-loaded retry policy based on configuration.
    /// </summary>
    protected AsyncRetryPolicy RetryPolicy
    {
        get
        {
            if (_retryPolicy != null)
                return _retryPolicy;

            if (!_resilienceOptions.RetryPolicy.Enabled)
            {
                _retryPolicy = Policy
                    .Handle<Exception>(_ => false)
                    .RetryAsync(0);
                return _retryPolicy;
            }

            var maxRetries = _resilienceOptions.RetryPolicy.MaxRetries;
            var baseDelay = TimeSpan.FromSeconds(_resilienceOptions.RetryPolicy.BaseDelaySeconds);
            var maxDelay = TimeSpan.FromSeconds(_resilienceOptions.RetryPolicy.MaxDelaySeconds);
            var exponentialBackoff = _resilienceOptions.RetryPolicy.ExponentialBackoff;

            _retryPolicy = Policy
                .Handle<Exception>(IsTransientException)
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
                    (exception, timeSpan, retryCount, _) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "[ADO Repository] Retry {RetryCount}/{MaxRetries} after {Delay}ms for {ProviderName} operation",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalMilliseconds,
                            ProviderName);
                    });

            return _retryPolicy;
        }
    }

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

    #region Query Methods

    /// <inheritdoc />
    public virtual async Task<List<T>> QueryAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            await using var cmd = CreateCommand(connection, sql, parameters, options);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ToListAsync<T>(ct);
        }, nameof(QueryAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<DataSet> GetDataSetAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= [];
        options ??= new CommandOptionsDto();

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            await using var cmd = CreateCommand(connection, sql, parameters, options);

            if (options.WithTableNames)
            {
                AddTableNamesOutputParameter(cmd, options);
            }

            var ds = new DataSet();
            using var adapter = CreateDataAdapter(cmd);
            await Task.Run(() => adapter.Fill(ds), ct);

            if (options.WithTableNames)
            {
                await DataTableNameMapper.ProcessTableNames(cmd, ds, ct);
            }

            return ds;
        }, nameof(GetDataSetAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> ExecuteNonQueryAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            await using var cmd = CreateCommand(connection, sql, parameters, options);
            return await cmd.ExecuteNonQueryAsync(ct);
        }, nameof(ExecuteNonQueryAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var results = await QueryAsync<T>(sql, parameters, options, cancellationToken);
        return results.SingleOrDefault();
    }

    /// <inheritdoc />
    public virtual async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var results = await QueryAsync<T>(sql, parameters, options, cancellationToken);
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public virtual async IAsyncEnumerable<T> QueryAsyncEnumerable<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        parameters ??= [];
        DbConnection? connectionToClose = null;
        DbCommand? cmd = null;
        DbDataReader? reader = null;

        try
        {
            var connection = await GetOpenConnectionAsync(null, cancellationToken);
            if (_currentConnection == null)
            {
                connectionToClose = connection;
            }

            cmd = CreateCommand(connection, sql, parameters, options);
            reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var columnMap = BuildColumnPropertyMap<T>(reader);

            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var instance = Activator.CreateInstance<T>();
                PopulateInstanceFromReader(instance, reader, columnMap);
                yield return instance;
            }
        }
        finally
        {
            if (reader != null)
            {
                await reader.DisposeAsync();
            }

            if (cmd != null)
            {
                await cmd.DisposeAsync();
            }

            await CloseConnectionSafelyAsync(connectionToClose);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TScalar?> ExecuteScalarAsync<TScalar>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            await using var cmd = CreateCommand(connection, sql, parameters, options);
            var result = await cmd.ExecuteScalarAsync(ct);

            if (result == null || result == DBNull.Value)
            {
                return default;
            }

            var targetType = typeof(TScalar);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            return (TScalar)Convert.ChangeType(result, underlyingType);
        }, nameof(ExecuteScalarAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync<object>(sql, parameters, options, cancellationToken);
        return result != null && result != DBNull.Value;
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync<object>(sql, parameters, options, cancellationToken);
        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt32(result);
    }

    /// <inheritdoc />
    public virtual async Task<long> LongCountAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync<object>(sql, parameters, options, cancellationToken);
        if (result == null || result == DBNull.Value) return 0;
        return Convert.ToInt64(result);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var countSql = GenerateCountSql(sql);
        return await GetPagedAsync<T>(sql, countSql, pagination, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        string countSql,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pagination);
        options ??= new CommandOptionsDto();
        var parameters = BuildFilterParameters(pagination, options);

        var totalCount = await CountAsync(countSql, parameters, options, cancellationToken);

        var pagedSql = BuildPagedSql(sql, pagination);
        parameters["@__Offset"] = pagination.Skip;
        parameters["@__Fetch"] = pagination.Take;
        parameters["@__Limit"] = pagination.Take;
        parameters["@Offset"] = pagination.Skip;
        parameters["@Fetch"] = pagination.Take;
        parameters["@Limit"] = pagination.Take;

        var items = await QueryAsync<T>(pagedSql, parameters, options, cancellationToken);
        var metadata = BuildPaginationMetadata(pagination);

        return new PagedResult<T>(
            items,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount,
            metadata);
    }

    /// <inheritdoc />
    public abstract Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<List<T>> GetFilteredAsync<T>(
        string sql,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CommandOptionsDto();
        var parameters = BuildFilterParameters(filter, options);
        var filteredSql = BuildFilteredSql(sql, filter);

        return await QueryAsync<T>(filteredSql, parameters, options, cancellationToken);
    }

    /// <inheritdoc />
    public abstract Task<List<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<DataSet> GetFilteredDataSetAsync(
        string sql,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CommandOptionsDto();
        var parameters = BuildFilterParameters(filter, options);
        var filteredSql = BuildFilteredSql(sql, filter);

        return await GetDataSetAsync(filteredSql, parameters, options, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<List<T>>> QueryMultipleAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= [];

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            await using var cmd = CreateCommand(connection, sql, parameters, options);
            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var results = new List<List<T>>();
            do
            {
                var tableResults = await reader.ToListAsync<T>(ct);
                results.Add(tableResults);
            } while (await reader.NextResultAsync(ct));

            return results;
        }, nameof(QueryMultipleAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> ExecuteBatchNonQueryAsync(
        List<string> sqlCommands,
        List<Dictionary<string, object>>? batchParameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        if (sqlCommands == null || sqlCommands.Count == 0)
        {
            return 0;
        }

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            var totalAffected = 0;
            for (var i = 0; i < sqlCommands.Count; i++)
            {
                var sql = sqlCommands[i];
                var parameters = batchParameters != null && i < batchParameters.Count
                    ? batchParameters[i]
                    : [];

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                totalAffected += await cmd.ExecuteNonQueryAsync(ct);
            }

            return totalAffected;
        }, nameof(ExecuteBatchNonQueryAsync), cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> ExecuteBatchNonQueryAsync(
        IEnumerable<(string Sql, Dictionary<string, object>? Parameters)> commands,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var commandList = commands.ToList();
        if (commandList.Count == 0)
        {
            return 0;
        }

        return await RetryPolicy.ExecuteAsync(
            ct => ExecuteBatchWithTransactionAsync(commandList, options, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> BulkInsertAsync<T>(
        IEnumerable<T> data,
        string tableName,
        Dictionary<string, string>? columnMappings = null,
        int batchSize = 10000,
        CancellationToken cancellationToken = default)
    {
        var dataTable = ConvertToDataTable(data, columnMappings);
        return await BulkInsertAsync(dataTable, tableName, columnMappings, batchSize, cancellationToken);
    }

    /// <inheritdoc />
    public abstract Task<int> BulkInsertAsync(
        DataTable dataTable,
        string tableName,
        Dictionary<string, string>? columnMappings = null,
        int batchSize = 10000,
        CancellationToken cancellationToken = default);

    #endregion

    #region Helper Methods

    /// <summary>
    /// Executes an operation using a database connection, handling connection lifecycle and provider exceptions.
    /// </summary>
    protected async Task<TResult> ExecuteWithConnectionAsync<TResult>(
        Func<DbConnection, CancellationToken, Task<TResult>> operation,
        string operationName,
        CancellationToken cancellationToken)
    {
        return await RetryPolicy.ExecuteAsync(async (ct) =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, ct);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                return await operation(connection, ct);
            }
            catch (Exception ex) when (IsTransientException(ex) || IsProviderException(ex))
            {
                HandleProviderException(ex, operationName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing {Operation} for {ProviderName}", operationName, ProviderName);
                throw;
            }
            finally
            {
                await CloseConnectionSafelyAsync(connectionToClose);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Creates and opens a connection, reusing the current connection if available.
    /// </summary>
    protected async Task<DbConnection> GetOpenConnectionAsync(
        string? connectionStringName,
        CancellationToken cancellationToken)
    {
        if (_currentConnection != null && _currentConnection.State == ConnectionState.Open)
        {
            return _currentConnection;
        }

        if (_currentConnection != null && _currentConnection.State != ConnectionState.Open)
        {
            await _currentConnection.OpenAsync(cancellationToken);
            return _currentConnection;
        }

        try
        {
            var connection = CreateConnection(GetConnectionString(connectionStringName ?? string.Empty));
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error creating and opening connection for '{connectionStringName}'.", ex);
        }
    }

    /// <summary>
    /// Creates and configures a <see cref="DbCommand"/>.
    /// </summary>
    protected virtual DbCommand CreateCommand(
        DbConnection connection,
        string commandText,
        Dictionary<string, object> parameters,
        CommandOptionsDto? options)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;

        options ??= new CommandOptionsDto();
        cmd.CommandTimeout = options.CommandTimeout ?? 30;
        cmd.CommandType = options.CommandType;

        if (_currentTransaction != null)
        {
            cmd.Transaction = _currentTransaction;
        }

        foreach (var parameter in parameters.Where(p => !string.IsNullOrEmpty(p.Key)))
        {
            var param = cmd.CreateParameter();
            param.ParameterName = parameter.Key.StartsWith('@') ? parameter.Key : $"@{parameter.Key}";
            param.Value = parameter.Value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    /// <summary>
    /// Safely closes and disposes the connection if not null.
    /// </summary>
    protected static async Task CloseConnectionSafelyAsync(DbConnection? connection)
    {
        if (connection != null)
        {
            await connection.CloseAsync();
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
                throw new InvalidOperationException($"Connection string '{name}' not found in configuration.");
            }
            return connectionString;
        });
    }

    /// <summary>
    /// Builds query parameters from FilterRequest using flexible strategy.
    /// </summary>
    protected static Dictionary<string, object> BuildFilterParameters(FilterRequest filter, CommandOptionsDto options)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            result["@SearchTerm"] = $"%{filter.SearchTerm}%";
            result["@__SearchTerm"] = $"%{filter.SearchTerm}%";
        }

        PopulateFilters(result, filter.Filters, options.UseJsonFilters ?? true);
        return result;
    }

    /// <summary>
    /// Builds query parameters from PaginationRequest using flexible strategy.
    /// </summary>
    protected static Dictionary<string, object> BuildFilterParameters(PaginationRequest pagination, CommandOptionsDto options)
    {
        var result = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            result["@SearchTerm"] = $"%{pagination.SearchTerm}%";
            result["@__SearchTerm"] = $"%{pagination.SearchTerm}%";
        }

        PopulateFilters(result, pagination.Filters, options.UseJsonFilters ?? true);
        return result;
    }

    /// <summary>
    /// Builds stored procedure parameters from FilterRequest.
    /// </summary>
    protected Dictionary<string, object> BuildStoredProcedureParameters(FilterRequest filter, CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            spParameters["@SortBy"] = ValidateAndSanitizeSortColumn(filter.SortBy);
            spParameters["@SortDirection"] = filter.SortDirection.ToString();
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            spParameters["@SearchTerm"] = filter.SearchTerm;
        }

        PopulateFilters(spParameters, filter.Filters, options.UseJsonFilters ?? true);
        return spParameters;
    }

    /// <summary>
    /// Builds stored procedure parameters from PaginationRequest.
    /// </summary>
    protected Dictionary<string, object> BuildStoredProcedureParameters(PaginationRequest pagination, CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>
        {
            ["@PageIndex"] = pagination.PageIndex,
            ["@PageSize"] = pagination.PageSize
        };

        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            spParameters["@SortBy"] = ValidateAndSanitizeSortColumn(pagination.SortBy);
            spParameters["@SortDirection"] = pagination.SortDirection.ToString();
        }

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            spParameters["@SearchTerm"] = pagination.SearchTerm;
        }

        PopulateFilters(spParameters, pagination.Filters, options.UseJsonFilters ?? true);
        return spParameters;
    }

    /// <summary>
    /// Populates filter parameters based on dictionary or JSON strategy.
    /// </summary>
    protected static void PopulateFilters(
        Dictionary<string, object> parameters,
        IReadOnlyDictionary<string, object>? filters,
        bool useJsonFilters)
    {
        if (filters == null || filters.Count == 0)
        {
            if (useJsonFilters)
            {
                parameters["@Filters"] = DBNull.Value;
            }
            return;
        }

        if (useJsonFilters)
        {
            parameters["@Filters"] = filters.SerializeWithCamelCaseKeys();
        }
        else
        {
            foreach (var kvp in filters)
            {
                var paramName = kvp.Key.StartsWith('@') ? kvp.Key : $"@{kvp.Key}";
                parameters[paramName] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Builds pagination metadata for PagedResult.
    /// </summary>
    protected static Dictionary<string, object> BuildPaginationMetadata(PaginationRequest pagination)
    {
        var metadata = new Dictionary<string, object>
        {
            [PaginationMetadataKeys.HasFilters] = pagination.Filters?.Any() ?? false,
            [PaginationMetadataKeys.HasSearch] = !string.IsNullOrWhiteSpace(pagination.SearchTerm),
            [PaginationMetadataKeys.SortBy] = pagination.SortBy ?? string.Empty,
            [PaginationMetadataKeys.SortDirection] = pagination.SortDirection.ToString()
        };

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            metadata[PaginationMetadataKeys.SearchTerm] = pagination.SearchTerm;
        }

        if (pagination.Filters?.Any() == true)
        {
            metadata[PaginationMetadataKeys.FilterCount] = pagination.Filters.Count;
        }

        return metadata;
    }

    /// <summary>
    /// Builds filtered SQL with ORDER BY clause based on FilterRequest.
    /// </summary>
    protected virtual string BuildFilteredSql(string sql, FilterRequest filter)
    {
        var builder = new StringBuilder(sql);

        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(filter.SortBy))
        {
            var safeSortBy = ValidateAndSanitizeSortColumn(filter.SortBy);
            var direction = filter.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
            builder.Append($" ORDER BY {SanitizeIdentifier(safeSortBy)} {direction}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Generates a count query wrapping the original SQL query.
    /// </summary>
    protected static string GenerateCountSql(string sql)
    {
        var cleanSql = Regex.Replace(
            sql,
            @"\s+ORDER\s+BY\s+[^;]+$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline,
            RegexTimeout).Trim();

        if (string.IsNullOrWhiteSpace(cleanSql))
        {
            cleanSql = sql;
        }

        return $"SELECT COUNT(*) FROM ({cleanSql}) AS CountQuery";
    }

    /// <summary>
    /// Converts an enumerable to a DataTable.
    /// </summary>
    protected static DataTable ConvertToDataTable<T>(IEnumerable<T> data, Dictionary<string, string>? columnMappings)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        foreach (var prop in properties)
        {
            var columnName = columnMappings?.ContainsKey(prop.Name) == true
                ? columnMappings[prop.Name]
                : prop.Name;

            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(columnName, columnType);
        }

        foreach (var item in data)
        {
            var row = dataTable.NewRow();
            foreach (var prop in properties)
            {
                var columnName = columnMappings?.ContainsKey(prop.Name) == true
                    ? columnMappings[prop.Name]
                    : prop.Name;

                var value = prop.GetValue(item);
                row[columnName] = value ?? DBNull.Value;
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Validates pagination parameters.
    /// </summary>
    protected static void ValidatePagination(PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        if (pagination.PageIndex < 1)
        {
            throw new ArgumentException("PageIndex must be greater than 0", nameof(pagination));
        }

        if (pagination.PageSize is < 1 or > 10000)
        {
            throw new ArgumentException("PageSize must be between 1 and 10000", nameof(pagination));
        }
    }

    /// <summary>
    /// Validates and sanitizes sort column names to prevent SQL injection.
    /// </summary>
    protected string ValidateAndSanitizeSortColumn(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        }

        columnName = columnName.Trim();

        var pattern = @"^[a-zA-Z0-9_\.]+$";
        if (!Regex.IsMatch(columnName, pattern, RegexOptions.None, RegexTimeout))
        {
            _logger.LogWarning("Potential SQL injection attempt detected in sort column: {ColumnName}", columnName);
            throw new ArgumentException(
                $"Invalid column name: {columnName}. Only alphanumeric characters, underscores, and dots are allowed.",
                nameof(columnName));
        }

        if (columnName.Length > 128)
        {
            _logger.LogWarning("Column name exceeds maximum length: {ColumnName}", columnName);
            throw new ArgumentException($"Column name exceeds maximum length of 128 characters: {columnName}", nameof(columnName));
        }

        var upperColumn = columnName.ToUpperInvariant();
        var dangerousKeywords = new[]
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE",
            "UNION", "ALL", "WHERE", "JOIN", "HAVING", "GROUP", "BY", "ORDER", "DECLARE", "CAST",
            "CONVERT", "WAITFOR", "DELAY", "SHUTDOWN", "XP_", "SP_", "--", "/*", "*/", ";", "@@"
        };

        foreach (var keyword in dangerousKeywords)
        {
            if (upperColumn.Contains(keyword, StringComparison.Ordinal))
            {
                _logger.LogWarning("Dangerous keyword '{Keyword}' detected in sort column: {ColumnName}", keyword, columnName);
                throw new ArgumentException($"Sort column contains forbidden keyword '{keyword}': {columnName}", nameof(columnName));
            }
        }

        return columnName;
    }

    private async Task<int> ExecuteBatchWithTransactionAsync(
        List<(string Sql, Dictionary<string, object>? Parameters)> commandList,
        CommandOptionsDto? options,
        CancellationToken ct)
    {
        DbConnection? connectionToClose = null;
        DbTransaction? transaction = null;
        try
        {
            var connection = await GetOpenConnectionAsync(null, ct);
            if (_currentConnection == null)
            {
                connectionToClose = connection;
            }

            if (_currentTransaction == null)
            {
                transaction = await connection.BeginTransactionAsync(ct);
            }

            var totalAffected = 0;
            foreach (var (sql, parameters) in commandList)
            {
                var cmdParams = parameters ?? [];
                await using var cmd = CreateCommand(connection, sql, cmdParams, options);
                if (transaction != null)
                {
                    cmd.Transaction = transaction;
                }

                totalAffected += await cmd.ExecuteNonQueryAsync(ct);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            return totalAffected;
        }
        catch (Exception ex) when (IsTransientException(ex) || IsProviderException(ex))
        {
            await RollbackTransactionSafelyAsync(transaction, ct);
            HandleProviderException(ex, nameof(ExecuteBatchNonQueryAsync));
            throw;
        }
        catch (Exception ex)
        {
            await RollbackTransactionSafelyAsync(transaction, ct);
            _logger.LogError(ex, "Error executing {Operation} for {ProviderName}", nameof(ExecuteBatchNonQueryAsync), ProviderName);
            throw new RepositoryException("Unexpected error executing ExecuteBatchNonQueryAsync.", ex);
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }

            await CloseConnectionSafelyAsync(connectionToClose);
        }
    }

    private static async Task RollbackTransactionSafelyAsync(DbTransaction? transaction, CancellationToken ct)
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync(ct);
        }
    }

    private static Dictionary<string, PropertyInfo> BuildColumnPropertyMap<T>(DbDataReader reader)
    {
        var type = typeof(T);
        var isRecord = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
                       && type.BaseType == typeof(object);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite || (isRecord && p.CanRead))
            .ToArray();

        var columnMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            if (string.IsNullOrEmpty(columnName))
            {
                continue;
            }

            var property = properties.FirstOrDefault(p =>
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                columnMap[columnName] = property;
            }
        }

        return columnMap;
    }

    private static void PopulateInstanceFromReader<T>(
        T instance,
        DbDataReader reader,
        Dictionary<string, PropertyInfo> columnMap)
    {
        foreach (var kvp in columnMap)
        {
            var ordinal = reader.GetOrdinal(kvp.Key);
            if (reader.IsDBNull(ordinal))
            {
                continue;
            }

            var value = reader.GetValue(ordinal);
            var property = kvp.Value;
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            try
            {
                var convertedValue = ConvertPropertyValue(value, propertyType);
                property.SetValue(instance, convertedValue);
            }
            catch
            {
                // Skip properties that fail to map
            }
        }
    }

    private static object? ConvertPropertyValue(object value, Type propertyType)
    {
        if (propertyType.IsEnum)
        {
            return Enum.ToObject(propertyType, value);
        }

        if (propertyType == typeof(Guid))
        {
            return value is string strGuid ? Guid.Parse(strGuid) : (Guid)value;
        }

        return Convert.ChangeType(value, propertyType);
    }

    #endregion
}

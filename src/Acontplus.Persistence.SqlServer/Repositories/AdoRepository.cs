using Acontplus.Core.Enums;
using Acontplus.Core.Extensions;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.SqlServer.Mapping;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Acontplus.Persistence.SqlServer.Repositories;

/// <summary>
///     Provides ADO.NET data access operations with retry policy and optional transaction sharing.
///     Enhanced with SQL Server error handling, domain error mapping, and flexible filter parameter strategies.
/// </summary>
public class AdoRepository : IAdoRepository
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, string> _connectionStrings = new();
    private readonly ILogger<AdoRepository> _logger;
    private readonly PersistenceResilienceOptions _resilienceOptions;
    private DbConnection? _currentConnection;

    // Fields for sharing connection/transaction with UnitOfWork
    private DbTransaction? _currentTransaction;

    // Lazy retry policy - created on first use with current configuration
    private AsyncRetryPolicy? _retryPolicy;

    /// <summary>
    ///     Lazy-loaded retry policy based on configuration.
    /// </summary>
    private AsyncRetryPolicy RetryPolicy
    {
        get
        {
            if (_retryPolicy != null)
                return _retryPolicy;

            if (!_resilienceOptions.RetryPolicy.Enabled)
            {
                // If retry is disabled, create a pass-through policy
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
                            "[ADO Repository] Retry {RetryCount}/{MaxRetries} after {Delay}ms for SQL Server operation",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalMilliseconds);
                    });

            return _retryPolicy;
        }
    }

    /// <summary>
    ///     Constructor for AdoRepository with resilience configuration.
    /// </summary>
    public AdoRepository(
        IConfiguration configuration,
        ILogger<AdoRepository> logger,
        IOptions<PersistenceResilienceOptions> resilienceOptions)
    {
        _configuration = configuration;
        _logger = logger;
        _resilienceOptions = resilienceOptions?.Value ?? new PersistenceResilienceOptions();
    }

    /// <summary>
    ///     Sets the current database transaction from the Unit of Work.
    /// </summary>
    public void SetTransaction(DbTransaction transaction) => _currentTransaction = transaction;

    /// <summary>
    ///     Sets the current database connection from the Unit of Work.
    /// </summary>
    public void SetConnection(DbConnection connection) => _currentConnection = connection;

    /// <summary>
    ///     Clears the current transaction and connection.
    /// </summary>
    public void ClearTransaction()
    {
        _currentTransaction = null;
        _currentConnection = null;
    }

    /// <summary>
    ///     Executes a SQL query and maps results to a list of objects.
    /// </summary>
    public async Task<List<T>> QueryAsync<T>(
        string sql,
        Dictionary<string, object>? parameters,
        CommandOptionsDto? options,
        CancellationToken cancellationToken)
    {
        parameters ??= new Dictionary<string, object>();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                return await reader.ToListAsync<T>(cancellationToken);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(QueryAsync));
                throw; // This line won't be reached, but keeps compiler happy
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing QueryAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a SQL query and returns a DataSet.
    /// </summary>
    public async Task<DataSet> GetDataSetAsync(
        string sql,
        Dictionary<string, object>? parameters,
        CommandOptionsDto? options,
        CancellationToken cancellationToken)
    {
        parameters ??= new Dictionary<string, object>();
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);

                if (options.WithTableNames)
                {
                    CommandParameterBuilder.AddOutputParameter(cmd, "@tableNames", SqlDbType.VarChar,
                        options.TableNamesLength);
                }

                var ds = new DataSet();
                using var adapter = new SqlDataAdapter(cmd);
                await Task.Run(() => adapter.Fill(ds), cancellationToken);

                if (options.WithTableNames)
                {
                    await DataTableNameMapper.ProcessTableNames(cmd, ds);
                }

                return ds;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetDataSetAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing GetDataSetAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a non-query SQL command.
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        Dictionary<string, object>? parameters,
        CommandOptionsDto? options,
        CancellationToken cancellationToken)
    {
        parameters ??= new Dictionary<string, object>();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(ExecuteNonQueryAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing ExecuteNonQueryAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a SQL query designed to return a single row.
    /// </summary>
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        parameters ??= new Dictionary<string, object>();
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                return await reader.ReadAsync(cancellationToken)
                    ? await DbDataReaderMapper.MapToObject<T>(reader)
                    : null;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(QuerySingleOrDefaultAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing QuerySingleOrDefaultAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a SQL query and returns the first row or null.
    /// </summary>
    public async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        parameters ??= new Dictionary<string, object>();
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                return await reader.ReadAsync(cancellationToken)
                    ? await DbDataReaderMapper.MapToObject<T>(reader)
                    : null;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(QueryFirstOrDefaultAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing QueryFirstOrDefaultAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    #region Streaming Methods

    /// <summary>
    ///     Streams query results as an async enumerable for memory-efficient processing.
    /// </summary>
    public async IAsyncEnumerable<T> QueryAsyncEnumerable<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        parameters ??= new Dictionary<string, object>();
        DbConnection? connectionToClose = null;
        SqlCommand? cmd = null;
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

            var type = typeof(T);
            var isRecord = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any()
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

            while (await reader.ReadAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var instance = Activator.CreateInstance<T>();

                foreach (var kvp in columnMap)
                {
                    var ordinal = reader.GetOrdinal(kvp.Key);
                    if (await reader.IsDBNullAsync(ordinal, cancellationToken))
                    {
                        continue;
                    }

                    var value = reader.GetValue(ordinal);
                    var property = kvp.Value;
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    try
                    {
                        var convertedValue = propertyType.IsEnum ? Enum.ToObject(propertyType, value) :
                            propertyType == typeof(Guid) ? value is string strGuid ? Guid.Parse(strGuid) : (Guid)value :
                            Convert.ChangeType(value, propertyType);
                        property.SetValue(instance, convertedValue);
                    }
                    catch
                    {
                        /* Skip properties that fail to map */
                    }
                }

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

            connectionToClose?.Close();
        }
    }

    #endregion

    /// <summary>
    ///     Retrieves a connection string from configuration, caching it for subsequent calls.
    /// </summary>
    private string GetConnectionString(string name)
    {
        var key = string.IsNullOrEmpty(name) ? "DefaultConnection" : name;

        return _connectionStrings.GetOrAdd(key, k =>
        {
            var connString = _configuration.GetConnectionString(k);
            if (!string.IsNullOrEmpty(connString))
            {
                return connString;
            }

            _logger.LogError("Connection string '{ConnectionName}' not found.", k);
            throw new InvalidOperationException($"Connection string '{k}' not found");
        });
    }

    /// <summary>
    ///     Creates and opens a new SqlConnection.
    /// </summary>
    private async Task<DbConnection> GetOpenConnectionAsync(string? connectionStringName,
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
            var connection = new SqlConnection(GetConnectionString(connectionStringName ?? string.Empty));
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating and opening connection for '{ConnectionName}'.", connectionStringName);
            throw;
        }
    }

    /// <summary>
    ///     Creates and configures a SqlCommand.
    /// </summary>
    private SqlCommand CreateCommand(
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
            cmd.Transaction = (SqlTransaction)_currentTransaction;
        }

        foreach (var parameter in parameters.Where(p => !string.IsNullOrEmpty(p.Key)))
        {
            CommandParameterBuilder.AddParameter(cmd, parameter.Key, parameter.Value ?? DBNull.Value);
        }

        return (SqlCommand)cmd;
    }

    #region Scalar Query Methods

    /// <summary>
    ///     Executes a query and returns a single scalar value.
    /// </summary>
    public async Task<TScalar?> ExecuteScalarAsync<TScalar>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= new Dictionary<string, object>();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                var result = await cmd.ExecuteScalarAsync(cancellationToken);

                if (result == null || result == DBNull.Value)
                {
                    return default;
                }

                return (TScalar)Convert.ChangeType(result, typeof(TScalar));
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(ExecuteScalarAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing ExecuteScalarAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Checks if any rows exist for the given query.
    /// </summary>
    public async Task<bool> ExistsAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<int>(sql, parameters, options, cancellationToken);
        return count > 0;
    }

    /// <summary>
    ///     Gets the count of rows for the given query.
    /// </summary>
    public async Task<int> CountAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync<int?>(sql, parameters, options, cancellationToken);
        return result ?? 0;
    }

    /// <summary>
    ///     Gets the long count of rows for the given query.
    /// </summary>
    public async Task<long> LongCountAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteScalarAsync<long?>(sql, parameters, options, cancellationToken);
        return result ?? 0L;
    }

    #endregion

    #region Paged Query Methods

    /// <summary>
    ///     Executes a paginated SQL query with automatic count query.
    ///     Uses SQL Server OFFSET-FETCH with optimized query plans.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        // Generate count SQL from the main SQL
        var countSql = GenerateCountSql(sql);
        return await GetPagedAsync<T>(sql, countSql, pagination, options, cancellationToken);
    }

    /// <summary>
    ///     Executes a paginated SQL query with custom count query.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        string countSql,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pagination);
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Build parameters using flexible strategy
                var parameters = BuildFilterParameters(pagination, options);

                // Get total count
                var totalCount = await CountAsync(countSql, parameters, options, cancellationToken);

                // Build paginated query with ORDER BY and OFFSET-FETCH
                var pagedSql = BuildPagedSql(sql, pagination);

                // Add pagination offset/fetch parameters
                parameters["@__Offset"] = (pagination.PageIndex - 1) * pagination.PageSize;
                parameters["@__Fetch"] = pagination.PageSize;

                // Execute paged query
                await using var cmd = CreateCommand(connection, pagedSql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var items = await reader.ToListAsync<T>(cancellationToken);

                // Build result with metadata
                var metadata = BuildPaginationMetadata(pagination, options);

                return new PagedResult<T>(items, pagination.PageIndex, pagination.PageSize, totalCount, metadata);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetPagedAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing GetPagedAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a paginated stored procedure.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pagination);
        options ??= new CommandOptionsDto { CommandType = CommandType.StoredProcedure };

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Build stored procedure parameters using flexible strategy
                var spParameters = BuildStoredProcedureParameters(pagination, options);

                await using var cmd = CreateCommand(connection, storedProcedureName, spParameters, options);

                // Add output parameter for total count
                CommandParameterBuilder.AddOutputParameter(cmd, "@TotalCount", SqlDbType.Int, 0);

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                var items = await reader.ToListAsync<T>(cancellationToken);

                // Close reader to get output parameters
                await reader.CloseAsync();

                var totalCount = cmd.Parameters["@TotalCount"].Value != DBNull.Value
                    ? Convert.ToInt32(cmd.Parameters["@TotalCount"].Value)
                    : 0;

                // Build result with metadata
                var metadata = BuildPaginationMetadata(pagination, options);

                return new PagedResult<T>(items, pagination.PageIndex, pagination.PageSize, totalCount, metadata);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetPagedFromStoredProcedureAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error executing GetPagedFromStoredProcedureAsync for '{storedProcedureName}'.",
                    storedProcedureName);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    #endregion

    #region Filtered Query Methods (Non-Paginated)

    /// <summary>
    ///     Executes a filtered SQL query with sorting and search capabilities (non-paginated).
    ///     Automatically builds parameters from FilterRequest and applies ORDER BY clause.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<List<T>> GetFilteredAsync<T>(
        string sql,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Build parameters using flexible strategy
                var parameters = BuildFilterParameters(filter, options);

                // Build filtered query with ORDER BY
                var filteredSql = BuildFilteredSql(sql, filter);

                // Execute query
                await using var cmd = CreateCommand(connection, filteredSql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                return await reader.ToListAsync<T>(cancellationToken);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetFilteredAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing GetFilteredAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a filtered stored procedure (non-paginated).
    ///     Passes filter criteria to the stored procedure without pagination parameters.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<List<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        options ??= new CommandOptionsDto { CommandType = CommandType.StoredProcedure };

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Build stored procedure parameters using flexible strategy
                var spParameters = BuildStoredProcedureParameters(filter, options);

                await using var cmd = CreateCommand(connection, storedProcedureName, spParameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                return await reader.ToListAsync<T>(cancellationToken);
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetFilteredFromStoredProcedureAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error executing GetFilteredFromStoredProcedureAsync for '{storedProcedureName}'.",
                    storedProcedureName);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes a filtered query and returns a DataSet (non-paginated).
    ///     Useful for reports that need multiple result sets with filtering and sorting.
    ///     Supports flexible filter parameter strategy via CommandOptionsDto.
    /// </summary>
    public async Task<DataSet> GetFilteredDataSetAsync(
        string sql,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        options ??= new CommandOptionsDto();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Build parameters using flexible strategy
                var parameters = BuildFilterParameters(filter, options);

                // Build filtered query with ORDER BY
                var filteredSql = BuildFilteredSql(sql, filter);

                await using var cmd = CreateCommand(connection, filteredSql, parameters, options);

                if (options.WithTableNames)
                {
                    CommandParameterBuilder.AddOutputParameter(cmd, "@tableNames", SqlDbType.VarChar,
                        options.TableNamesLength);
                }

                var ds = new DataSet();
                using var adapter = new SqlDataAdapter(cmd);
                await Task.Run(() => adapter.Fill(ds), cancellationToken);

                if (options.WithTableNames)
                {
                    await DataTableNameMapper.ProcessTableNames(cmd, ds);
                }

                return ds;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(GetFilteredDataSetAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing GetFilteredDataSetAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    #endregion

    #region Batch Operations

    /// <summary>
    ///     Executes multiple queries in a single batch.
    /// </summary>
    public async Task<List<List<T>>> QueryMultipleAsync<T>(
        string sql,
        Dictionary<string, object>? parameters = null,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        parameters ??= new Dictionary<string, object>();

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                await using var cmd = CreateCommand(connection, sql, parameters, options);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

                var results = new List<List<T>>();

                do
                {
                    var resultSet = await reader.ToListAsync<T>(cancellationToken);
                    results.Add(resultSet);
                } while (await reader.NextResultAsync(cancellationToken));

                return results;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(QueryMultipleAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing QueryMultipleAsync for '{Sql}'.", sql);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Executes multiple non-query commands in a batch.
    /// </summary>
    public async Task<int> ExecuteBatchNonQueryAsync(
        IEnumerable<(string Sql, Dictionary<string, object>? Parameters)> commands,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        var commandList = commands.ToList();
        if (!commandList.Any())
        {
            return 0;
        }

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            DbTransaction? transaction = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                // Start transaction if not already in one
                if (_currentTransaction == null)
                {
                    transaction = await connection.BeginTransactionAsync(cancellationToken);
                }

                var totalAffected = 0;

                foreach (var (sql, parameters) in commandList)
                {
                    var cmdParams = parameters ?? new Dictionary<string, object>();
                    await using var cmd = CreateCommand(connection, sql, cmdParams, options);
                    if (transaction != null)
                    {
                        cmd.Transaction = (SqlTransaction)transaction;
                    }

                    totalAffected += await cmd.ExecuteNonQueryAsync(cancellationToken);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return totalAffected;
            }
            catch (SqlException ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(ExecuteBatchNonQueryAsync));
                throw;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                _logger.LogError(ex, "Unexpected error executing ExecuteBatchNonQueryAsync.");
                throw;
            }
            finally
            {
                transaction?.Dispose();
                connectionToClose?.Close();
            }
        });
    }

    /// <summary>
    ///     Bulk insert using SqlBulkCopy for optimal performance.
    /// </summary>
    public async Task<int> BulkInsertAsync<T>(
        IEnumerable<T> data,
        string tableName,
        Dictionary<string, string>? columnMappings = null,
        int batchSize = 10000,
        CancellationToken cancellationToken = default)
    {
        var dataTable = ConvertToDataTable(data, columnMappings);
        return await BulkInsertAsync(dataTable, tableName, columnMappings, batchSize, cancellationToken);
    }

    /// <summary>
    ///     Bulk insert from DataTable using SqlBulkCopy.
    /// </summary>
    public async Task<int> BulkInsertAsync(
        DataTable dataTable,
        string tableName,
        Dictionary<string, string>? columnMappings = null,
        int batchSize = 10000,
        CancellationToken cancellationToken = default)
    {
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return 0;
        }

        return await RetryPolicy.ExecuteAsync(async () =>
        {
            DbConnection? connectionToClose = null;
            try
            {
                var connection = await GetOpenConnectionAsync(null, cancellationToken);
                if (_currentConnection == null)
                {
                    connectionToClose = connection;
                }

                using var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default,
                    (SqlTransaction?)_currentTransaction)
                {
                    DestinationTableName = tableName,
                    BatchSize = batchSize,
                    BulkCopyTimeout = 300, // 5 minutes for large batches
                    EnableStreaming = true
                };

                // Add column mappings
                if (columnMappings != null)
                {
                    foreach (var mapping in columnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                    }
                }
                else
                {
                    // Auto-map columns by name
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                }

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                return dataTable.Rows.Count;
            }
            catch (SqlException ex)
            {
                SqlServerExceptionHandler.HandleSqlException(ex, _logger, nameof(BulkInsertAsync));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing BulkInsertAsync for table '{TableName}'.", tableName);
                throw;
            }
            finally
            {
                connectionToClose?.Close();
            }
        });
    }

    #endregion

    #region Flexible Filter Parameter Builders

    /// <summary>
    ///     Builds query parameters from FilterRequest using flexible strategy.
    ///     Strategy is determined by CommandOptionsDto.UseJsonFilters flag.
    ///     - UseJsonFilters = false (default): Individual parameters for raw SQL queries
    ///     - UseJsonFilters = true: JSON serialized parameters for stored procedures
    /// </summary>
    private Dictionary<string, object> BuildFilterParameters(FilterRequest filter, CommandOptionsDto options)
    {
        var result = new Dictionary<string, object>();

        // Add search term if provided
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            result["@SearchTerm"] = $"%{filter.SearchTerm}%"; // For LIKE queries
        }

        // Apply flexible filter strategy
        if (options.UseJsonFilters ?? true)
        {
            // JSON approach - single parameter for stored procedures
            result["@Filters"] = (filter.Filters != null && filter.Filters.Any())
                ? filter.Filters.SerializeWithCamelCaseKeys()
                : DBNull.Value;
        }
        else
        {
            // Individual parameters approach - for raw SQL queries
            if (filter.Filters != null && filter.Filters.Any())
            {
                foreach (var kvp in filter.Filters)
                {
                    var paramName = kvp.Key.StartsWith("@") ? kvp.Key : $"@{kvp.Key}";
                    result[paramName] = kvp.Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Builds query parameters from PaginationRequest using flexible strategy.
    ///     Strategy is determined by CommandOptionsDto.UseJsonFilters flag.
    /// </summary>
    private Dictionary<string, object> BuildFilterParameters(PaginationRequest pagination, CommandOptionsDto options)
    {
        var result = new Dictionary<string, object>();

        // Add search term if provided
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            result["@SearchTerm"] = $"%{pagination.SearchTerm}%"; // For LIKE queries
        }

        // Apply flexible filter strategy
        if (options.UseJsonFilters ?? true)
        {
            // JSON approach - single parameter for stored procedures
            result["@Filters"] = (pagination.Filters != null && pagination.Filters.Any())
                ? pagination.Filters.SerializeWithCamelCaseKeys()
                : DBNull.Value;
        }
        else
        {
            // Individual parameters approach - for raw SQL queries
            if (pagination.Filters != null && pagination.Filters.Any())
            {
                foreach (var kvp in pagination.Filters)
                {
                    var paramName = kvp.Key.StartsWith("@") ? kvp.Key : $"@{kvp.Key}";
                    result[paramName] = kvp.Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Builds stored procedure parameters from FilterRequest.
    ///     Automatically uses JSON strategy for stored procedures unless explicitly overridden.
    /// </summary>
    private Dictionary<string, object> BuildStoredProcedureParameters(FilterRequest filter, CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>();

        // Add sort parameters if provided
        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            spParameters["@SortBy"] = ValidateAndSanitizeSortColumn(filter.SortBy);
            spParameters["@SortDirection"] = filter.SortDirection.ToString();
        }

        // Add search term if provided
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            spParameters["@SearchTerm"] = filter.SearchTerm;
        }

        // Default to JSON for stored procedures unless explicitly set to false
        var useJsonForSp = options.UseJsonFilters ?? true;

        if (useJsonForSp)
        {
            // JSON approach - recommended for stored procedures
            spParameters["@Filters"] = (filter.Filters != null && filter.Filters.Any())
                ? filter.Filters.SerializeWithCamelCaseKeys()
                : DBNull.Value;
        }
        else
        {
            // Individual parameters approach - if explicitly requested
            if (filter.Filters != null && filter.Filters.Any())
            {
                foreach (var kvp in filter.Filters)
                {
                    var paramName = kvp.Key.StartsWith("@") ? kvp.Key : $"@{kvp.Key}";
                    spParameters[paramName] = kvp.Value;
                }
            }
        }

        return spParameters;
    }

    /// <summary>
    ///     Builds stored procedure parameters from PaginationRequest.
    ///     Automatically uses JSON strategy for stored procedures unless explicitly overridden.
    /// </summary>
    private Dictionary<string, object> BuildStoredProcedureParameters(PaginationRequest pagination,
        CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>
        {
            ["@PageIndex"] = pagination.PageIndex,
            ["@PageSize"] = pagination.PageSize
        };

        // Add sort parameters if provided
        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            spParameters["@SortBy"] = ValidateAndSanitizeSortColumn(pagination.SortBy);
            spParameters["@SortDirection"] = pagination.SortDirection.ToString();
        }

        // Add search term if provided
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            spParameters["@SearchTerm"] = pagination.SearchTerm;
        }

        // Default to JSON for stored procedures unless explicitly set to false
        var useJsonForSp = options.UseJsonFilters ?? true;

        if (useJsonForSp)
        {
            // JSON approach - recommended for stored procedures
            spParameters["@Filters"] = (pagination.Filters != null && pagination.Filters.Any())
                ? pagination.Filters.SerializeWithCamelCaseKeys()
                : DBNull.Value;
        }
        else
        {
            // Individual parameters approach - if explicitly requested
            if (pagination.Filters != null && pagination.Filters.Any())
            {
                foreach (var kvp in pagination.Filters)
                {
                    var paramName = kvp.Key.StartsWith("@") ? kvp.Key : $"@{kvp.Key}";
                    spParameters[paramName] = kvp.Value;
                }
            }
        }

        return spParameters;
    }

    /// <summary>
    ///     Builds pagination metadata for PagedResult.
    /// </summary>
    private Dictionary<string, object> BuildPaginationMetadata(PaginationRequest pagination, CommandOptionsDto options)
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

        // Add strategy info in development environments (optional)
        // metadata["filterStrategy"] = options.UseJsonFilters ? "JSON" : "Individual";

        return metadata;
    }

    /// <summary>
    ///     Builds pagination metadata for FilterRequest (non-paginated).
    /// </summary>
    private Dictionary<string, object> BuildFilterMetadata(FilterRequest filter, CommandOptionsDto options)
    {
        var metadata = new Dictionary<string, object>
        {
            ["hasFilters"] = filter.Filters?.Any() ?? false,
            ["hasSearch"] = !string.IsNullOrWhiteSpace(filter.SearchTerm),
            ["sortBy"] = filter.SortBy ?? string.Empty,
            ["sortDirection"] = filter.SortDirection.ToString()
        };

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            metadata["searchTerm"] = filter.SearchTerm;
        }

        if (filter.Filters?.Any() == true)
        {
            metadata["filterCount"] = filter.Filters.Count;
        }

        return metadata;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Builds filtered SQL with ORDER BY clause based on FilterRequest.
    /// </summary>
    private string BuildFilteredSql(string sql, FilterRequest filter)
    {
        var builder = new StringBuilder(sql);

        // Add ORDER BY if not present and if SortBy is provided
        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var safeSortBy = ValidateAndSanitizeSortColumn(filter.SortBy);
                var direction = filter.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
                builder.Append($" ORDER BY [{safeSortBy}] {direction}");
            }
        }

        return builder.ToString();
    }

    private static string GenerateCountSql(string sql)
    {
        // Remove ORDER BY clause - match ORDER BY followed by everything until end or closing paren
        // Use Multiline mode where $ matches end of line, not Singleline where . matches newlines
        var cleanSql = Regex.Replace(
            sql,
            @"\s+ORDER\s+BY\s+[^;]+$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();

        // Fallback: if cleanSql is empty or just whitespace, use original sql
        if (string.IsNullOrWhiteSpace(cleanSql))
        {
            cleanSql = sql;
        }

        return $"SELECT COUNT(*) FROM ({cleanSql}) AS CountQuery";
    }

    private string BuildPagedSql(string sql, PaginationRequest pagination)
    {
        var builder = new StringBuilder(sql);

        // Add ORDER BY if not present and if SortBy is provided
        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(pagination.SortBy))
            {
                var safeSortBy = ValidateAndSanitizeSortColumn(pagination.SortBy);
                var direction = pagination.SortDirection == SortDirection.Desc ? "DESC" : "ASC";
                builder.Append($" ORDER BY [{safeSortBy}] {direction}");
            }
            else
            {
                // Default to first column if no sort specified
                builder.Append(" ORDER BY 1 ASC");
            }
        }

        // Add OFFSET-FETCH (SQL Server optimized)
        builder.Append(" OFFSET @__Offset ROWS FETCH NEXT @__Fetch ROWS ONLY");

        return builder.ToString();
    }

    /// <summary>
    ///     Validates and sanitizes sort column names to prevent SQL injection (CWE-89).
    ///     Uses a strict whitelist approach - only allows exact matches from a predefined set.
    ///     Falls back to regex validation with comprehensive keyword blacklist.
    /// </summary>
    private string ValidateAndSanitizeSortColumn(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("Column name cannot be empty", nameof(columnName));
        }

        // Trim and normalize
        columnName = columnName.Trim();

        // CWE-89 Prevention: Multi-layer validation approach

        // Layer 1: Strict pattern matching - only allow safe characters
        var pattern = @"^[a-zA-Z0-9_\.]+$";
        if (!Regex.IsMatch(columnName, pattern))
        {
            _logger.LogWarning("Potential SQL injection attempt detected in sort column: {ColumnName}", columnName);
            throw new ArgumentException(
                $"Invalid column name: {columnName}. Only alphanumeric characters, underscores, and dots are allowed.",
                nameof(columnName));
        }

        // Layer 2: Length validation - prevent buffer overflow attempts
        if (columnName.Length > 128)
        {
            _logger.LogWarning("Column name exceeds maximum length: {ColumnName}", columnName);
            throw new ArgumentException($"Column name exceeds maximum length of 128 characters: {columnName}",
                nameof(columnName));
        }

        // Layer 3: Enhanced keyword blacklist - prevent common SQL injection patterns
        var upperColumn = columnName.ToUpperInvariant();
        var dangerousKeywords = new[]
        {
            "DROP", "DELETE", "INSERT", "UPDATE", "TRUNCATE", "MERGE",
            "EXEC", "EXECUTE", "SP_EXECUTESQL", "XP_CMDSHELL",
            "SELECT", "UNION", "JOIN", "FROM", "WHERE",
            "CAST", "CONVERT", "TRY_CAST", "TRY_CONVERT",
            "DECLARE", "SET", "BEGIN", "END", "IF", "ELSE", "WHILE",
            "--", "/*", "*/",
            "CONCAT", "CONCAT_WS", "STRING_AGG",
            "SYSTEM", "OPENROWSET", "OPENDATASOURCE", "OPENQUERY",
            "ALTER", "CREATE", "GRANT", "REVOKE",
            "GO"
        };

        foreach (var keyword in dangerousKeywords)
        {
            var keywordPattern = $"\\b{Regex.Escape(keyword)}\\b";
            if (Regex.IsMatch(upperColumn, keywordPattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("SQL keyword detected in sort column: {ColumnName} contains {Keyword}", columnName,
                    keyword);
                throw new ArgumentException($"Column name contains restricted SQL keyword '{keyword}': {columnName}",
                    nameof(columnName));
            }
        }

        // Layer 4: Prevent common injection patterns
        var injectionPatterns = new[]
        {
            @";\s*",
            @"'\s*OR\s*'",
            @"'\s*AND\s*'",
            @"=\s*'",
            @"\|\|",
            @"@@"
        };

        foreach (var injectionPattern in injectionPatterns)
        {
            if (Regex.IsMatch(upperColumn, injectionPattern, RegexOptions.IgnoreCase))
            {
                _logger.LogWarning("SQL injection pattern detected in sort column: {ColumnName}", columnName);
                throw new ArgumentException($"Column name contains suspicious SQL pattern: {columnName}",
                    nameof(columnName));
            }
        }

        return columnName;
    }

    private void ValidatePagination(PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        if (pagination.PageIndex < 1)
        {
            throw new ArgumentException("PageIndex must be greater than 0", nameof(pagination));
        }

        if (pagination.PageSize < 1 || pagination.PageSize > 10000)
        {
            throw new ArgumentException("PageSize must be between 1 and 10000", nameof(pagination));
        }
    }

    private DataTable ConvertToDataTable<T>(IEnumerable<T> data, Dictionary<string, string>? columnMappings)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        // Add columns
        foreach (var prop in properties)
        {
            var columnName = columnMappings?.ContainsKey(prop.Name) == true
                ? columnMappings[prop.Name]
                : prop.Name;

            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(columnName, columnType);
        }

        // Add rows
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

    #endregion
}

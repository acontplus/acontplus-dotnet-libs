using System.Data.Common;

namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Dapper-based repository interface for simplified high-performance SQL operations.
/// Provides a lighter-weight alternative to <see cref="IAdoRepository"/> with Dapper's automatic mapping capabilities.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use IDapperRepository vs IAdoRepository:</b>
/// </para>
/// <list type="bullet">
/// <item><description><b>IDapperRepository</b>: Simple CRUD, quick queries, anonymous type parameters, automatic object mapping</description></item>
/// <item><description><b>IAdoRepository</b>: Complex scenarios, bulk operations, DataSet results, streaming, full control</description></item>
/// </list>
/// </remarks>
public interface IDapperRepository
{
    #region Query Methods

    /// <summary>
    /// Executes a SQL query and maps results to a collection of objects.
    /// Uses Dapper's automatic type mapping.
    /// </summary>
    /// <typeparam name="T">The type to map results to.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">Anonymous object or dictionary of parameters.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <param name="commandType">The type of command (Text, StoredProcedure, TableDirect).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of mapped objects.</returns>
    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SQL query and returns the first result or null.
    /// </summary>
    Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SQL query expecting exactly one result or null.
    /// Throws if more than one result is returned.
    /// </summary>
    Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Execute Methods

    /// <summary>
    /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
    /// Returns the number of affected rows.
    /// </summary>
    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns a single scalar value.
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Multiple Result Sets

    /// <summary>
    /// Executes a query that returns multiple result sets.
    /// Returns a tuple with the results from each result set.
    /// </summary>
    /// <typeparam name="T1">Type for the first result set.</typeparam>
    /// <typeparam name="T2">Type for the second result set.</typeparam>
    Task<(IEnumerable<T1> First, IEnumerable<T2> Second)> QueryMultipleAsync<T1, T2>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query that returns three result sets.
    /// </summary>
    Task<(IEnumerable<T1> First, IEnumerable<T2> Second, IEnumerable<T3> Third)> QueryMultipleAsync<T1, T2, T3>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Paged Query Methods

    /// <summary>
    /// Executes a paginated SQL query with automatic count query.
    /// Uses database-specific pagination (OFFSET-FETCH for SQL Server, LIMIT-OFFSET for PostgreSQL).
    /// </summary>
    /// <typeparam name="T">The type to map results to.</typeparam>
    /// <param name="sql">Main SELECT query (without OFFSET/LIMIT).</param>
    /// <param name="pagination">Pagination parameters including sort, filters, and search.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a paginated SQL query with a custom count query for complex scenarios.
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync<T>(
        string sql,
        string countSql,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a paginated stored procedure.
    /// </summary>
    Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Query Methods (Non-Paginated)

    /// <summary>
    /// Executes a filtered SQL query with sorting and search capabilities (non-paginated).
    /// </summary>
    Task<IEnumerable<T>> GetFilteredAsync<T>(
        string sql,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a filtered stored procedure (non-paginated).
    /// </summary>
    Task<IEnumerable<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Transaction Support

    /// <summary>
    /// Sets the current database transaction from the Unit of Work.
    /// </summary>
    void SetTransaction(DbTransaction transaction);

    /// <summary>
    /// Sets the current database connection from the Unit of Work.
    /// </summary>
    void SetConnection(DbConnection connection);

    /// <summary>
    /// Clears the current transaction and connection.
    /// </summary>
    void ClearTransaction();

    #endregion
}

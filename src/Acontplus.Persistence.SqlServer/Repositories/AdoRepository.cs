using System.Diagnostics.CodeAnalysis;
using System.Text;
using Acontplus.Core.Enums;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Mapping;
using Acontplus.Persistence.Common.Repositories;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.SqlServer.Repositories;

/// <summary>
/// Provides ADO.NET data access operations with retry policy and optional transaction sharing for SQL Server.
/// Enhanced with SQL Server error handling, domain error mapping, and flexible filter parameter strategies.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2139",
    Justification = "Repository methods log detailed diagnostic context before rethrowing database exceptions.")]
public class AdoRepository(
    IConfiguration configuration,
    ILogger<AdoRepository> logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : BaseAdoRepository(configuration, logger, resilienceOptions)
{
    /// <inheritdoc />
    protected override string ProviderName => "SQL Server";

    /// <inheritdoc />
    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    /// <inheritdoc />
    protected override bool IsTransientException(Exception exception) =>
        exception is SqlException sqlEx && SqlServerExceptionHandler.IsTransientException(sqlEx);

    /// <inheritdoc />
    protected override bool IsProviderException(Exception exception) => exception is SqlException;

    /// <inheritdoc />
    protected override void HandleProviderException(Exception exception, string operationName)
    {
        if (exception is SqlException sqlEx)
        {
            SqlServerExceptionHandler.HandleSqlException(sqlEx, Logger, operationName);
        }
    }

    /// <inheritdoc />
    protected override DbDataAdapter CreateDataAdapter(DbCommand command) => new SqlDataAdapter((SqlCommand)command);

    /// <inheritdoc />
    protected override string SanitizeIdentifier(string identifier) => $"[{identifier}]";

    /// <inheritdoc />
    protected override string BuildPagedSql(string sql, PaginationRequest pagination)
    {
        var builder = new StringBuilder(sql);

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
                builder.Append(" ORDER BY 1 ASC");
            }
        }

        builder.Append(" OFFSET @__Offset ROWS FETCH NEXT @__Fetch ROWS ONLY");
        return builder.ToString();
    }

    /// <inheritdoc />
    protected override void AddTableNamesOutputParameter(DbCommand command, CommandOptionsDto options)
    {
        if (command is SqlCommand sqlCmd)
        {
            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(sqlCmd, "@tableNames", SqlDbType.VarChar, options.TableNamesLength);
        }
    }

    /// <inheritdoc />
    public override async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CommandOptionsDto { CommandType = CommandType.StoredProcedure };

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            var spParameters = BuildStoredProcedureParameters(pagination, options);
            await using var cmd = (SqlCommand)CreateCommand(connection, storedProcedureName, spParameters, options);

            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(cmd, "@TotalCount", SqlDbType.Int, 0);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var items = await reader.ToListAsync<T>(ct);

            await reader.CloseAsync();

            var totalCount = cmd.Parameters["@TotalCount"].Value != DBNull.Value
                ? Convert.ToInt32(cmd.Parameters["@TotalCount"].Value)
                : 0;

            var metadata = BuildPaginationMetadata(pagination);
            return new PagedResult<T>(items, pagination.PageIndex, pagination.PageSize, totalCount, metadata);
        }, nameof(GetPagedFromStoredProcedureAsync), cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<List<T>> GetFilteredFromStoredProcedureAsync<T>(
        string storedProcedureName,
        FilterRequest filter,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        options ??= new CommandOptionsDto { CommandType = CommandType.StoredProcedure };

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            var spParameters = BuildStoredProcedureParameters(filter, options);
            await using var cmd = CreateCommand(connection, storedProcedureName, spParameters, options);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            return await reader.ToListAsync<T>(ct);
        }, nameof(GetFilteredFromStoredProcedureAsync), cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<int> BulkInsertAsync(
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

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            using var bulkCopy = new SqlBulkCopy(
                (SqlConnection)connection,
                SqlBulkCopyOptions.Default,
                (SqlTransaction?)CurrentTransaction)
            {
                DestinationTableName = tableName,
                BatchSize = batchSize,
                BulkCopyTimeout = 300,
                EnableStreaming = true
            };

            ConfigureSqlBulkCopyMappings(bulkCopy, dataTable, columnMappings);
            await bulkCopy.WriteToServerAsync(dataTable, ct);
            return dataTable.Rows.Count;
        }, nameof(BulkInsertAsync), cancellationToken);
    }

    private static void ConfigureSqlBulkCopyMappings(
        SqlBulkCopy bulkCopy,
        DataTable dataTable,
        Dictionary<string, string>? columnMappings)
    {
        if (columnMappings != null)
        {
            foreach (var mapping in columnMappings)
            {
                bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
            }
        }
        else
        {
            foreach (DataColumn column in dataTable.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }
        }
    }
}

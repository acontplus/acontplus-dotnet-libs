using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Acontplus.Core.Enums;
using Acontplus.Core.Extensions;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Repositories;
using Microsoft.Extensions.Options;

namespace Acontplus.Persistence.PostgreSQL.Repositories;

/// <summary>
/// Provides ADO.NET data access operations with retry policy and optional transaction sharing for PostgreSQL.
/// Enhanced with PostgreSQL error handling, domain error mapping, and flexible filter parameter strategies.
/// </summary>
[SuppressMessage("SonarQube", "csharpsquid:S2139",
    Justification = "Repository methods log detailed diagnostic context before rethrowing database exceptions.")]
public partial class AdoRepository(
    IConfiguration configuration,
    ILogger<AdoRepository> logger,
    IOptions<PersistenceResilienceOptions> resilienceOptions) : BaseAdoRepository(configuration, logger, resilienceOptions)
{
    private const string FilterKey = "filters";

    /// <inheritdoc />
    protected override string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    protected override DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    /// <inheritdoc />
    protected override bool IsTransientException(Exception exception) =>
        exception is NpgsqlException npgEx && PostgresExceptionHandler.IsTransientException(npgEx);

    /// <inheritdoc />
    protected override bool IsProviderException(Exception exception) => exception is NpgsqlException;

    /// <inheritdoc />
    protected override void HandleProviderException(Exception exception, string operationName)
    {
        if (exception is NpgsqlException npgEx)
        {
            PostgresExceptionHandler.HandleSqlException(npgEx, Logger, operationName);
        }
    }

    /// <inheritdoc />
    protected override DbDataAdapter CreateDataAdapter(DbCommand command) => new NpgsqlDataAdapter((NpgsqlCommand)command);

    /// <inheritdoc />
    protected override string SanitizeIdentifier(string identifier) => $"\"{identifier}\"";

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
                builder.Append($" ORDER BY \"{safeSortBy}\" {direction}");
            }
            else
            {
                builder.Append(" ORDER BY 1 ASC");
            }
        }

        builder.Append(" LIMIT @__Limit OFFSET @__Offset");
        return builder.ToString();
    }

    /// <inheritdoc />
    protected override void AddTableNamesOutputParameter(DbCommand command, CommandOptionsDto options)
    {
        if (command is NpgsqlCommand npgCmd)
        {
            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(npgCmd, "tableNames", NpgsqlDbType.Varchar, options.TableNamesLength);
        }
    }

    /// <inheritdoc />
    public override async Task<PagedResult<T>> GetPagedFromStoredProcedureAsync<T>(
        string storedProcedureName,
        PaginationRequest pagination,
        CommandOptionsDto? options = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pagination);
        options ??= new CommandOptionsDto { CommandType = CommandType.StoredProcedure };

        return await ExecuteWithConnectionAsync(async (connection, ct) =>
        {
            var spParameters = BuildPostgresStoredProcedureParameters(pagination, options);
            await using var cmd = (NpgsqlCommand)CreateCommand(connection, storedProcedureName, spParameters, options);

            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(cmd, "total_count", NpgsqlDbType.Integer, 0);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var items = await reader.ToListAsync<T>(ct);

            await reader.CloseAsync();

            var totalCount = cmd.Parameters["total_count"].Value != DBNull.Value
                ? Convert.ToInt32(cmd.Parameters["total_count"].Value)
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
            var spParameters = BuildPostgresStoredProcedureParameters(filter, options);
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
            var npgsqlConnection = (NpgsqlConnection)connection;
            var copyCommand = BuildCopyCommand(dataTable, tableName, columnMappings);

            await using var writer = await npgsqlConnection.BeginBinaryImportAsync(copyCommand, ct);
            await WriteDataTableRowsAsync(writer, dataTable, ct);
            await writer.CompleteAsync(ct);

            return dataTable.Rows.Count;
        }, nameof(BulkInsertAsync), cancellationToken);
    }

    private static string BuildCopyCommand(DataTable dataTable, string tableName, Dictionary<string, string>? columnMappings)
    {
        var columns = columnMappings != null
            ? string.Join(", ", columnMappings.Values.Select(c => $"\"{c}\""))
            : string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName}\""));

        return $"COPY {tableName} ({columns}) FROM STDIN (FORMAT BINARY)";
    }

    private static async Task WriteDataTableRowsAsync(
        NpgsqlBinaryImporter writer,
        DataTable dataTable,
        CancellationToken ct)
    {
        foreach (DataRow row in dataTable.Rows)
        {
            await writer.StartRowAsync(ct);
            foreach (DataColumn column in dataTable.Columns)
            {
                var value = row[column];
                await writer.WriteAsync(value == DBNull.Value ? null : value, ct);
            }
        }
    }

    private Dictionary<string, object> BuildPostgresStoredProcedureParameters(FilterRequest filter, CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(filter.SortBy))
        {
            spParameters["sort_by"] = ValidateAndSanitizeSortColumn(filter.SortBy);
            spParameters["sort_direction"] = filter.SortDirection.ToString();
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            spParameters["search_term"] = filter.SearchTerm;
        }

        PopulatePostgresStoredProcedureFilters(spParameters, filter.Filters, options.UseJsonFilters ?? true);
        return spParameters;
    }

    private Dictionary<string, object> BuildPostgresStoredProcedureParameters(PaginationRequest pagination, CommandOptionsDto options)
    {
        var spParameters = new Dictionary<string, object>
        {
            ["page_index"] = pagination.PageIndex,
            ["page_size"] = pagination.PageSize
        };

        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
        {
            spParameters["sort_by"] = ValidateAndSanitizeSortColumn(pagination.SortBy);
            spParameters["sort_direction"] = pagination.SortDirection.ToString();
        }

        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            spParameters["search_term"] = pagination.SearchTerm;
        }

        PopulatePostgresStoredProcedureFilters(spParameters, pagination.Filters, options.UseJsonFilters ?? true);
        return spParameters;
    }

    private static void PopulatePostgresStoredProcedureFilters(
        Dictionary<string, object> spParameters,
        IReadOnlyDictionary<string, object>? filters,
        bool useJsonFilters)
    {
        if (filters == null || filters.Count == 0)
        {
            if (useJsonFilters)
            {
                spParameters[FilterKey] = DBNull.Value;
            }

            return;
        }

        if (useJsonFilters)
        {
            spParameters[FilterKey] = filters.SerializeWithCamelCaseKeys();
        }
        else
        {
            foreach (var kvp in filters)
            {
                var paramName = ConvertToSnakeCase(kvp.Key);
                spParameters[paramName] = kvp.Value;
            }
        }
    }

    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        input = input.TrimStart('@');
        return SnakeCaseRegex().Replace(input, "$1_$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.None, 500)]
    private static partial Regex SnakeCaseRegex();
}

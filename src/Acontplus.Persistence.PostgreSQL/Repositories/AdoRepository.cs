using System.Diagnostics.CodeAnalysis;
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
    protected override string BuildPagedSql(string sql, PaginationRequest pagination) =>
        $"{AppendOrderByIfMissing(sql, pagination)} LIMIT @__Limit OFFSET @__Offset";

    /// <inheritdoc />
    protected override void AddTableNamesOutputParameter(DbCommand command, CommandOptionsDto options)
    {
        if (command is NpgsqlCommand npgCmd)
        {
            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(npgCmd, "tableNames", NpgsqlDbType.Varchar, options.TableNamesLength);
        }
    }

    /// <inheritdoc />
    protected override void AddTotalCountParameter(DbCommand command)
    {
        if (command is NpgsqlCommand npgCmd)
        {
            Ado.Parameters.CommandParameterBuilder.AddOutputParameter(npgCmd, "total_count", NpgsqlDbType.Integer, 0);
        }
    }

    /// <inheritdoc />
    protected override int ReadTotalCountParameter(DbCommand command) =>
        command.Parameters["total_count"].Value != DBNull.Value
            ? Convert.ToInt32(command.Parameters["total_count"].Value)
            : 0;

    /// <inheritdoc />
    protected override Dictionary<string, object> CreateStoredProcedureParameters(PaginationRequest pagination, CommandOptionsDto options) =>
        BuildPostgresStoredProcedureParameters(pagination, options);

    /// <inheritdoc />
    protected override Dictionary<string, object> CreateStoredProcedureParameters(FilterRequest filter, CommandOptionsDto options) =>
        BuildPostgresStoredProcedureParameters(filter, options);

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

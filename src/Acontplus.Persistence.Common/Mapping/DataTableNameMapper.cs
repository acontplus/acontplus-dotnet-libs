namespace Acontplus.Persistence.Common.Mapping;

/// <summary>
/// Provides utilities for assigning table names in a <see cref="DataSet"/> from a stored procedure output parameter.
/// </summary>
public static class DataTableNameMapper
{
    /// <summary>
    /// Assigns table names from the <c>@tableNames</c> or <c>tableNames</c> command parameter to the corresponding tables in the <see cref="DataSet"/>.
    /// </summary>
    /// <param name="cmd">The database command containing the table names parameter.</param>
    /// <param name="ds">The data set whose tables will be renamed.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public static async Task ProcessTableNames(DbCommand cmd, DataSet ds, CancellationToken cancellationToken = default)
    {
        string? rawTableNames = null;
        if (cmd.Parameters.Contains("@tableNames"))
        {
            rawTableNames = cmd.Parameters["@tableNames"].Value?.ToString();
        }
        else if (cmd.Parameters.Contains("tableNames"))
        {
            rawTableNames = cmd.Parameters["tableNames"].Value?.ToString();
        }

        var tableNames = rawTableNames?.Split(',');
        if (tableNames == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
            Parallel.ForEach(tableNames, parallelOptions, (tableName, _, index) =>
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return;
                }

                if (index >= 0 && index < ds.Tables.Count)
                {
                    ds.Tables[(int)index].TableName = tableName;
                }
            });
        }, cancellationToken);
    }
}

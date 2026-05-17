namespace Acontplus.Persistence.SqlServer.Mapping;

/// <summary>
/// Provides utilities for assigning table names in a <see cref="DataSet"/> from a stored procedure output parameter.
/// </summary>
public static class DataTableNameMapper
{
    /// <summary>
    /// Assigns table names from the <c>@tableNames</c> command parameter to the corresponding tables in the <see cref="DataSet"/>.
    /// </summary>
    /// <param name="cmd">The SQL command containing the <c>@tableNames</c> parameter.</param>
    /// <param name="ds">The data set whose tables will be renamed.</param>
    public static async Task ProcessTableNames(SqlCommand cmd, DataSet ds)
    {
        var tableNames = cmd.Parameters["@tableNames"].Value?.ToString()?.Split(',');
        if (tableNames == null)
        {
            return;
        }

        // Parallel.ForEach is okay here as it's a CPU-bound operation on in-memory data
        await Task.Run(() =>
        {
            Parallel.ForEach(tableNames, (tableName, _, index) =>
            {
                if (string.IsNullOrEmpty(tableName))
                {
                    return;
                }

                // Ensure index is within bounds to prevent ArgumentOutOfRangeException
                if (index >= 0 && index < ds.Tables.Count)
                {
                    ds.Tables[(int)index].TableName = tableName;
                }
            });
        });
    }
}

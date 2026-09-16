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
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public static Task ProcessTableNames(SqlCommand cmd, DataSet ds, CancellationToken cancellationToken = default) =>
        Common.Mapping.DataTableNameMapper.ProcessTableNames(cmd, ds, cancellationToken);
}

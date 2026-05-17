namespace Acontplus.Persistence.PostgreSQL.Ado.Parameters;

/// <summary>
/// Provides helper methods for building ADO.NET command parameters.
/// </summary>
public static class CommandParameterBuilder
{
    /// <summary>
    /// Adds a named input parameter to the specified <see cref="DbCommand"/>.
    /// </summary>
    /// <param name="cmd">The command to add the parameter to.</param>
    /// <param name="name">The parameter name. An '@' prefix is added automatically if missing.</param>
    /// <param name="value">The parameter value.</param>
    public static void AddParameter(DbCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name.StartsWith("@") ? name : $"@{name}";
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    /// <summary>
    /// Adds a named output parameter to the specified <see cref="NpgsqlCommand"/>.
    /// </summary>
    /// <param name="cmd">The Npgsql command to add the parameter to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="type">The Npgsql data type of the parameter.</param>
    /// <param name="size">The maximum size of the parameter value.</param>
    public static void AddOutputParameter(NpgsqlCommand cmd, string name, NpgsqlDbType type, int size)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.NpgsqlDbType = type;
        param.Size = size;
        param.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(param);
    }

    /// <summary>
    /// Returns the value of the named output parameter from the specified <see cref="NpgsqlCommand"/>.
    /// </summary>
    /// <param name="command">The Npgsql command containing the parameter.</param>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>The parameter value, or <c>null</c>.</returns>
    public static object? GetParameter(NpgsqlCommand command, string parameterName)
    {
        return command.Parameters[parameterName].Value;
    }
}

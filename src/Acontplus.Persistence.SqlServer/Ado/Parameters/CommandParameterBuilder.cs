namespace Acontplus.Persistence.SqlServer.Ado.Parameters;

/// <summary>
/// Provides utility methods for building and managing SQL command parameters.
/// </summary>
public static class CommandParameterBuilder
{
    /// <summary>
    /// Adds an input parameter to the specified database command.
    /// </summary>
    /// <param name="cmd">The database command to add the parameter to.</param>
    /// <param name="name">The parameter name, with or without the '@' prefix.</param>
    /// <param name="value">The parameter value.</param>
    public static void AddParameter(DbCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name.StartsWith("@") ? name : $"@{name}";
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    /// <summary>
    /// Adds an output parameter to the specified SQL command.
    /// </summary>
    /// <param name="cmd">The SQL command to add the parameter to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="type">The SQL database type of the parameter.</param>
    /// <param name="size">The maximum size of the parameter data.</param>
    public static void AddOutputParameter(SqlCommand cmd, string name, SqlDbType type, int size)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.SqlDbType = type;
        param.Size = size;
        param.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(param);
    }

    /// <summary>
    /// Retrieves the value of a parameter from the specified SQL command.
    /// </summary>
    /// <param name="command">The SQL command containing the parameter.</param>
    /// <param name="parameterName">The name of the parameter to retrieve.</param>
    /// <returns>The value of the specified parameter.</returns>
    public static object GetParameter(SqlCommand command, string parameterName)
    {
        if (command?.Parameters[parameterName] == null)
            throw new ArgumentException($"Parameter '{parameterName}' not found in command.", nameof(parameterName));
        return command.Parameters[parameterName].Value;
    }
}

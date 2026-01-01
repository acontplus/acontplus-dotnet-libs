namespace Acontplus.Persistence.PostgreSQL.Ado.Parameters;

public static class CommandParameterBuilder
{
    public static void AddParameter(DbCommand cmd, string name, object value)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name.StartsWith("@") ? name : $"@{name}";
        param.Value = value;
        cmd.Parameters.Add(param);
    }

    public static void AddOutputParameter(NpgsqlCommand cmd, string name, NpgsqlDbType type, int size)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = name;
        param.NpgsqlDbType = type;
        param.Size = size;
        param.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(param);
    }

    public static object? GetParameter(NpgsqlCommand command, string parameterName)
    {
        return command.Parameters[parameterName].Value;
    }
}

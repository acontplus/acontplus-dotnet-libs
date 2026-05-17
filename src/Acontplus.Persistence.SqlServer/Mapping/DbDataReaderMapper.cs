using System.Reflection;
using System.Runtime.CompilerServices;

namespace Acontplus.Persistence.SqlServer.Mapping;

/// <summary>
/// Provides extension methods for mapping <see cref="DbDataReader"/> results to strongly-typed objects.
/// </summary>
public static class DbDataReaderMapper
{
    /// <summary>
    ///     Maps a DbDataReader to a List of entities of type T with support for records and init-only properties
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(this DbDataReader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var result = new List<T>();
        var type = typeof(T);
        var isRecord = IsRecordType(type);

        // Get all public instance properties including init-only
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsWritable(p, isRecord))
            .ToArray();

        var columnMap = BuildColumnMapping(reader, properties);

        await foreach (var item in MapRecordsAsync<T>(reader, columnMap, cancellationToken))
        {
            result.Add(item);
        }

        return result;
    }

    private static async IAsyncEnumerable<T> MapRecordsAsync<T>(
        DbDataReader reader,
        Dictionary<string, PropertyInfo> columnMap,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var columnNames = columnMap.Keys.ToArray();

        while (await reader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var instance = CreateInstance<T>();

            foreach (var columnName in columnNames)
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (await reader.IsDBNullAsync(ordinal, cancellationToken))
                {
                    continue;
                }

                var value = reader.GetValue(ordinal);
                var property = columnMap[columnName];
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                try
                {
                    var convertedValue = ConvertValue(value, propertyType);
                    property.SetValue(instance, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error mapping column '{columnName}' to property '{property.Name}'", ex);
                }
            }

            yield return instance;
        }
    }

    private static Dictionary<string, PropertyInfo> BuildColumnMapping(
        DbDataReader reader,
        PropertyInfo[] properties)
    {
        var columnMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);
            if (string.IsNullOrEmpty(columnName))
            {
                continue;
            }

            var property = properties.FirstOrDefault(p =>
                string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                columnMap[columnName] = property;
            }
        }

        return columnMap;
    }

    private static T CreateInstance<T>()
    {
        var type = typeof(T);

        if (type.IsValueType)
        {
            return default!;
        }

        if (type.GetConstructor(Type.EmptyTypes) != null)
        {
            return Activator.CreateInstance<T>();
        }

        // For records and types without parameterless constructors, use RuntimeHelpers
        try
        {
            return (T)RuntimeHelpers.GetUninitializedObject(type);
        }
        catch
        {
            throw new InvalidOperationException(
                $"Unable to create instance of type {type.Name}. Ensure it has a parameterless constructor.");
        }
    }

    private static object ConvertValue(object value, Type targetType)
    {
        try
        {
            return targetType.IsEnum
                ? Enum.ToObject(targetType, value)
                : targetType == typeof(Guid)
                    ? value is string s ? Guid.Parse(s) : (Guid)value
                    : targetType == typeof(DateTimeOffset)
                        ? value is DateTime dt ? new DateTimeOffset(dt) : (DateTimeOffset)value
                        : Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to convert value '{value}' to type {targetType.Name}", ex);
        }
    }

    private static bool IsRecordType(Type type)
    {
        // Check for compiler-generated attributes or Clone method
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) ||
               type.GetMethods().Any(m => m.Name == "<Clone>$");
    }

    private static bool IsWritable(PropertyInfo prop, bool isRecord)
    {
        // For records, we consider init-only properties as writable during construction
        if (isRecord)
        {
            var setMethod = prop.GetSetMethod(true);
            return setMethod != null && (setMethod.IsPublic || setMethod.IsAssembly);
        }

        return prop.CanWrite;
    }

    // Synchronous version
    /// <summary>
    /// Synchronously maps all rows from the data reader to a list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to map each row to.</typeparam>
    /// <param name="reader">The data reader positioned before the first row.</param>
    /// <returns>A list of mapped <typeparamref name="T"/> instances.</returns>
    public static List<T> ToList<T>(this DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var result = new List<T>();
        var type = typeof(T);
        var isRecord = IsRecordType(type);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsWritable(p, isRecord))
            .ToArray();

        var columnMap = BuildColumnMapping(reader, properties);

        while (reader.Read())
        {
            var instance = CreateInstance<T>();

            foreach (var columnName in columnMap.Keys)
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                {
                    continue;
                }

                var value = reader.GetValue(ordinal);
                var property = columnMap[columnName];
                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                try
                {
                    var convertedValue = ConvertValue(value, propertyType);
                    property.SetValue(instance, convertedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error mapping column '{columnName}' to property '{property.Name}'", ex);
                }
            }

            result.Add(instance);
        }

        return result;
    }

    private static Task<T> MapProperties<T>(SqlDataReader reader, T instance) where T : class
    {
        // Pre-cache column ordinals
        var columnOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columnOrdinals[reader.GetName(i)] = i;
        }

        foreach (var property in typeof(T).GetProperties())
        {
            if (!columnOrdinals.TryGetValue(property.Name, out var index) || reader.IsDBNull(index))
            {
                continue;
            }

            var value = reader.GetValue(index);
            try
            {
                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(instance, convertedValue);
            }
            catch (InvalidCastException)
            {
                // Handle or ignore conversion errors
            }
        }

        return Task.FromResult(instance);
    }

    private static object? GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>
    ///     Maps a single row from a SqlDataReader to an object of type T using reflection.
    /// </summary>
    public static async Task<T?> MapToObject<T>(SqlDataReader reader) where T : class
    {
        try
        {
            // Try to get the first constructor and its parameters
            var ctor = typeof(T).GetConstructors().FirstOrDefault();
            if (ctor == null)
            {
                return null;
            }

            var parameters = ctor.GetParameters();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var paramName = parameters[i].Name;
                if (paramName == null)
                {
                    continue;
                }

                try
                {
                    var ordinal = reader.GetOrdinal(paramName);
                    args[i] = reader.IsDBNull(ordinal)
                        ? GetDefaultValue(parameters[i].ParameterType)
                        : reader.GetValue(ordinal);
                }
                catch
                {
                    args[i] = GetDefaultValue(parameters[i].ParameterType);
                }
            }

            var instance = (T?)ctor.Invoke(args);
            if (instance == null)
            {
                return null;
            }

            // Map remaining properties that weren't set via constructor
            return await MapProperties(reader, instance);
        }
        catch
        {
            return null;
        }
    }
}

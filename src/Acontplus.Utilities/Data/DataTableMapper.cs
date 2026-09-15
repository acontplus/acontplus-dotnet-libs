using System.Reflection;

namespace Acontplus.Utilities.Data;

/// <summary>
/// Provides reflection-based mapping utilities between ADO.NET DataTables / DataRows and strongly typed models.
/// </summary>
public static class DataTableMapper
{
    /// <summary>
    /// Maps a single <see cref="DataRow"/> to a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target model type.</typeparam>
    /// <param name="row">The source data row to map.</param>
    /// <returns>A mapped instance of <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="row"/> is null.</exception>
    public static T MapDataRowToModel<T>(DataRow row)
    {
        if (row == null)
        {
            throw new ArgumentException("DataRow is null");
        }

        var columns = row.Table.Columns.Cast<DataColumn>().Select(dc => dc.ColumnName.ToLower()).ToList();
        var type = typeof(T);

        // Create instance using the constructor that initializes required members
        var instance = CreateInstance<T>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite) continue;

            var columnExists = columns.Contains(property.Name.ToLower());
            if (!columnExists) continue;

            var value = row[property.Name];
            if (value == DBNull.Value)
            {
                if (IsNullableType(property.PropertyType))
                {
                    property.SetValue(instance, null);
                }

                continue;
            }

            var convertedValue = ConvertValue(value, property.PropertyType);
            if (convertedValue != null)
            {
                property.SetValue(instance, convertedValue);
            }
        }

        ValidateRequiredProperties(instance);
        return instance;
    }

    private static T CreateInstance<T>()
    {
        var type = typeof(T);
        try
        {
            // Try to use the parameterless constructor first
            return Activator.CreateInstance<T>();
        }
        catch (MissingMethodException)
        {
            // If parameterless constructor is not available, find a suitable constructor
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"No public constructors found for type {type.Name}");
            }

            // Use the constructor with the least parameters (assuming it sets required members)
            var constructor = constructors.OrderBy(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters().Select(p => GetDefaultValue(p.ParameterType)).ToArray();
            return (T)constructor.Invoke(parameters);
        }
    }

    private static void ValidateRequiredProperties<T>(T instance)
    {
        var type = typeof(T);
        var requiredPropertiesNotSet = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsRequiredProperty(property)) continue;

            var value = property.GetValue(instance);
            if (value == null || value is string str && string.IsNullOrEmpty(str))
            {
                requiredPropertiesNotSet.Add(property.Name);
            }
        }

        if (requiredPropertiesNotSet.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required properties not set: {string.Join(", ", requiredPropertiesNotSet)}");
        }
    }

    private static bool IsRequiredProperty(PropertyInfo property) =>
        property.CustomAttributes.Any(attr =>
            attr.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");

    private static object? GetDefaultValue(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>
    /// Maps all rows in a <see cref="DataTable"/> to a strongly typed list of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target model type, requiring a parameterless constructor.</typeparam>
    /// <param name="dt">The source data table.</param>
    /// <returns>A list of mapped model instances.</returns>
    public static List<T> MapDataTableToList<T>(DataTable dt) where T : new()
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            return []; // Return empty list instead of throwing exception
        }

        return (from DataRow dr in dt.Rows select MapDataRowToModel<T>(dr)).ToList();
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // If types match exactly, return as-is
        if (value.GetType() == underlyingType)
            return value;

        try
        {
            // Handle special cases first
            if (underlyingType == typeof(bool))
            {
                return ConvertBooleanValue(value);
            }

            if (underlyingType == typeof(int) && value is string intStr)
            {
                return int.TryParse(intStr, out var intResult) ? intResult : 0;
            }

            if (underlyingType == typeof(byte[]))
            {
                return ConvertByteArrayValue(value);
            }

            // Handle List<T> types
            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ConvertListValue(value, targetType);
            }

            // Handle string to string conversion explicitly to avoid unnecessary Convert.ChangeType
            if (underlyingType == typeof(string))
            {
                return value.ToString();
            }

            // Try standard conversion for other types
            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            // Log the conversion failure if needed
            Debug.WriteLine($"Failed to convert value '{value}' to type {targetType.Name}: {ex.Message}");

            // Return default value for value types, null for reference types
            return underlyingType.IsValueType ? Activator.CreateInstance(underlyingType) : null;
        }
    }

    private static bool ConvertBooleanValue(object value)
    {
        if (value is string strValue)
        {
            if (bool.TryParse(strValue, out var boolResult))
                return boolResult;

            return strValue.ToLowerInvariant() switch
            {
                "1" or "yes" or "y" => true,
                "0" or "no" or "n" => false,
                _ => false
            };
        }

        if (value is int intValue)
            return intValue != 0;

        return false;
    }

    private static byte[]? ConvertByteArrayValue(object value)
    {
        if (value is string base64Str)
        {
            try
            {
                return Convert.FromBase64String(base64Str);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static object? ConvertListValue(object value, Type targetType)
    {
        if (value is string jsonStr)
        {
            try
            {
                return JsonSerializer.Deserialize(jsonStr, targetType, JsonExtensions.DefaultOptions);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static bool IsNullableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ||
               !type.IsValueType;
    }
}

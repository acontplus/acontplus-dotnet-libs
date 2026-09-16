namespace Acontplus.Utilities.Data;

/// <summary>
/// Data Converters
/// </summary>
public static class DataConverters
{
    /// <summary>
    /// Get default JSON serialization options with camelCase naming
    /// </summary>
    private static JsonSerializerOptions GetDefaultOptions() =>
        // Use the new DefaultOptions from JsonExtensions
        JsonExtensions.DefaultOptions;

    /// <summary>
    /// Converts a DataTable to JSON string
    /// </summary>
    public static string DataTableToJson(DataTable table)
    {
        if (table == null)
            return "null";

        // Convert DataTable to a list of row dictionaries to avoid System.Type serialization issues
        var rows = new List<Dictionary<string, object?>>();
        var namingPolicy = JsonNamingPolicy.CamelCase;

        foreach (DataRow dr in table.Rows)
        {
            var row = new Dictionary<string, object?>();
            foreach (DataColumn col in table.Columns)
            {
                // Apply camelCase naming to column names
                var camelCaseName = namingPolicy.ConvertName(col.ColumnName);
                row[camelCaseName] = dr[col] == DBNull.Value ? null : dr[col];
            }
            rows.Add(row);
        }

        return JsonSerializer.Serialize(rows, GetDefaultOptions());
    }

    /// <summary>
    /// Converts a DataSet to JSON string
    /// </summary>
    public static string DataSetToJson(DataSet dataSet)
    {
        var dataSetDict = new Dictionary<string, object>();
        foreach (DataTable table in dataSet.Tables)
        {
            dataSetDict[table.TableName] = ConvertDataTableToList(table);
        }
        return dataSetDict.SerializeOptimized();
    }
    /// <summary>
    /// Converts a JSON string to DataTable.
    /// </summary>
    public static DataTable JsonToDataTable(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new DataTable();

        try
        {
            var rows = json.DeserializeOptimized<List<Dictionary<string, object>>>();
            if (rows == null || rows.Count == 0)
                return new DataTable();

            var dt = new DataTable();
            PopulateColumns(dt, rows[0]);
            PopulateRows(dt, rows);
            return dt;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to convert JSON to DataTable: {ex.Message}", ex);
        }
    }

    private static void PopulateColumns(DataTable dt, Dictionary<string, object> firstRow)
    {
        foreach (var (key, value) in firstRow)
        {
            dt.Columns.Add(key, InferColumnType(value));
        }
    }

    private static void PopulateRows(DataTable dt, List<Dictionary<string, object>> rows)
    {
        foreach (var row in rows)
        {
            var dataRow = dt.NewRow();
            foreach (var (key, value) in row)
            {
                dataRow[key] = ConvertCellValue(value);
            }
            dt.Rows.Add(dataRow);
        }
    }

    private static Type InferColumnType(object? value)
    {
        if (value is null)
            return typeof(string);

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number => typeof(decimal),
                System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => typeof(bool),
                _ => typeof(string)
            };
        }

        return value.GetType();
    }

    private static object ConvertCellValue(object? value)
    {
        if (value is null)
            return DBNull.Value;

        if (value is System.Text.Json.JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => jsonElement.GetString() ?? (object)DBNull.Value,
                System.Text.Json.JsonValueKind.Number => jsonElement.TryGetDecimal(out var d) ? d : jsonElement.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => DBNull.Value,
                _ => jsonElement.ToString()
            };
        }

        return value;
    }

    /// <summary>
    /// Serializes a dictionary to an optimized JSON string.
    /// </summary>
    /// <param name="data">The dictionary data to serialize.</param>
    /// <returns>A JSON string representation of the dictionary.</returns>
    public static string SerializeDictionary(Dictionary<string, object> data) => data.SerializeOptimized();

    /// <summary>
    /// Helper method to sanitize values for serialization
    /// </summary>
    private static object? SanitizeValueForSerialization(object? value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        // Handle DataTable
        if (value is DataTable dt)
            return ConvertDataTableToList(dt);

        // Handle DataSet
        if (value is DataSet ds)
        {
            var result = new Dictionary<string, object>();
            foreach (DataTable table in ds.Tables)
            {
                result[table.TableName] = ConvertDataTableToList(table);
            }
            return result;
        }

        // Handle Type objects (convert to string)
        if (value is Type)
            return value.ToString();

        // Check if it's an array
        if (value is Array arr)
        {
            var result = new List<object?>();
            foreach (var item in arr)
            {
                result.Add(SanitizeValueForSerialization(item));
            }
            return result;
        }

        // Handle generic collections but preserve their actual values
        if (value is IEnumerable collection and not string)
        {
            var result = new List<object?>();
            foreach (var item in collection)
            {
                result.Add(SanitizeValueForSerialization(item));
            }
            return result;
        }

        // Return primitives and other simple types as is
        return value;
    }

    /// <summary>
    /// Helper method to convert DataTable to a list of dictionaries
    /// </summary>
    private static List<Dictionary<string, object?>> ConvertDataTableToList(DataTable table)
    {
        var result = new List<Dictionary<string, object?>>();
        var namingPolicy = JsonNamingPolicy.CamelCase;

        foreach (DataRow row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in table.Columns)
            {
                var camelCaseName = namingPolicy.ConvertName(col.ColumnName);
                dict[camelCaseName] = row[col] == DBNull.Value ? null : row[col];
            }
            result.Add(dict);
        }

        return result;
    }

    /// <summary>
    /// Convert dictionary to string representation
    /// </summary>
    public static string DictionaryToString<TKey, TValue>(IDictionary<TKey, TValue> dictionary) =>
        dictionary == null ? "{}" : "{" + string.Join(", ", dictionary.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray()) + "}";

    /// <summary>
    /// Generic method to serialize any object to JSON.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="data">The object to serialize.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string SerializeObjectCustom<T>(T? data)
    {
        if (data is null)
            return "null";

        // If it's a DataTable or DataSet, use our specialized methods
        if (data is DataTable dt)
            return DataTableToJson(dt);

        if (data is DataSet ds)
            return DataSetToJson(ds);

        // For other objects, apply standard serialization
        try
        {
            return JsonSerializer.Serialize(data, GetDefaultOptions());
        }
        catch (JsonException)
        {
            // If standard serialization fails, try to sanitize the object first
            var sanitizedData = SanitizeValueForSerialization(data);
            return JsonSerializer.Serialize(sanitizedData, GetDefaultOptions());
        }
    }

    /// <summary>
    /// Serializes an object to JSON with optional formatting configurations.
    /// </summary>
    /// <param name="data">The object to serialize.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The serialized JSON string.</returns>
    public static string SerializeWithOptions(object data, JsonSerializerOptions? options = null) =>
        data.SerializeOptimized(options == JsonExtensions.PrettyOptions);

    /// <summary>
    /// Serializes an object after sanitizing complex or database values.
    /// </summary>
    /// <param name="data">The object to sanitize and serialize.</param>
    /// <returns>The sanitized JSON string.</returns>
    public static string SerializeSanitizedData(object data)
    {
        var sanitizedData = SanitizeValueForSerialization(data);
        return sanitizedData.SerializeOptimized();
    }
}

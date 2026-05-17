namespace Acontplus.Core.Extensions;

/// <summary>
/// JSON serialization helpers built on top of <see cref="System.Text.Json"/>.
/// The three <c>*Options</c> fields are <c>static readonly</c> so that
/// <see cref="System.Text.Json.JsonSerializer"/> can cache its internal type metadata
/// across calls — recreating options on every access discards that cache.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Default options: camelCase, null-ignoring, comment-tolerant, enum-as-string.
    /// Suitable for the vast majority of API scenarios.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Pretty-print options: same as <see cref="DefaultOptions"/> with <c>WriteIndented = true</c>.
    /// Useful for logging or developer-facing output.
    /// </summary>
    public static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Strict options: case-sensitive, no trailing commas, no comments, never-ignore-null,
    /// strict number handling. Suitable for security-sensitive deserialization.
    /// </summary>
    public static readonly JsonSerializerOptions StrictOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.Strict
    };

    /// <summary>
    /// Deserializes JSON string to the specified type using optimized options.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentException">Thrown when JSON string is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when deserialization fails.</exception>
    public static T DeserializeOptimized<T>(this string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions)!;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to deserialize JSON to {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes JSON string to the specified type with fallback value on failure.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="fallback">The fallback value to return if deserialization fails.</param>
    /// <returns>The deserialized object or fallback value.</returns>
    public static T DeserializeSafe<T>(this string json, T fallback = default!)
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Serializes object to JSON string using optimized options.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="pretty">Whether to format the JSON with indentation.</param>
    /// <returns>The JSON string representation of the object.</returns>
    public static string SerializeOptimized<T>(this T obj, bool pretty = false)
    {
        if (obj == null)
            return "null";

        var options = pretty ? PrettyOptions : DefaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Serializes a dictionary to JSON string, converting all keys to camelCase.
    /// Useful for ensuring consistent JSON property naming when working with dynamic dictionaries.
    /// </summary>
    /// <param name="dictionary">The dictionary to serialize.</param>
    /// <param name="pretty">Whether to format the JSON with indentation.</param>
    /// <returns>The JSON string representation with camelCase keys.</returns>
    public static string SerializeWithCamelCaseKeys(this IReadOnlyDictionary<string, object>? dictionary, bool pretty = false)
    {
        if (dictionary == null || !dictionary.Any())
            return "null";

        var camelCaseDictionary = dictionary.ToDictionary(
            k => JsonNamingPolicy.CamelCase.ConvertName(k.Key),
            v => v.Value);

        return SerializeOptimized(camelCaseDictionary, pretty);
    }

    /// <summary>
    /// Creates a deep clone of an object via JSON serialization and deserialization.
    /// </summary>
    /// <typeparam name="T">The type of object to clone.</typeparam>
    /// <param name="obj">The object to clone.</param>
    /// <returns>A deep clone of the object.</returns>
    public static T CloneDeep<T>(this T obj)
    {
        if (obj == null)
            return default!;

        var json = JsonSerializer.Serialize(obj, DefaultOptions);
        return JsonSerializer.Deserialize<T>(json, DefaultOptions)!;
    }
}

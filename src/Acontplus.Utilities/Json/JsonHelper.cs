namespace Acontplus.Utilities.Json;

/// <summary>
/// Advanced JSON utilities building on core JSON functionality
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Validate JSON and return validation result with error details
    /// </summary>
    public static JsonValidationResult ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new JsonValidationResult(false, "JSON string is null or empty");

        try
        {
            using var document = JsonDocument.Parse(json);
            return new JsonValidationResult(true, null);
        }
        catch (JsonException ex)
        {
            return new JsonValidationResult(false, $"JSON parsing error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new JsonValidationResult(false, $"Invalid JSON format: {ex.Message}");
        }
    }

    /// <summary>
    /// Get JSON property value with strongly typed return
    /// </summary>
    public static T? GetJsonProperty<T>(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyName))
            return default;

        try
        {
            using var document = JsonDocument.Parse(json);

            // Try exact match first
            if (document.RootElement.TryGetProperty(propertyName, out var property))
            {
                return JsonSerializer.Deserialize<T>(property.GetRawText(), JsonExtensions.DefaultOptions);
            }

            // Try case-insensitive match
            foreach (var prop in document.RootElement.EnumerateObject()
                         .Where(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                return JsonSerializer.Deserialize<T>(prop.Value.GetRawText(), JsonExtensions.DefaultOptions);
            }

            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Merge two JSON objects (second object properties overwrite first)
    /// </summary>
    public static string MergeJson(string json1, string json2)
    {
        if (string.IsNullOrWhiteSpace(json1)) return json2 ?? "{}";
        if (string.IsNullOrWhiteSpace(json2)) return json1;

        try
        {
            using var doc1 = JsonDocument.Parse(json1);
            using var doc2 = JsonDocument.Parse(json2);

            var merged = new Dictionary<string, JsonElement>();

            // Add properties from first JSON
            foreach (var property in doc1.RootElement.EnumerateObject())
            {
                merged[property.Name] = property.Value;
            }

            // Overwrite with properties from second JSON
            foreach (var property in doc2.RootElement.EnumerateObject())
            {
                merged[property.Name] = property.Value;
            }

            return JsonSerializer.Serialize(merged, JsonExtensions.DefaultOptions);
        }
        catch (JsonException)
        {
            return json2; // Return second JSON if merge fails
        }
    }

    /// <summary>
    /// Compare two JSON strings for equality (ignoring property order)
    /// </summary>
    public static bool AreEqual(string json1, string json2)
    {
        if (string.IsNullOrWhiteSpace(json1) && string.IsNullOrWhiteSpace(json2))
            return true;

        if (string.IsNullOrWhiteSpace(json1) || string.IsNullOrWhiteSpace(json2))
            return false;

        try
        {
            using var doc1 = JsonDocument.Parse(json1);
            using var doc2 = JsonDocument.Parse(json2);

            var normalized1 = JsonSerializer.Serialize(doc1.RootElement, JsonExtensions.DefaultOptions);
            var normalized2 = JsonSerializer.Serialize(doc2.RootElement, JsonExtensions.DefaultOptions);

            return normalized1 == normalized2;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Result of JSON validation
    /// </summary>
    public record JsonValidationResult(bool IsValid, string? ErrorMessage);
}
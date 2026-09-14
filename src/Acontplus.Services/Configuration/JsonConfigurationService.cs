// AcontPlus.Services/Configuration/JsonConfigurationService.cs

namespace Acontplus.Services.Configuration;

/// <summary>
/// Service for configuring JSON serialization in applications
/// Provides centralized configuration for System.Text.Json across the application
/// </summary>
public static class JsonConfigurationService
{
    /// <summary>
    /// Get JSON serializer options with configurable settings
    /// </summary>
    /// <param name="prettyFormat">Whether to use pretty formatting</param>
    /// <param name="strictMode">Whether to use strict validation</param>
    /// <returns>Configured JsonSerializerOptions</returns>
    public static JsonSerializerOptions GetOptions(bool prettyFormat = false, bool strictMode = false) => new()
    {
        PropertyNameCaseInsensitive = !strictMode,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = !strictMode,
        ReadCommentHandling = strictMode ? JsonCommentHandling.Disallow : JsonCommentHandling.Skip,
        WriteIndented = prettyFormat,
        DefaultIgnoreCondition = strictMode ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = strictMode ? JsonNumberHandling.Strict : JsonNumberHandling.AllowReadingFromString,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <summary>
    /// Configure JSON options for ASP.NET Core applications
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="useStrictMode">Whether to use strict JSON validation</param>
    public static void ConfigureAspNetCore(IServiceCollection services, bool useStrictMode = false)
    {
        var jsonOptions = useStrictMode ? GetOptions(strictMode: true) : GetOptions();

        // Configure HTTP JSON options (for minimal APIs)
        services.ConfigureHttpJsonOptions(options =>
        {
            CopyOptionsTo(jsonOptions, options.SerializerOptions);
        });

        // Configure MVC JSON options (for controllers)
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                CopyOptionsTo(jsonOptions, options.JsonSerializerOptions);
            });
    }

    /// <summary>
    /// Configure JSON options for ASP.NET Core with custom environment settings
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="isDevelopment">Whether the application is in development mode</param>
    /// <param name="useStrictMode">Whether to use strict JSON validation</param>
    public static void ConfigureAspNetCore(IServiceCollection services, bool isDevelopment, bool useStrictMode = false)
    {
        var jsonOptions = useStrictMode ? GetOptions(strictMode: true) :
                         isDevelopment ? GetOptions(prettyFormat: true) : GetOptions();

        services.ConfigureHttpJsonOptions(options =>
        {
            CopyOptionsTo(jsonOptions, options.SerializerOptions);
        });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                CopyOptionsTo(jsonOptions, options.JsonSerializerOptions);
            });
    }

    /// <summary>
    /// Copy JSON serializer options from source to target
    /// </summary>
    /// <param name="source">Source options</param>
    /// <param name="target">Target options</param>
    private static void CopyOptionsTo(JsonSerializerOptions source, JsonSerializerOptions target)
    {
        target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
        target.PropertyNamingPolicy = source.PropertyNamingPolicy;
        target.AllowTrailingCommas = source.AllowTrailingCommas;
        target.ReadCommentHandling = source.ReadCommentHandling;
        target.WriteIndented = source.WriteIndented;
        target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
        target.NumberHandling = source.NumberHandling;

        // Copy converters
        foreach (var converter in source.Converters)
        {
            target.Converters.Add(converter);
        }
    }

    /// <summary>
    /// Register JSON configuration as a singleton service
    /// </summary>
    /// <param name="services">Service collection</param>
    public static void RegisterJsonConfiguration(IServiceCollection services)
    {
        services.AddSingleton(provider => GetOptions());
        services.AddSingleton<IJsonConfigurationProvider, JsonConfigurationProvider>();
    }
}

/// <summary>
/// Interface for JSON configuration provider
/// </summary>
public interface IJsonConfigurationProvider
{
    /// <summary>
    /// Gets JSON serializer options with configurable settings.
    /// </summary>
    /// <param name="prettyFormat">Whether to format the JSON output with indentation.</param>
    /// <param name="strictMode">Whether to enforce strict JSON reading and validation rules.</param>
    /// <returns>Configured <see cref="JsonSerializerOptions"/>.</returns>
    JsonSerializerOptions GetOptions(bool prettyFormat = false, bool strictMode = false);
}

/// <summary>
/// Implementation of JSON configuration provider
/// </summary>
public class JsonConfigurationProvider : IJsonConfigurationProvider
{
    /// <inheritdoc />
    public JsonSerializerOptions GetOptions(bool prettyFormat = false, bool strictMode = false) =>
        JsonConfigurationService.GetOptions(prettyFormat, strictMode);
}

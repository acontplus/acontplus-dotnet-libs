using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Acontplus.ApiDocumentation;

/// <summary>
/// Provides configuration for Swagger/OpenAPI generation options, supporting multiple API versions and dynamic metadata.
/// </summary>
/// <remarks>
/// This configurator creates a distinct Swagger document for each discovered API version, using metadata from both the API version description and the application's configuration (such as contact and license info).
/// </remarks>
/// <param name="provider">The API version description provider used to enumerate API versions.</param>
/// <param name="configuration">The application configuration for retrieving Swagger metadata.</param>
public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration) : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider = provider;
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Configures the <see cref="SwaggerGenOptions"/> for all discovered API versions.
    /// </summary>
    /// <param name="options">The Swagger generation options to configure.</param>
    public void Configure(SwaggerGenOptions options)
    {
        // Create a Swagger document for each discovered API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
        }
    }

    /// <summary>
    /// Configures the <see cref="SwaggerGenOptions"/> for a named options instance. Delegates to the main <see cref="Configure(SwaggerGenOptions)"/> method.
    /// </summary>
    /// <param name="name">The name of the options instance (not used).</param>
    /// <param name="options">The Swagger generation options to configure.</param>
    public void Configure(string? name, SwaggerGenOptions options) =>
        Configure(options);

    /// <summary>
    /// Creates the <see cref="OpenApiInfo"/> object for a given API version, including title, version, description, contact, and license information.
    /// </summary>
    /// <param name="description">The description of the API version.</param>
    /// <returns>An <see cref="OpenApiInfo"/> object with metadata for the Swagger document.</returns>
    private OpenApiInfo CreateVersionInfo(ApiVersionDescription description)
    {
        // Retrieve the SwaggerInfo section from appsettings.json
        var swaggerInfoSection = _configuration.GetSection("SwaggerInfo");
        var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "My API";

        var info = new OpenApiInfo
        {
            Title = $"{entryAssemblyName} v{description.ApiVersion}",
            Version = description.ApiVersion.ToString(),
            Description = description.IsDeprecated
                ? "This API version has been deprecated. Please use a newer version."
                : $"API documentation for {entryAssemblyName}.",
            Contact = new OpenApiContact
            {
                Name = swaggerInfoSection["ContactName"] ?? "Support Team",
                Email = swaggerInfoSection["ContactEmail"] ?? "support@example.com",
                Url = Uri.TryCreate(swaggerInfoSection["ContactUrl"], UriKind.Absolute, out var contactUri) ? contactUri : null
            },
            License = new OpenApiLicense
            {
                Name = swaggerInfoSection["LicenseName"] ?? "MIT",
                Url = Uri.TryCreate(swaggerInfoSection["LicenseUrl"], UriKind.Absolute, out var licenseUri) ? licenseUri : null
            }
        };

        return info;
    }
}

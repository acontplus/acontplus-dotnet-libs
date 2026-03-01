using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Acontplus.ApiDocumentation;

/// <summary>
/// Provides extension methods for configuring API versioning and Swagger/OpenAPI documentation in ASP.NET Core applications.
/// </summary>
/// <remarks>
/// These extensions enable standardized API versioning, JWT Bearer authentication in Swagger UI, and automatic inclusion of XML documentation comments.
/// </remarks>
public static class ApiDocumentationExtensions
{
    /// <summary>
    /// Adds and configures API versioning and Swagger/OpenAPI documentation services to the application's service collection.
    /// Supports both controller-based and Minimal API projects.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApiVersioningAndDocumentation(this IServiceCollection services)
    {
        // 1. Configure API Versioning for both Controllers and Minimal APIs
        var versioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            // Combine multiple version readers for flexibility
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new MediaTypeApiVersionReader("x-api-version")
            );
        });

        // Register MVC versioning support (for controller-based APIs)
        versioningBuilder.AddMvc();

        // Register API Explorer for BOTH controllers and Minimal APIs.
        // NOTE: Calling AddApiExplorer() directly on the builder (not chained from AddMvc())
        // ensures the IApiVersionDescriptionProvider can discover versions from Minimal API
        // endpoint version sets (app.NewApiVersionSet()) as well as MVC controller attributes.
        versioningBuilder.AddApiExplorer(options =>
        {
            // Format the version as "vX" (e.g., "v1", "v2")
            options.GroupNameFormat = "'v'V";
            // Automatically substitute the API version in route templates
            options.SubstituteApiVersionInUrl = true;
        });

        // 2. Add our custom options configurator for Swagger
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        // 3. Configure Swagger Generator
        services.AddSwaggerGen(options =>
        {
            // Enable JWT Bearer token authentication in the Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = []
            });

            // Include XML comments from all assemblies in the base directory
            var baseDirectory = AppContext.BaseDirectory;
            var xmlFiles = Directory.EnumerateFiles(baseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var xmlFile in xmlFiles)
            {
                options.IncludeXmlComments(xmlFile);
            }
        });

        return services;
    }

    /// <summary>
    /// Configures the application pipeline to use Swagger and the Swagger UI with versioning support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Preferred overload for Minimal APIs:</strong> call the <c>WebApplication</c> overload
    /// instead — it uses <c>app.DescribeApiVersions()</c> which reads the live endpoint data sources
    /// directly and always reflects every version registered via <c>app.NewApiVersionSet()</c>.
    /// This <c>IApplicationBuilder</c> overload falls back to <see cref="IApiVersionDescriptionProvider"/>
    /// which is a cached singleton and may miss versions registered by Minimal API endpoints.
    /// </para>
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseApiVersioningAndDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();
            var descriptions = provider.ApiVersionDescriptions;

            if (descriptions.Count == 0)
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
                return;
            }

            foreach (var description in descriptions.Reverse())
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });

        return app;
    }

    /// <summary>
    /// Configures the application pipeline to use Swagger and the Swagger UI with versioning support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload is the <strong>preferred choice for Minimal APIs</strong>. It calls
    /// <c>app.DescribeApiVersions()</c> which scans the live endpoint data sources
    /// rather than the cached <see cref="IApiVersionDescriptionProvider"/> singleton. This guarantees
    /// that every version set registered via <c>app.NewApiVersionSet()</c> appears in the Swagger UI
    /// dropdown regardless of when this method is called relative to DI initialisation.
    /// </para>
    /// <para>
    /// <strong>Call order:</strong> invoke this method <em>after</em> all <c>app.MapXxx()</c> calls so
    /// that endpoint version metadata is present in the data sources when the discovery runs.
    /// </para>
    /// </remarks>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <returns>The same <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication UseApiVersioningAndDocumentation(this WebApplication app)
    {
        app.UseSwagger();

        // DescribeApiVersions() reads directly from IEndpointRouteBuilder.DataSources.
        // Unlike IApiVersionDescriptionProvider (a cached singleton), this reflects the
        // actual endpoint version sets at the moment it is called, which is exactly what
        // we need when the caller has just finished registering all Minimal API endpoints.
        var descriptions = app.DescribeApiVersions();

        app.UseSwaggerUI(options =>
        {
            foreach (var description in descriptions.Reverse())
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });

        return app;
    }
}

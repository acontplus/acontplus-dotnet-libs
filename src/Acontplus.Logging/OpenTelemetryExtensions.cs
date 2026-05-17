using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Acontplus.Logging;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry observability (tracing, metrics, and logging).
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configures OpenTelemetry tracing, metrics, and logging with the specified options.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddAdvancedOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new OpenTelemetryOptions();
        configuration.GetSection("OpenTelemetry").Bind(options);

        if (!options.Enabled)
        {
            return services;
        }

        // Auto-detect service name from assembly if not configured
        if (string.IsNullOrWhiteSpace(options.ServiceName) || options.ServiceName == "MyService")
        {
            options.ServiceName = GetServiceNameFromAssembly();
        }

        // Auto-detect service version from assembly if not configured
        if (string.IsNullOrWhiteSpace(options.ServiceVersion))
        {
            options.ServiceVersion = GetServiceVersionFromAssembly();
        }

        // Register options for DI
        services.AddSingleton(options);

        // Configure resource attributes
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion ?? "1.0.0",
                serviceNamespace: options.ServiceNamespace);

        // Detect if any Dynatrace exporter is active.
        // UseOtlpExporter cannot coexist with per-signal AddOtlpExporter, so we choose one strategy.
        var hasDynatrace = options.Tracing.EnableDynatraceExporter
            || options.Metrics.EnableDynatraceExporter
            || options.Logging.EnableDynatraceExporter;

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddAttributes(resourceBuilder.Build().Attributes))
            .WithTracing(builder => ConfigureTracing(builder, options, resourceBuilder, hasDynatrace))
            .WithMetrics(builder => ConfigureMetrics(builder, options, resourceBuilder, hasDynatrace));

        if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            if (hasDynatrace)
            {
                // Cannot use UseOtlpExporter alongside per-signal AddOtlpExporter (used by Dynatrace).
                // OTLP was already added inside ConfigureTracing/ConfigureMetrics.
                // Handle logging OTLP + Dynatrace via WithLogging.
                otelBuilder.WithLogging(builder => ConfigureLogging(builder, options));
            }
            else
            {
                // UseOtlpExporter registers OTLP for all three signals (traces, metrics, logs) in one call.
                var protocol = options.OtlpProtocol.ToLowerInvariant() == "http"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
                otelBuilder.UseOtlpExporter(protocol, new Uri(options.OtlpEndpoint));
            }
        }
        else if (options.Logging.EnableDynatraceExporter)
        {
            otelBuilder.WithLogging(builder => ConfigureLogging(builder, options));
        }

        return services;
    }

    /// <summary>
    /// Configures distributed tracing with OpenTelemetry.
    /// </summary>
    private static void ConfigureTracing(
        TracerProviderBuilder builder,
        OpenTelemetryOptions options,
        ResourceBuilder resourceBuilder,
        bool hasDynatrace)
    {
        if (!options.Tracing.Enabled)
        {
            return;
        }

        builder.SetResourceBuilder(resourceBuilder);

        // Add default activity source for the service
        builder.AddSource(options.ServiceName);

        // Add additional activity sources
        foreach (var source in options.Tracing.AdditionalSources)
        {
            builder.AddSource(source);
        }

        // Add automatic instrumentation
        if (options.Tracing.EnableAspNetCoreInstrumentation)
        {
            builder.AddAspNetCoreInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.scheme", request.Scheme);
                    activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                };
                opts.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("http.response_content_length", response.ContentLength);
                };
            });
        }

        if (options.Tracing.EnableHttpClientInstrumentation)
        {
            builder.AddHttpClientInstrumentation(opts =>
            {
                opts.RecordException = true;
                opts.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request_content_length", request.Content?.Headers.ContentLength);
                };
                opts.EnrichWithHttpResponseMessage = (activity, response) =>
                {
                    activity.SetTag("http.response_content_length", response.Content?.Headers.ContentLength);
                };
            });
        }

        if (options.Tracing.EnableSqlClientInstrumentation)
        {
            builder.AddSqlClientInstrumentation(opts =>
            {
                opts.RecordException = true;
            });
        }

        // Configure exporters
        if (options.Tracing.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        // When Dynatrace is also configured, UseOtlpExporter cannot be used globally.
        // Add OTLP per-signal here so both OTLP and Dynatrace exporters are active.
        if (hasDynatrace && options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
                otlpOptions.Protocol = options.OtlpProtocol.ToLowerInvariant() == "http"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
            });
        }

        // Dynatrace: Uses OTLP protocol with specific headers
        if (options.Tracing.EnableDynatraceExporter && !string.IsNullOrEmpty(options.Tracing.DynatraceEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.Tracing.DynatraceEndpoint);
                otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;

                // Add Dynatrace API token header if provided
                if (!string.IsNullOrEmpty(options.Tracing.DynatraceApiToken))
                {
                    otlpOptions.Headers = $"Authorization=Api-Token {options.Tracing.DynatraceApiToken}";
                }
            });
        }

        // Configure sampling and processing
        builder.SetSampler(new AlwaysOnSampler());
    }

    /// <summary>
    /// Configures metrics collection with OpenTelemetry.
    /// </summary>
    private static void ConfigureMetrics(
        MeterProviderBuilder builder,
        OpenTelemetryOptions options,
        ResourceBuilder resourceBuilder,
        bool hasDynatrace)
    {
        if (!options.Metrics.Enabled)
        {
            return;
        }

        builder.SetResourceBuilder(resourceBuilder);

        // Add default meter for the service
        builder.AddMeter(options.ServiceName);

        // Add additional meters
        foreach (var meter in options.Metrics.AdditionalMeters)
        {
            builder.AddMeter(meter);
        }

        // Add automatic instrumentation
        if (options.Metrics.EnableAspNetCoreInstrumentation)
        {
            builder.AddAspNetCoreInstrumentation();
        }

        if (options.Metrics.EnableHttpClientInstrumentation)
        {
            builder.AddHttpClientInstrumentation();
        }

        // Configure exporters
        if (options.Metrics.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        // When Dynatrace is also configured, UseOtlpExporter cannot be used globally.
        // Add OTLP per-signal here so both OTLP and Dynatrace exporters are active.
        if (hasDynatrace && options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
                otlpOptions.Protocol = options.OtlpProtocol.ToLowerInvariant() == "http"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
            });
        }

        // Dynatrace: Uses OTLP protocol with specific headers
        if (options.Metrics.EnableDynatraceExporter && !string.IsNullOrEmpty(options.Metrics.DynatraceEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.Metrics.DynatraceEndpoint);
                otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;

                // Add Dynatrace API token header if provided
                if (!string.IsNullOrEmpty(options.Metrics.DynatraceApiToken))
                {
                    otlpOptions.Headers = $"Authorization=Api-Token {options.Metrics.DynatraceApiToken}";
                }
            });
        }
    }

    /// <summary>
    /// Configures OpenTelemetry log export.
    /// </summary>
    private static void ConfigureLogging(
        LoggerProviderBuilder builder,
        OpenTelemetryOptions options)
    {
        // OTLP logging: only used when hasDynatrace forces per-signal mode (UseOtlpExporter not available).
        if (options.EnableOtlpExporter && !string.IsNullOrEmpty(options.OtlpEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpEndpoint);
                otlpOptions.Protocol = options.OtlpProtocol.ToLowerInvariant() == "http"
                    ? OtlpExportProtocol.HttpProtobuf
                    : OtlpExportProtocol.Grpc;
            });
        }

        if (options.Logging.EnableDynatraceExporter && !string.IsNullOrEmpty(options.Logging.DynatraceEndpoint))
        {
            builder.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.Logging.DynatraceEndpoint);
                otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (!string.IsNullOrEmpty(options.Logging.DynatraceApiToken))
                {
                    otlpOptions.Headers = $"Authorization=Api-Token {options.Logging.DynatraceApiToken}";
                }
            });
        }
    }

    /// <summary>
    /// Gets the service name from the entry assembly.
    /// </summary>
    /// <returns>The service name derived from the assembly name.</returns>
    private static string GetServiceNameFromAssembly()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            var assemblyName = assembly.GetName().Name;
            if (!string.IsNullOrWhiteSpace(assemblyName))
            {
                return assemblyName;
            }
        }
        return "UnknownService";
    }

    /// <summary>
    /// Gets the service version from the entry assembly.
    /// </summary>
    /// <returns>The service version from the assembly version.</returns>
    private static string GetServiceVersionFromAssembly()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
        {
            var version = assembly.GetName().Version;
            if (version != null)
            {
                return version.ToString();
            }

            // Try to get from InformationalVersion attribute
            var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (infoVersionAttr != null && !string.IsNullOrWhiteSpace(infoVersionAttr.InformationalVersion))
            {
                return infoVersionAttr.InformationalVersion;
            }
        }
        return "1.0.0";
    }

    /// <summary>
    /// Registers an ActivitySource for distributed tracing in the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sourceName">The name of the activity source (typically the service name).</param>
    /// <param name="version">The version of the activity source.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddActivitySource(
        this IServiceCollection services,
        string sourceName,
        string? version = null)
    {
        services.AddSingleton(_ => new ActivitySource(sourceName, version));
        return services;
    }

    /// <summary>
    /// Registers a Meter for metrics collection in the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="meterName">The name of the meter (typically the service name).</param>
    /// <param name="version">The version of the meter.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddMeter(
        this IServiceCollection services,
        string meterName,
        string? version = null)
    {
        services.AddSingleton(_ => new Meter(meterName, version));
        return services;
    }
}

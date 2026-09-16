using Acontplus.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acontplus.Logging.Tests.Unit;

public class OpenTelemetryExtensionsTests
{
    [Fact]
    public void AddAdvancedOpenTelemetry_WithOtelExporterEndpoint_AutoEnablesOpenTelemetryAndOtlp()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317",
                ["OTEL_SERVICE_NAME"] = "test-aspire-service"
            })
            .Build();

        // Act
        services.AddAdvancedOpenTelemetry(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<OpenTelemetryOptions>();
        Assert.NotNull(options);
        Assert.True(options.Enabled);
        Assert.True(options.EnableOtlpExporter);
        Assert.Equal("http://localhost:4317", options.OtlpEndpoint);
        Assert.Equal("test-aspire-service", options.ServiceName);
    }

    [Fact]
    public void AddAdvancedOpenTelemetry_WhenExplicitlyDisabled_DoesNotRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:Enabled"] = "false",
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317"
            })
            .Build();

        // Act
        services.AddAdvancedOpenTelemetry(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<OpenTelemetryOptions>();
        Assert.Null(options);
    }

    [Fact]
    public void AddAdvancedOpenTelemetry_WithHttpProtobufProtocol_ConfiguresProtocol()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf"
            })
            .Build();

        // Act
        services.AddAdvancedOpenTelemetry(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<OpenTelemetryOptions>();
        Assert.NotNull(options);
        Assert.Equal("http/protobuf", options.OtlpProtocol);
    }
}

# Acontplus.Logging

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Logging.svg)](https://www.nuget.org/packages/Acontplus.Logging)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)


## Description
`Acontplus.Logging` is an advanced logging library for .NET applications, built on top of Serilog. It provides enterprise-grade logging capabilities with support for multiple sinks (console, file, database, Elasticsearch), structured logging, custom enrichers, and cloud-native observability patterns. Seamlessly integrates with the .NET Generic Host and ASP.NET Core applications.

## Key Features

### 🔧 **Multi-Sink Architecture**
- **Console Logging**: Development-friendly output with color coding
- **File Logging**: Rolling file support with configurable retention
- **Database Logging**: SQL Server integration for structured querying
- **Elasticsearch Integration**: Official ELK stack support with ECS compliance

### 📊 **Structured Logging**
- JSON formatting for production environments
- Custom timezone enrichers
- Environment-aware configurations
- Async logging for optimal performance

### 🚀 **Enterprise Features**
- **ELK Stack Integration**: Official Elastic.Serilog.Sinks package
- **ECS Compliance**: Elastic Common Schema adherence
- **Data Streams**: Modern Elasticsearch data stream support
- **ILM Integration**: Index Lifecycle Management ready
- **High Performance**: Optimized for production workloads

### ⚙️ **Easy Configuration**
- Simple JSON configuration
- Environment-specific settings
- Dependency injection ready
- Bootstrap logger support

## Installation
To install the library, run the following command in the NuGet Package Manager Console:
```bash
Install-Package Acontplus.Logging
```
Or using the .NET CLI:
```bash
dotnet add package Acontplus.Logging
```
Additionally, ensure you have the appropriate Serilog integration package for your host:
- For ASP.NET Core Web APIs (with `WebApplication.CreateBuilder`):
  ```bash
  dotnet add package Serilog.AspNetCore
  ```
- For Generic Hosts (like Worker Services):
  ```bash
  dotnet add package Serilog.Extensions.Hosting
  ```

## Configuration

Configure the logging system by adding the `AdvancedLogging` section to your `appsettings.json`:

```json
{
  "AdvancedLogging": {
    "EnableLocalFile": true,
    "Shared": false,
    "Buffered": true,
    "LocalFilePath": "logs/log-.log",
    "RollingInterval": "Day",
    "RetainedFileCountLimit": 7,
    "FileSizeLimitBytes": 10485760,
    "EnableDatabaseLogging": false,
    "DatabaseConnectionString": "Server=...",
    "EnableElasticsearchLogging": false,
    "ElasticsearchUrl": "http://localhost:9200",
    "ElasticsearchIndexFormat": "logs-{0:yyyy.MM.dd}",
    "ElasticsearchUsername": "elastic",
    "ElasticsearchPassword": "your-password",
    "TimeZoneId": "America/Guayaquil"
  }
}
```

### Configuration Options

#### **File Logging**
- **EnableLocalFile** *(bool)*: Enables or disables storing logs in local files.
- **Shared** *(bool)*: Enables or disables shared log files (multiple processes can write to the same file).
- **Buffered** *(bool)*: Enables or disables buffered logging for local files (improves performance by writing in chunks).
- **LocalFilePath** *(string)*: Path to the log file. Supports rolling file patterns.
- **RollingInterval** *(string)*: Interval to roll log files. Values: `Year`, `Month`, `Day`, `Hour`, `Minute`.
- **RetainedFileCountLimit** *(int)*: Number of historical log files to keep.
- **FileSizeLimitBytes** *(int)*: Maximum size of a single log file in bytes before it rolls over.

#### **Database Logging**
- **EnableDatabaseLogging** *(bool)*: Enables or disables storing logs in a database.
- **DatabaseConnectionString** *(string)*: Connection string to the database where logs will be stored.

#### **Elasticsearch Logging**
- **EnableElasticsearchLogging** *(bool)*: Enables or disables storing logs in Elasticsearch for ELK stack integration.
- **ElasticsearchUrl** *(string)*: URL of the Elasticsearch instance (e.g., "http://localhost:9200").
- **ElasticsearchIndexFormat** *(string)*: Index format for Elasticsearch (default: "logs-{0:yyyy.MM.dd}").
- **ElasticsearchUsername** *(string)*: Username for Elasticsearch authentication (optional).
- **ElasticsearchPassword** *(string)*: Password for Elasticsearch authentication (optional).

#### **General Settings**
- **TimeZoneId** *(string)*: Time zone ID for the custom timestamp enricher (e.g., "America/Guayaquil", "UTC").

## Usage

### Basic Integration

Integrate `Acontplus.Logging` in your `Program.cs`:

```csharp
using Acontplus.Logging;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        // Bootstrap logger for early startup issues
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            var environment = builder.Environment.EnvironmentName;

            // Configure Serilog with advanced logging
            builder.Host.UseSerilog((hostContext, services, loggerConfiguration) =>
            {
                loggerConfiguration.ConfigureAdvancedLogger(hostContext.Configuration, environment);
                loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);
                loggerConfiguration.ReadFrom.Services(services);
            });

            // Register logging options
            builder.Services.AddAdvancedLoggingOptions(builder.Configuration);

            var app = builder.Build();

            // Add request logging middleware
            app.UseSerilogRequestLogging();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

### Advanced Configuration

For more advanced Serilog configuration, add a `Serilog` section to your `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "Enrich": [
      "FromLogContext",
      "WithEnvironmentUserName",
      "WithMachineName"
    ],
    "Properties": {
      "Application": "YourAppName"
    }
  }
}
```

## Requirements
- .NET 10 or higher
- Proper write permissions if `EnableLocalFile` is enabled
- Accessible database if `EnableDatabaseLogging` is enabled
- Elasticsearch 8.x+ instance if `EnableElasticsearchLogging` is enabled

## ELK Stack Integration

This library provides seamless integration with the ELK (Elasticsearch, Logstash, Kibana) stack for advanced log management and analytics:

### Benefits of ELK Integration:
- **Centralized Log Management**: Aggregate logs from multiple services
- **Advanced Search & Analytics**: Powerful querying capabilities with Elasticsearch
- **Real-time Monitoring**: Live dashboards and alerts with Kibana
- **Scalability**: Handle high-volume logging with distributed architecture
- **Visualization**: Create custom dashboards and reports
- **Alerting**: Set up automated alerts based on log patterns

### Setup Instructions:
1. **Install Elasticsearch**: Deploy Elasticsearch 8.x+ on your infrastructure
2. **Configure Kibana**: Set up Kibana for visualization and monitoring
3. **Enable Logging**: Set `EnableElasticsearchLogging: true` in your configuration
4. **Configure Connection**: Provide Elasticsearch URL and credentials
5. **Monitor**: Access Kibana to view and analyze your logs

### Example ELK Configuration:
```json
{
  "AdvancedLogging": {
    "EnableElasticsearchLogging": true,
    "ElasticsearchUrl": "https://your-elasticsearch-cluster:9200",
    "ElasticsearchIndexFormat": "acontplus-logs-{0:yyyy.MM.dd}",
    "ElasticsearchUsername": "elastic",
    "ElasticsearchPassword": "your-secure-password"
  }
}
```

**Note:** The library uses `Elastic.Serilog.Sinks` package which provides better integration with Elasticsearch 8.x and newer versions. This is the **official** Elastic package that adheres to newer best practices around logging, datastreams and ILM. For advanced configuration options, refer to the [Elastic.Serilog.Sinks documentation](https://www.nuget.org/packages/Elastic.Serilog.Sinks/).

### Key Benefits of Elastic.Serilog.Sinks:
- **Official Elastic Support**: Maintained by Elastic team
- **ECS Compliance**: Adheres to Elastic Common Schema
- **Data Streams**: Uses modern Elasticsearch data streams
- **ILM Integration**: Built-in Index Lifecycle Management support
- **Performance**: Optimized for high-throughput logging

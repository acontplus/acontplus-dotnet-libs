# Acontplus.Logging

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Logging.svg)](https://www.nuget.org/packages/Acontplus.Logging)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## Description
`Acontplus.Logging` is a comprehensive **observability library** for .NET applications, combining **Serilog** for structured logging with **OpenTelemetry** for distributed tracing and metrics. It provides enterprise-grade observability capabilities with support for multiple backends including Jaeger, Dynatrace, Prometheus, Elasticsearch, and more. Perfect for cloud-native, microservices, and distributed architectures.

## Features

### 📊 **OpenTelemetry Observability**
- **Distributed Tracing**: Track request flows across services with context propagation
- **Custom Metrics**: Counters, histograms, gauges for business and technical metrics
- **Activity Sources**: Create custom spans for detailed operation tracking
- **Automatic Instrumentation**: ASP.NET Core, HTTP clients, SQL database calls
- **Multiple Exporters**: OTLP (Jaeger, Grafana Cloud, etc.), Console, Dynatrace
- **Single OTLP config**: `EnableOtlpExporter` at root covers traces, metrics, and logs — no duplication per signal
- **Auto-Detection**: ServiceName and ServiceVersion from assembly metadata

### 🔧 **Structured Logging (Serilog)**
- **Multi-Sink Architecture**: Console, File, Database, Elasticsearch
- **JSON Formatting**: Production-ready structured logs
- **Custom Enrichers**: Timezone, environment, machine name
- **Async Logging**: High-performance async sinks
- **Rolling Files**: Automatic log rotation and retention

### 🌐 **Backend Integration**
- **Jaeger**: Distributed tracing UI and analysis via OTLP protocol
- **Grafana Cloud / Tempo**: Traces and metrics with a free tier
- **Dynatrace**: Enterprise APM with AI-powered insights (per-signal OTLP endpoints)
- **Prometheus**: Metrics via OpenTelemetry Collector → Prometheus scrape
- **Grafana**: Unified dashboards for metrics and traces
- **Elasticsearch**: ELK stack integration for log analytics
- **OTLP**: Vendor-neutral telemetry protocol

### ⚡ **Enterprise Features**
- **ELK Stack Support**: Official Elastic.Serilog.Sinks package
- **ECS Compliance**: Elastic Common Schema adherence
- **W3C Trace Context**: Standard context propagation
- **Sampling Strategies**: Configurable trace sampling
- **Resource Attributes**: Service name, version, namespace
- **Exception Tracking**: Automatic exception recording in traces

## Installation

Install the library via NuGet:

```bash
dotnet add package Acontplus.Logging
```

For ASP.NET Core applications, also install:
```bash
dotnet add package Serilog.AspNetCore
```

For Worker Services or Generic Hosts:
```bash
dotnet add package Serilog.Extensions.Hosting
```

## Quick Start

### 1. Basic Setup with OpenTelemetry

```csharp
using Acontplus.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ConfigureAdvancedLogger(context.Configuration, builder.Environment.EnvironmentName);
});

// Configure OpenTelemetry (tracing + metrics)
builder.Services.AddAdvancedOpenTelemetry(builder.Configuration);

// ServiceName is auto-detected from assembly name if not configured
// ServiceVersion is auto-detected from assembly version if not configured

// Register ActivitySource and Meter for custom instrumentation
builder.Services.AddActivitySource("YourServiceName"); // Optional: overrides auto-detected name
builder.Services.AddMeter("YourServiceName"); // Optional: overrides auto-detected name

// Optional: Register helpers for easier usage
builder.Services.AddSingleton<TracingHelper>();
builder.Services.AddSingleton<MetricsHelper>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapControllers();
app.Run();
```

### 2. Configuration (appsettings.json)

```json
{
  "AdvancedLogging": {
    "EnableLocalFile": true,
    "LocalFilePath": "logs/log-.log",
    "RollingInterval": "Day",
    "RetainedFileCountLimit": 7,
    "Formatter": "Json",
    "EnableElasticsearchLogging": true,
    "ElasticsearchUrl": "http://localhost:9200",
    "TimeZoneId": "UTC"
  },

  "OpenTelemetry": {
    "Enabled": true,

    // OTLP configured once at root — UseOtlpExporter covers traces, metrics, AND logs in one call.
    // ServiceName and ServiceVersion are auto-detected from assembly if not specified.
    "EnableOtlpExporter": true,
    "OtlpEndpoint": "http://localhost:4317",  // gRPC; use http://localhost:4318 for HTTP/protobuf
    "OtlpProtocol": "grpc",                   // grpc (default) or http
    "ServiceNamespace": "YourNamespace",

    "Tracing": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableSqlClientInstrumentation": true
    },

    "Metrics": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableRuntimeInstrumentation": true
    }
  }
}
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

## Usage Examples

### Distributed Tracing

```csharp
using System.Diagnostics;

public class OrderService
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ActivitySource activitySource, ILogger<OrderService> logger)
    {
        _activitySource = activitySource;
        _logger = logger;
    }

    public async Task<Order> ProcessOrderAsync(int orderId)
    {
        // Create a custom span for this operation
        using var activity = _activitySource.StartActivity("ProcessOrder");

        // Add tags for filtering and analysis
        activity?.SetTag("order.id", orderId);
        activity?.SetTag("order.priority", "high");

        _logger.LogInformation("Processing order {OrderId}", orderId);

        try
        {
            // Simulate processing
            var order = await GetOrderAsync(orderId);
            await ValidateOrderAsync(order);
            await ChargePaymentAsync(order);

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("OrderCompleted"));

            return order;
        }
        catch (Exception ex)
        {
            // Record exception in trace
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);

            _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
            throw;
        }
    }

    private async Task<Order> GetOrderAsync(int orderId)
    {
        // This creates a child span automatically
        using var activity = _activitySource.StartActivity("GetOrder");
        activity?.SetTag("db.system", "sqlserver");

        // Database call (auto-instrumented if SQL instrumentation enabled)
        return await _dbContext.Orders.FindAsync(orderId);
    }
}
```

### Custom Metrics

```csharp
using System.Diagnostics.Metrics;

public class PaymentService
{
    private readonly Counter<long> _paymentsProcessed;
    private readonly Histogram<double> _paymentAmount;
    private readonly Histogram<double> _processingDuration;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(Meter meter, ILogger<PaymentService> logger)
    {
        _logger = logger;

        // Create custom metrics
        _paymentsProcessed = meter.CreateCounter<long>(
            "payments.processed",
            "payments",
            "Total number of payments processed");

        _paymentAmount = meter.CreateHistogram<double>(
            "payment.amount",
            "USD",
            "Payment amount in USD");

        _processingDuration = meter.CreateHistogram<double>(
            "payment.processing.duration",
            "ms",
            "Payment processing duration");
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await ChargePaymentGatewayAsync(payment);

            // Record metrics
            _paymentsProcessed.Add(1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("gateway", payment.Gateway));

            _paymentAmount.Record(payment.Amount,
                new KeyValuePair<string, object?>("currency", payment.Currency),
                new KeyValuePair<string, object?>("type", payment.Type));

            _processingDuration.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("gateway", payment.Gateway));

            _logger.LogInformation("Payment processed successfully: {PaymentId}", payment.Id);
            return result;
        }
        catch (Exception ex)
        {
            _paymentsProcessed.Add(1,
                new KeyValuePair<string, object?>("status", "failed"),
                new KeyValuePair<string, object?>("gateway", payment.Gateway));

            _logger.LogError(ex, "Payment processing failed: {PaymentId}", payment.Id);
            throw;
        }
    }
}
```

### Using Helper Classes

```csharp
public class InventoryService
{
    private readonly TracingHelper _tracing;
    private readonly MetricsHelper _metrics;
    private readonly Counter<long> _stockUpdates;

    public InventoryService(TracingHelper tracing, MetricsHelper metrics)
    {
        _tracing = tracing;
        _metrics = metrics;
        _stockUpdates = _metrics.CreateCounter<long>("inventory.stock.updates", "updates");
    }

    public async Task UpdateStockAsync(int productId, int quantity)
    {
        using var activity = _tracing.StartActivity("UpdateStock");
        _tracing.AddTag("product.id", productId);
        _tracing.AddTag("quantity", quantity);

        try
        {
            await _repository.UpdateStockAsync(productId, quantity);

            _stockUpdates.Add(1,
                new KeyValuePair<string, object?>("product_id", productId),
                new KeyValuePair<string, object?>("operation", "update"));

            _tracing.AddEvent("StockUpdated");
        }
        catch (Exception ex)
        {
            _tracing.RecordException(ex);
            throw;
        }
    }
}
```

## Observability Backends Setup

### Jaeger (Distributed Tracing)

**Jaeger** provides distributed tracing with interactive dashboards for analyzing request flows across microservices.

#### Docker (recommended — includes UI + OTLP receiver):
```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  -p 4318:4318 \
  jaegertracing/jaeger:latest
```

#### Configuration (appsettings.json):
```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "EnableOtlpExporter": true,
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc"
  }
}
```

> `UseOtlpExporter` sends traces, metrics, **and** logs to Jaeger automatically — no per-signal config needed.

#### Access Jaeger UI:
- Open browser: http://localhost:16686
- Search for traces by service name, operation, tags
- Analyze trace timelines and dependencies

**Benefits:**
- 🔍 Visual trace timeline with span details
- 🌐 Service dependency graph
- 📊 Operation statistics and latencies
- 🔗 Trace comparison and analysis

---

### Dynatrace (Enterprise APM Platform)

**Dynatrace** is a comprehensive enterprise observability platform with AI-powered insights, automatic discovery, and full-stack monitoring. It supports traces, metrics, and logs via OTLP protocol.

#### Setup:

1. **Get Dynatrace Environment**:
   - Sign up for Dynatrace SaaS: https://www.dynatrace.com/trial/
   - Note your environment ID: `{your-environment-id}.live.dynatrace.com`

2. **Create API Token**:
   - Go to Settings → Integration → Dynatrace API
   - Create token with permissions: `openTelemetryTrace.ingest`, `metrics.ingest`, `logs.ingest`
   - Copy the API token

#### Configuration (appsettings.json):

> **Note**: Dynatrace requires a different OTLP endpoint per signal (traces/metrics/logs). The library
> detects this automatically and uses per-signal `AddOtlpExporter` instead of the global `UseOtlpExporter`.
> You can also combine `EnableOtlpExporter` (e.g., for Jaeger) with Dynatrace simultaneously.

```json
{
  "OpenTelemetry": {
    "Enabled": true,

    "Tracing": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableSqlClientInstrumentation": true,

      "EnableDynatraceExporter": true,
      "DynatraceEndpoint": "https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/traces",
      "DynatraceApiToken": "dt0c01.***.***.***"
    },

    "Metrics": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableRuntimeInstrumentation": true,

      "EnableDynatraceExporter": true,
      "DynatraceEndpoint": "https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/metrics",
      "DynatraceApiToken": "dt0c01.***.***.***"
    },

    "Logging": {
      "EnableDynatraceExporter": true,
      "DynatraceEndpoint": "https://{your-environment-id}.live.dynatrace.com/api/v2/otlp/v1/logs",
      "DynatraceApiToken": "dt0c01.***.***.***"
    }
  }
}
```

#### Access Dynatrace:
- Open your Dynatrace environment: `https://{your-environment-id}.live.dynatrace.com`
- Navigate to:
  - **Distributed traces**: Applications → Distributed traces
  - **Service flow**: Applications → Service flow
  - **Metrics**: Observe and explore → Metrics
  - **Logs**: Observe and explore → Logs

**Benefits:**
- 🤖 **AI-powered insights**: Automatic problem detection and root cause analysis
- 🔍 **Full-stack observability**: From frontend to database
- 📊 **Smart dashboards**: Pre-built and customizable dashboards
- 🎯 **Service dependency mapping**: Automatic service topology
- 🚨 **Intelligent alerting**: AI-driven anomaly detection
- 📈 **Business analytics**: Custom metrics and business KPIs
- 🔒 **Enterprise security**: SOC 2, ISO 27001 certified

---

### Prometheus + Grafana (Metrics & Dashboards)

Use OTLP exporter with OpenTelemetry Collector to expose Prometheus metrics endpoint.

#### 1. Run OpenTelemetry Collector with Prometheus:

Create `otel-collector-config.yaml`:
```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
  logging:
    loglevel: debug

service:
  pipelines:
    metrics:
      receivers: [otlp]
      exporters: [prometheus, logging]
```

Run OpenTelemetry Collector:
```bash
docker run -d --name otel-collector \
  -p 4317:4317 -p 4318:4318 -p 8889:8889 \
  -v $(pwd)/otel-collector-config.yaml:/etc/otel-collector-config.yaml \
  otel/opentelemetry-collector:latest \
  --config=/etc/otel-collector-config.yaml
```

#### 2. Run Prometheus:

Create `prometheus.yml`:
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['host.docker.internal:8889']
```

Run Prometheus:
```bash
docker run -d --name prometheus \
  -p 9090:9090 \
  -v $(pwd)/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus:latest
```

#### 3. Run Grafana:
```bash
docker run -d --name grafana \
  -p 3000:3000 \
  grafana/grafana:latest
```

#### 4. Application Configuration:
```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "EnableOtlpExporter": true,
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc"
  }
}
```

#### 5. Configure Grafana:
- Open http://localhost:3000 (admin/admin)
- Add Prometheus data source: http://prometheus:9090
- Import dashboard or create custom panels
- Visualize: request rates, latencies, error rates, custom metrics

**Recommended Metrics to Monitor:**
- `http_server_request_duration` - Request latencies
- `http_server_active_requests` - Current active requests
- Custom business metrics from your application

---

### Complete Observability Stack (All-in-One)

#### Docker Compose Setup:

Create `docker-compose.yml`:
```yaml
version: '3.8'

services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    depends_on:
      - prometheus

  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    ports:
      - "9200:9200"
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    ports:
      - "5601:5601"
    depends_on:
      - elasticsearch
```

Run the stack:
```bash
docker-compose up -d
```

#### Access Points:
- **Jaeger UI**: http://localhost:16686 (Traces)
- **Grafana**: http://localhost:3000 (Metrics)
- **Prometheus**: http://localhost:9090 (Metrics backend)
- **Kibana**: http://localhost:5601 (Logs)
- **Elasticsearch**: http://localhost:9200 (Logs backend)

#### Full Configuration:
```json
{
  "AdvancedLogging": {
    "EnableLocalFile": true,
    "EnableElasticsearchLogging": true,
    "ElasticsearchUrl": "http://localhost:9200",
    "Formatter": "Json"
  },

  "OpenTelemetry": {
    "Enabled": true,
    "EnableOtlpExporter": true,
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc",
    "ServiceName": "YourServiceName",
    "ServiceVersion": "1.0.0",

    "Tracing": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableSqlClientInstrumentation": true
    },

    "Metrics": {
      "EnableAspNetCoreInstrumentation": true,
      "EnableHttpClientInstrumentation": true,
      "EnableRuntimeInstrumentation": true
    }
  }
}
```

**Now you have:**
- ✅ **Distributed Tracing** → Jaeger
- ✅ **Metrics Monitoring** → Prometheus + Grafana
- ✅ **Log Analytics** → Elasticsearch + Kibana
- ✅ **Unified Observability** → Complete picture of your system

---

## Configuration

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

---

## Best Practices

### Distributed Tracing

1. **Use Meaningful Span Names**: Name spans based on operations, not implementation details
   ```csharp
   // ✅ Good
   using var activity = _activitySource.StartActivity("ProcessPayment");

   // ❌ Bad
   using var activity = _activitySource.StartActivity("Method1");
   ```

2. **Add Contextual Tags**: Include relevant business and technical context
   ```csharp
   activity?.SetTag("user.id", userId);
   activity?.SetTag("order.value", orderTotal);
   activity?.SetTag("payment.method", "credit_card");
   ```

3. **Record Exceptions**: Always capture exceptions in traces
   ```csharp
   try
   {
       // operation
   }
   catch (Exception ex)
   {
       activity?.RecordException(ex);
       activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
       throw;
   }
   ```

4. **Use Activity Events**: Mark important milestones
   ```csharp
   activity?.AddEvent(new ActivityEvent("PaymentAuthorized"));
   activity?.AddEvent(new ActivityEvent("InventoryReserved"));
   ```

### Metrics

1. **Choose Appropriate Metric Types**:
   - **Counter**: Monotonically increasing values (requests, errors)
   - **Histogram**: Value distributions (latencies, sizes)
   - **Gauge**: Current values (queue size, connections)
   - **UpDownCounter**: Values that can increase/decrease (cache size)

2. **Use Consistent Naming**: Follow OpenTelemetry semantic conventions
   ```csharp
   // Good: descriptive, hierarchical
   "payment.processing.duration"
   "inventory.stock.level"
   "order.value.total"
   ```

3. **Add Dimensions**: Use tags for filtering and grouping
   ```csharp
   _counter.Add(1,
       new KeyValuePair<string, object?>("status", "success"),
       new KeyValuePair<string, object?>("region", "us-west"),
       new KeyValuePair<string, object?>("payment_type", "card"));
   ```

4. **Avoid High Cardinality**: Don't use unique IDs as tags
   ```csharp
   // ❌ Bad: creates too many time series
   counter.Add(1, new KeyValuePair<string, object?>("user_id", userId));

   // ✅ Good: use categories
   counter.Add(1, new KeyValuePair<string, object?>("user_type", "premium"));
   ```

### Logging

1. **Use Structured Logging**: Use message templates, not string interpolation
   ```csharp
   // ✅ Good
   _logger.LogInformation("Order {OrderId} processed for customer {CustomerId}", orderId, customerId);

   // ❌ Bad
   _logger.LogInformation($"Order {orderId} processed for customer {customerId}");
   ```

2. **Appropriate Log Levels**:
   - **Trace**: Very detailed diagnostic info (rarely used)
   - **Debug**: Debugging information (development)
   - **Information**: General informational messages
   - **Warning**: Unexpected but recoverable situations
   - **Error**: Errors and exceptions
   - **Critical**: Critical failures requiring immediate attention

3. **Correlation**: Logs automatically include trace context when using OpenTelemetry
   ```csharp
   // Logs will include TraceId and SpanId for correlation
   _logger.LogInformation("Processing started");
   ```

### Performance

1. **Use Async Sinks**: Always enable async logging for better performance
   ```json
   {
     "AdvancedLogging": {
       "Buffered": true
     }
   }
   ```

2. **Configure Sampling**: Use sampling for high-traffic services (production)
   ```csharp
   // In OpenTelemetryExtensions.cs, you can customize:
   builder.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling
   ```

3. **Batch Exports**: Exporters batch telemetry for efficiency (configured by default)

4. **Monitor Resource Usage**: Check exporter health and adjust batch sizes if needed

---

## Troubleshooting

### Traces Not Appearing in Jaeger

**Problem**: No traces visible in Jaeger UI

**Solutions**:
1. Verify OTLP endpoint is accessible:
   ```bash
   curl http://localhost:4317
   ```

2. Check application logs for OpenTelemetry errors:
   ```bash
   # Look for OpenTelemetry initialization messages
   dotnet run --configuration Development
   ```

3. Verify configuration:
   ```json
   {
     "OpenTelemetry": {
       "Enabled": true,
       "Tracing": {
         "Enabled": true,
         "EnableOtlpExporter": true,
         "OtlpEndpoint": "http://localhost:4317"
       }
     }
   }
   ```

4. Check if ActivitySource is registered:
   ```csharp
   builder.Services.AddActivitySource("YourServiceName");
   ```

5. Ensure service name matches in configuration and ActivitySource

---

### Metrics Not Scraped by Prometheus

**Problem**: Prometheus shows target as DOWN or no metrics available

**Solutions**:
1. Verify OpenTelemetry Collector is receiving metrics:
   ```bash
   curl http://localhost:8889/metrics
   ```

2. Check if OTLP exporter is configured:
   ```json
   {
     "OpenTelemetry": {
       "Metrics": {
         "EnableOtlpExporter": true,
         "OtlpEndpoint": "http://localhost:4317"
       }
     }
   }
   ```

3. Verify Prometheus is scraping from OpenTelemetry Collector:
   ```yaml
   scrape_configs:
     - job_name: 'otel-collector'
       static_configs:
         - targets: ['host.docker.internal:8889']  # OpenTelemetry Collector endpoint
   ```

4. Check Prometheus UI (http://localhost:9090) → Status → Targets

---

### High Memory Usage

**Problem**: Application consuming too much memory

**Solutions**:
1. Reduce log retention:
   ```json
   {
     "AdvancedLogging": {
       "RetainedFileCountLimit": 3
     }
   }
   ```

2. Enable sampling for traces:
   ```csharp
   builder.SetSampler(new TraceIdRatioBasedSampler(0.1));
   ```

3. Reduce metric cardinality (avoid unique values in tags)

4. Adjust batch sizes in exporters

---

### Logs Not Correlating with Traces

**Problem**: Cannot find related logs for a trace

**Solutions**:
1. Ensure both logging and tracing are enabled
2. Use structured logging (`ILogger`)
3. Check that logs include `TraceId` and `SpanId` fields
4. Verify log formatter supports JSON (for Elasticsearch/Kibana)

```json
{
  "AdvancedLogging": {
    "Formatter": "Json"
  }
}
```

---

## Support & Resources

- **GitHub Issues**: https://github.com/acontplus/acontplus-dotnet-libs/issues
- **OpenTelemetry Docs**: https://opentelemetry.io/docs/
- **Jaeger Docs**: https://www.jaegertracing.io/docs/
- **Prometheus Docs**: https://prometheus.io/docs/
- **Serilog Docs**: https://serilog.net/

---

## Contributing

Contributions are welcome! Please submit pull requests or open issues for bugs and feature requests.

---

## License

This library is licensed under the MIT License. See LICENSE file for details.

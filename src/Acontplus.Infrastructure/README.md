# Acontplus.Infrastructure

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Infrastructure.svg)](https://www.nuget.org/packages/Acontplus.Infrastructure)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Enterprise-grade infrastructure library providing caching, resilience patterns, HTTP client factory, rate limiting, health checks, response compression, and event bus for .NET applications. Built with modern .NET 10 features and industry best practices.

> **💡 Application Services**: For authentication, authorization policies, security headers, device detection, and request context, use **[Acontplus.Services](https://www.nuget.org/packages/Acontplus.Services)**

## 🚀 Features

### 🗄️ Caching

- **Unified Interface**: Single `ICacheService` for both in-memory and distributed caching
- **In-Memory Cache**: High-performance memory cache with statistics tracking
- **Distributed Cache**: Redis support for multi-instance deployments
- **Automatic Fallback**: Graceful degradation when cache operations fail
- **Thread-Safe**: Concurrent access patterns with proper locking
- **Statistics**: Cache hit/miss rates and performance metrics (in-memory only)

### 🛡️ Resilience Patterns

- **Circuit Breaker**: Automatic failure detection and recovery using Polly
- **Retry Policies**: Configurable retry with exponential backoff
- **Timeout Policies**: Operation-specific timeout configurations
- **Pre-configured Policies**: Default, API, Database, External, and Auth policies
- **Health Monitoring**: Circuit breaker state tracking and reporting

### 🌐 HTTP Client Resilience

- **Resilient HTTP Clients**: Built-in circuit breaker, retry, and timeout
- **Multiple Configurations**: Default, API, External, and Long-Running clients
- **Standard Resilience Handler**: Uses Microsoft.Extensions.Http.Resilience
- **Factory Pattern**: Easy client creation with appropriate resilience settings

### 🚦 Rate Limiting

- **Advanced Configuration**: Multi-key rate limiting (IP, Client ID, User ID)
- **Custom Policies**: Pre-configured "api" and "auth" policies
- **Built-in Middleware**: Uses .NET's built-in rate limiting infrastructure
- **Custom Responses**: JSON error responses with retry-after headers
- **Flexible Windows**: Configurable time windows and request limits

### 🏥 Health Checks

- **Cache Health Check**: Tests read/write/delete operations
- **Circuit Breaker Health Check**: Monitors circuit breaker states
- **Ready/Live Probes**: Kubernetes-compatible health endpoints
- **Detailed Metrics**: Rich health check data for monitoring

### 🗜️ Response Compression

- **Brotli & Gzip**: Modern compression algorithms with Brotli preferred
- **Configurable MIME Types**: Customize compressed content types
- **HTTPS Security**: Optional HTTPS-only compression
- **Performance Boost**: Reduce bandwidth and improve response times
- **Client-Aware**: Automatic compression based on client capabilities

### 📡 Application Event Bus (NEW in v1.2.1+)

- **Channel-Based Architecture**: High-performance using `System.Threading.Channels`
- **Async Background Processing**: Non-blocking event handling for cross-cutting concerns
- **Pub/Sub Pattern**: In-memory event publishing and subscribing
- **CQRS Ready**: Perfect for command/query separation with application events
- **Microservices Communication**: Scalable async event processing
- **Multiple Subscribers**: Many background handlers can listen to the same event
- **Thread-Safe**: Concurrent event publishing and consumption
- **Clean Architecture**: Abstractions in Core, implementation in Infrastructure
- **⚠️ Note**: For **transactional domain events** (same transaction, DB ID dependencies), use `IDomainEventDispatcher` from Core

## 📦 Installation

### NuGet Package Manager

```bash
Install-Package Acontplus.Infrastructure
```

### .NET CLI

```bash
dotnet add package Acontplus.Infrastructure
```

### PackageReference

```xml
<PackageReference Include="Acontplus.Infrastructure" Version="1.0.0" />
```

## 🎯 Quick Start

### 1. Basic Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add all infrastructure services with one line (includes Event Bus)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health");

app.Run();
```

### 2. With Event Bus

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add in-memory event bus for CQRS/Event-Driven architecture
builder.Services.AddInMemoryEventBus(options =>
{
    options.EnableDiagnosticLogging = true;
});

// Register event handlers as background services
builder.Services.AddHostedService<OrderCreatedHandler>();

var app = builder.Build();
app.Run();
```

### 3. With Rate Limiting

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add rate limiting (optional)
builder.Services.AddAdvancedRateLimiting(builder.Configuration);

var app = builder.Build();

// Use rate limiting middleware
app.UseRateLimiter();

// Map health and controllers
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
```

### 4. Basic Configuration

Add to your `appsettings.json`:

```json
{
  "Caching": {
    "UseDistributedCache": false,
    "MemoryCacheSizeLimit": 104857600,
    "ExpirationScanFrequencyMinutes": 5
  },
  "Resilience": {
    "CircuitBreaker": {
      "Enabled": true,
      "ExceptionsAllowedBeforeBreaking": 5,
      "DurationOfBreakSeconds": 30,
      "SamplingDurationSeconds": 60,
      "MinimumThroughput": 10
    },
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "BaseDelaySeconds": 1,
      "ExponentialBackoff": true,
      "MaxDelaySeconds": 30
    },
    "RateLimiting": {
      "Enabled": true,
      "WindowSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "ByIpAddress": true,
      "ByClientId": true,
      "ByUserId": false
    },
    "Timeout": {
      "Enabled": true,
      "DefaultTimeoutSeconds": 30,
      "DatabaseTimeoutSeconds": 60,
      "HttpClientTimeoutSeconds": 30,
      "LongRunningTimeoutSeconds": 300
    }
  }
}
```

## 📚 Usage Guide

### Caching Service

#### In-Memory Caching

```csharp
public class UserService
{
    private readonly ICacheService _cache;

    public UserService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var cacheKey = $"user:{userId}";

        // Simple get/set
        var cachedUser = await _cache.GetAsync<User>(cacheKey);
        if (cachedUser != null)
            return cachedUser;

        var user = await _repository.GetByIdAsync(userId);
        await _cache.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));

        return user;
    }

    public async Task<User> GetOrCreateUserAsync(int userId)
    {
        // Factory pattern - only calls factory if not in cache
        return await _cache.GetOrCreateAsync(
            $"user:{userId}",
            async () => await _repository.GetByIdAsync(userId),
            TimeSpan.FromMinutes(30)
        );
    }

    public async Task<CacheStatistics> GetCacheStatsAsync()
    {
        // Get cache statistics (in-memory only)
        return _cache.GetStatistics();
    }
}
```

#### Redis Distributed Caching

```json
{
  "Caching": {
    "UseDistributedCache": true,
    "RedisConnectionString": "localhost:6379,abortConnect=false",
    "RedisInstanceName": "myapp:"
  }
}
```

```csharp
// Same code works with Redis - just change configuration!
var user = await _cache.GetOrCreateAsync(
    $"user:{userId}",
    async () => await _repository.GetByIdAsync(userId),
    TimeSpan.FromMinutes(30)
);
```

### Circuit Breaker Service

#### Pre-configured Policies

```csharp
public class ExternalApiService
{
    private readonly ICircuitBreakerService _circuitBreaker;

    // Default policy: 5 failures, 30s break
    public async Task<Data> CallDefaultAsync()
    {
        return await _circuitBreaker.ExecuteAsync(
            async () => await MakeApiCall(),
            "default"
        );
    }

    // API policy: More lenient (7 failures, 60s break)
    public async Task<Data> CallApiAsync()
    {
        return await _circuitBreaker.ExecuteAsync(
            async () => await MakeApiCall(),
            "api"
        );
    }

    // Database policy: Strict (4 failures, 90s break)
    public async Task<Data> CallDatabaseAsync()
    {
        return await _circuitBreaker.ExecuteAsync(
            async () => await QueryDatabase(),
            "database"
        );
    }

    // External policy: Very strict (1 failure, 300s break)
    public async Task<Data> CallExternalAsync()
    {
        return await _circuitBreaker.ExecuteAsync(
            async () => await CallThirdPartyApi(),
            "external"
        );
    }

    // Auth policy: Strict (4 failures, 60s break)
    public async Task<AuthResult> AuthenticateAsync()
    {
        return await _circuitBreaker.ExecuteAsync(
            async () => await ValidateToken(),
            "auth"
        );
    }

    // Check circuit breaker state
    public CircuitBreakerState GetStatus(string policy = "default")
    {
        return _circuitBreaker.GetCircuitBreakerState(policy);
        // Returns: Closed, Open, or HalfOpen
    }
}
```

### Retry Policy Service

```csharp
public class OrderService
{
    private readonly RetryPolicyService _retryPolicy;

    // Async with default retry settings
    public async Task<Order> CreateOrderAsync(Order order)
    {
        return await _retryPolicy.ExecuteAsync(
            async () => await _repository.CreateAsync(order)
        );
    }

    // Async with custom retry settings
    public async Task<Order> CreateOrderWithCustomRetryAsync(Order order)
    {
        return await _retryPolicy.ExecuteAsync(
            async () => await _repository.CreateAsync(order),
            maxRetries: 5,
            baseDelay: TimeSpan.FromSeconds(2)
        );
    }

    // Synchronous retry
    public Order CreateOrderSync(Order order)
    {
        return _retryPolicy.Execute(
            () => _repository.Create(order),
            maxRetries: 3
        );
    }
}
```

### Resilient HTTP Client Factory

```csharp
public class ApiIntegrationService
{
    private readonly ResilientHttpClientFactory _httpFactory;

    public ApiIntegrationService(ResilientHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    // Use default client (30s timeout, standard resilience)
    public async Task<string> CallApiAsync()
    {
        var client = _httpFactory.CreateClient();
        var response = await client.GetAsync("https://api.example.com/data");
        return await response.Content.ReadAsStringAsync();
    }

    // Use API client (30s timeout, lenient resilience)
    public async Task<string> CallApiWithLenientPolicyAsync()
    {
        var client = _httpFactory.CreateApiClient();
        var response = await client.GetAsync("https://api.example.com/data");
        return await response.Content.ReadAsStringAsync();
    }

    // Use external client (30s timeout, strict resilience)
    public async Task<string> CallExternalApiAsync()
    {
        var client = _httpFactory.CreateExternalClient();
        var response = await client.GetAsync("https://external.api.com/data");
        return await response.Content.ReadAsStringAsync();
    }

    // Use long-running client (300s timeout)
    public async Task<string> ProcessLongRunningAsync()
    {
        var client = _httpFactory.CreateLongRunningClient();
        var response = await client.GetAsync("https://api.example.com/process");
        return await response.Content.ReadAsStringAsync();
    }

    // Use custom timeout
    public async Task<string> CallWithCustomTimeoutAsync()
    {
        var client = _httpFactory.CreateClientWithTimeout(
            "custom",
            TimeSpan.FromMinutes(5)
        );
        var response = await client.GetAsync("https://api.example.com/data");
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Rate Limiting

#### Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure rate limiting
builder.Services.AddAdvancedRateLimiting(builder.Configuration);

var app = builder.Build();

// Add rate limiting middleware (MUST be before MapControllers)
app.UseRateLimiter();

app.MapControllers();
app.Run();
```

#### Configuration

```json
{
  "Resilience": {
    "RateLimiting": {
      "Enabled": true,
      "WindowSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "ByIpAddress": true,
      "ByClientId": true,
      "ByUserId": false
    }
  }
}
```

#### Apply to Specific Endpoints

```csharp
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Uses global rate limiting (100 requests per 60 seconds)
    [HttpGet]
    public IActionResult GetAll() => Ok();

    // Uses "api" policy (50 requests per 60 seconds)
    [HttpGet("{id}")]
    [EnableRateLimiting("api")]
    public IActionResult Get(int id) => Ok();

    // Uses "auth" policy (5 requests per 5 minutes)
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public IActionResult Login() => Ok();

    // Disable rate limiting for specific endpoint
    [HttpGet("health")]
    [DisableRateLimiting]
    public IActionResult Health() => Ok();
}
```

#### Rate Limit Response

When rate limit is exceeded, clients receive:

```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": 60
}
```

HTTP Status: `429 Too Many Requests`

### Health Checks (2025+ Modern Approach)

#### Unified Health Endpoints with Tags

Acontplus.Infrastructure now provides a single extension to map all health check endpoints with consistent JSON formatting and tag-based filtering:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
var app = builder.Build();

// One line to map all health endpoints (with apiName, tags, and full details)
app.MapHealthCheckEndpoints();

app.Run();
```

This creates:

- `/health` (all checks)
- `/health/ready` (checks tagged `ready`)
- `/health/live` (checks tagged `live`)
- `/health/cache` (checks tagged `cache`)
- `/health/resilience` (checks tagged `resilience`)

**Behavior:**

- If no cache or circuit breaker is registered, endpoints still work and return a self-check with the app name.
- If a tag endpoint (like `/health/cache`) has no checks, it returns an empty array but a valid response.
- All responses include `apiName`, `status`, `checks`, and `totalDuration`.

#### Example Response

```json
{
  "apiName": "Demo.Api",
  "status": "Healthy",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Demo.Api is running",
      "data": {
        "application": "Demo.Api",
        "tags": "live, ready",
        "lastCheckTime": "2025-11-27T12:00:00Z"
      }
    }
  ],
  "totalDuration": "00:00:00.0054321"
}
```

#### Customization

- You can override the base path: `app.MapHealthCheckEndpoints("/myhealth")`
- You can still add custom health checks and tags as before.

#### Migration

- **Old:** Multiple `app.MapHealthChecks` with custom response writers
- **New:** Just call `app.MapHealthCheckEndpoints()` for all endpoints and formatting

See `Extensions/HealthCheckEndpointExtensions.cs` for details.

### Response Compression

Optimize API performance with automatic response compression using Brotli and Gzip algorithms.

#### Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add response compression
builder.Services.AddResponseCompression(builder.Configuration);

var app = builder.Build();

// Use response compression middleware (MUST be before MapControllers)
app.UseResponseCompression();

app.MapControllers();
app.Run();
```

#### Configuration

```json
{
  "ResponseCompression": {
    "EnableForHttps": true,
    "MimeTypes": [],
    "EnableBrotli": true,
    "BrotliLevel": "Optimal",
    "EnableGzip": true,
    "GzipLevel": "Optimal"
  }
}
```

#### Features

- **Dual Compression**: Brotli (preferred) and Gzip support
- **HTTPS Only**: Optional HTTPS-only compression for security
- **Configurable MIME Types**: Customize which content types to compress
- **Default Types**: Automatically includes JSON, XML, CSS, JS, and more
- **Performance Optimized**: Brotli provides better compression ratios

## ⚙️ Configuration Reference

### Complete Configuration Example

```json
{
  "Caching": {
    "UseDistributedCache": false,
    "RedisConnectionString": "localhost:6379,abortConnect=false",
    "RedisInstanceName": "myapp:",
    "MemoryCacheSizeLimit": 104857600,
    "ExpirationScanFrequencyMinutes": 5
  },
  "Resilience": {
    "RateLimiting": {
      "Enabled": true,
      "WindowSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "SlidingWindow": true,
      "ByIpAddress": true,
      "ByUserId": false,
      "ByClientId": true
    },
    "CircuitBreaker": {
      "Enabled": true,
      "ExceptionsAllowedBeforeBreaking": 5,
      "DurationOfBreakSeconds": 30,
      "SamplingDurationSeconds": 60,
      "MinimumThroughput": 10
    },
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3,
      "BaseDelaySeconds": 1,
      "ExponentialBackoff": true,
      "MaxDelaySeconds": 30
    },
    "Timeout": {
      "Enabled": true,
      "DefaultTimeoutSeconds": 30,
      "DatabaseTimeoutSeconds": 60,
      "HttpClientTimeoutSeconds": 30,
      "LongRunningTimeoutSeconds": 300
    }
  }
}
```

## 📚 Event Bus - Complete Guide

### Overview

The **Acontplus Event Bus** provides a high-performance, scalable in-memory event-driven architecture using `System.Threading.Channels`. It's designed for **Clean Architecture + DDD + CQRS** patterns with support for horizontal and vertical scaling under high workloads.

### Package Structure

```
Acontplus.Core (Abstractions)
├── IEventPublisher          - Publish events
├── IEventSubscriber         - Subscribe to events
└── IEventBus                - Combined interface

Acontplus.Infrastructure (Implementation)
├── InMemoryEventBus         - Channel-based implementation
├── EventBusOptions          - Configuration options
└── EventBusExtensions       - DI registration
```

### Quick Start

#### 1. Register Event Bus

```csharp
// Program.cs
services.AddInMemoryEventBus(options =>
{
    options.EnableDiagnosticLogging = true;
});
```

#### 2. Define Events

```csharp
// Events are simple POCOs (record types recommended)
public record OrderCreatedEvent(
    Guid OrderId,
    string CustomerName,
    decimal TotalAmount,
    DateTime CreatedAt);
```

#### 3. Publish Events

```csharp
public class OrderService
{
    private readonly IEventPublisher _eventPublisher;

    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        // ... create order logic ...

        // Publish event
        await _eventPublisher.PublishAsync(new OrderCreatedEvent(
            orderId,
            command.CustomerName,
            totalAmount,
            DateTime.UtcNow));
    }
}
```

#### 4. Subscribe to Events

```csharp
public class OrderNotificationHandler : BackgroundService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly ILogger<OrderNotificationHandler> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var orderEvent in _eventSubscriber
            .SubscribeAsync<OrderCreatedEvent>(stoppingToken))
        {
            _logger.LogInformation("Sending email for Order {OrderId}", orderEvent.OrderId);
            await SendEmailAsync(orderEvent);
        }
    }
}
```

#### 5. Register Event Handlers

```csharp
// Register as hosted services
services.AddHostedService<OrderNotificationHandler>();
services.AddHostedService<OrderAnalyticsHandler>();
```

### Clean Architecture Implementation

#### Layer Organization

```
📁 Clean Architecture Layers
├── 🎯 Domain Layer (Acontplus.TestDomain)
│   ├── Entities/
│   │   └── Order.cs                 - Aggregate root
│   └── Events/
│       └── OrderEvents.cs           - Application events
│
├── 📋 Application Layer (Acontplus.TestApplication)
│   ├── Dtos/
│   │   └── OrderDtos.cs             - Commands, Queries, Results
│   ├── Interfaces/
│   │   └── IOrderService.cs         - Application service contract
│   └── Services/
│       └── OrderService.cs          - CQRS Command/Query handlers
│
├── 🏗️ Infrastructure Layer (Demo.Infrastructure)
│   └── EventHandlers/
│       ├── OrderNotificationHandler.cs   - Email notifications
│       ├── OrderAnalyticsHandler.cs      - Analytics tracking
│       └── OrderWorkflowHandler.cs       - Workflow automation
│
└── 🌐 Presentation Layer (Demo.Api)
    └── Endpoints/Business/
        └── OrderEndpoints.cs        - Minimal API endpoints
```

#### Request Flow

```
1. HTTP POST /api/orders
   ↓
2. OrderEndpoints.MapPost (Presentation)
   ↓
3. IOrderService.CreateOrderAsync (Application)
   ├── Create Order entity (Domain)
   ├── Save to repository (Infrastructure)
   └── Publish OrderCreatedEvent (via IEventPublisher)
   ↓
4. Event Bus distributes to subscribers:
   ├── OrderNotificationHandler → Send email
   ├── OrderAnalyticsHandler → Record analytics
   └── OrderWorkflowHandler → Auto-process order
   ↓
5. Return OrderCreatedResult
```

### Configuration Options

```csharp
services.AddInMemoryEventBus(options =>
{
    // Enable detailed logging for diagnostics
    options.EnableDiagnosticLogging = true;

    // Limit concurrent handlers (0 = unlimited)
    options.MaxConcurrentHandlers = 10;

    // Dispose on application shutdown
    options.DisposeOnShutdown = true;
});
```

### Performance Characteristics

#### Channel Configuration

```csharp
// Unbounded channels optimized for high throughput
Channel.CreateUnbounded<object>(new UnboundedChannelOptions
{
    SingleWriter = false,                     // Multiple publishers
    SingleReader = false,                     // Multiple subscribers
    AllowSynchronousContinuations = false     // Prevent deadlocks
});
```

#### Benchmarks (Estimated)

| Operation | Throughput | Latency |
|-----------|-----------|---------|
| Publish Event | ~1M ops/sec | <1μs |
| Subscribe & Process | ~500K ops/sec | <10μs |
| Concurrent Publishers (8 threads) | ~5M ops/sec | <5μs |

### Scaling Strategies

#### Horizontal Scaling

For distributed systems, replace `InMemoryEventBus` with:
- **Azure Service Bus**: `services.AddAzureServiceBusEventBus()`
- **RabbitMQ**: `services.AddRabbitMqEventBus()`
- **Kafka**: `services.AddKafkaEventBus()`

Interface (`IEventPublisher`, `IEventSubscriber`) remains the same!

#### Vertical Scaling

- Event handlers run as `BackgroundService` instances
- Increase parallelism with multiple handler instances
- Use `MaxConcurrentHandlers` to throttle processing

### Best Practices

#### ✅ Do

- Use **record types** for events (immutable, value equality)
- Keep events **small and focused** (single responsibility)
- Make events **JSON-serializable** (for future distributed support)
- Use **cancellation tokens** for graceful shutdown
- Register handlers as **scoped or transient** for DI injection
- Log important events for **observability**

#### ❌ Don't

- Throw exceptions in event handlers (use try-catch)
- Perform long-running blocking operations (use async)
- Share mutable state between handlers
- Publish events from constructors or finalizers
- Use events for request-response patterns (use MediatR instead)

### Event Systems Comparison

Acontplus provides **TWO distinct event systems** for different purposes:

#### 1. Domain Event Dispatcher (DDD Pattern)
- **Interface**: `IDomainEventDispatcher` + `IDomainEventHandler<T>`
- **Events**: Generic entity events (`EntityCreatedEvent`, `EntityModifiedEvent`, etc.)
- **Purpose**: Domain-Driven Design events **within bounded context**
- **Execution**: **Synchronous** within same transaction/unit of work
- **Use For**:
  - Domain invariant enforcement
  - Updating related aggregates
  - Audit logging (transactional)
  - Domain business rules

#### 2. Application Event Bus (Microservices Pattern)
- **Interface**: `IEventPublisher` + `IEventSubscriber`
- **Events**: Custom application events (`OrderCreatedEvent`, `PaymentProcessedEvent`, etc.)
- **Purpose**: **Cross-service** communication and async workflows
- **Execution**: **Asynchronous** via background handlers (System.Threading.Channels)
- **Use For**:
  - Microservices communication
  - Notifications (email, SMS, push)
  - Analytics and reporting
  - Integration with external systems
  - Background processing

#### When to Use Which?

| Scenario | Use Domain Event Dispatcher | Use Application Event Bus |
|----------|----------------------------|---------------------------|
| **Update related aggregate in same transaction** | ✅ Yes | ❌ No |
| **Send email notification** | ❌ No | ✅ Yes |
| **Enforce domain invariant** | ✅ Yes | ❌ No |
| **Publish to external system** | ❌ No | ✅ Yes |
| **Audit trail (transactional)** | ✅ Yes | ❌ No |
| **Analytics/metrics** | ❌ No | ✅ Yes |
| **Workflow automation** | ❌ No | ✅ Yes |
| **Cross-bounded-context communication** | ❌ No | ✅ Yes |

### Testing

#### Unit Testing

```csharp
[Fact]
public async Task CreateOrder_PublishesOrderCreatedEvent()
{
    // Arrange
    var eventBus = new InMemoryEventBus(logger);
    var service = new OrderService(eventBus, logger);
    var events = new List<OrderCreatedEvent>();

    // Start subscriber
    var cts = new CancellationTokenSource();
    _ = Task.Run(async () =>
    {
        await foreach (var evt in eventBus.SubscribeAsync<OrderCreatedEvent>(cts.Token))
        {
            events.Add(evt);
            cts.Cancel(); // Stop after first event
        }
    });

    // Act
    await service.CreateOrderAsync(new CreateOrderCommand(...));
    await Task.Delay(100); // Allow event processing

    // Assert
    Assert.Single(events);
    Assert.Equal("John Doe", events[0].CustomerName);
}
```

### Live Demo

Run the Demo.Api and use HTTP requests to test:

```bash
cd apps/src/Demo.Api
dotnet run
```

#### Example HTTP Request

```http
POST https://localhost:7001/api/orders
Content-Type: application/json

{
  "customerName": "John Doe",
  "productName": "Premium Widget",
  "quantity": 5,
  "price": 99.99
}
```

#### Expected Console Output

```
[OrderService] Order created: {OrderId} for customer John Doe
[InMemoryEventBus] Event published: OrderCreatedEvent at 2025-12-05T10:30:00Z

[OrderNotificationHandler] 📧 Sending email for Order {OrderId} - Customer: John Doe, Total: $499.95
[OrderNotificationHandler] ✅ Email sent successfully

[OrderAnalyticsHandler] 📊 Recording analytics for Order {OrderId} - Product: Premium Widget
[OrderAnalyticsHandler] ✅ Analytics recorded

[OrderWorkflowHandler] 🔄 Auto-processing Order {OrderId}
[OrderWorkflowHandler] ✅ Order processed and event published
[OrderWorkflowHandler] 📦 Preparing shipment for Order {OrderId}
[OrderWorkflowHandler] 🚚 Order shipped - Tracking: TRACK-{OrderId}
```

### Troubleshooting

#### Events not received

- Ensure handlers are registered as `HostedService`
- Check cancellation token is not cancelled
- Enable diagnostic logging

#### Memory leaks

- Ensure handlers honor `CancellationToken`
- Check for unhandled exceptions in handlers
- Monitor channel subscriptions

#### Slow processing

- Check handler logic for blocking operations
- Review database query performance
- Consider parallel handler instances

## 📚 API Reference

### Extension Methods

#### Service Registration

```csharp
// Register all infrastructure services
services.AddInfrastructureServices(configuration);

// Or register individually
services.AddCachingServices(configuration);
services.AddResilienceServices(configuration);
services.AddResilientHttpClients(configuration);
services.AddInfrastructureHealthChecks();
services.AddAdvancedRateLimiting(configuration);
```

#### Middleware

```csharp
// Rate limiting middleware (uses .NET's built-in rate limiter)
app.UseRateLimiter();
```

### Core Interfaces

```csharp
// Caching
ICacheService
  - GetAsync<T>(string key, CancellationToken cancellationToken = default)
  - SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
  - GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
  - RemoveAsync(string key, CancellationToken cancellationToken = default)
  - GetStatistics() // In-memory only

// Circuit Breaker
ICircuitBreakerService
  - ExecuteAsync<T>(Func<Task<T>> action, string policy = "default")
  - GetCircuitBreakerState(string policy = "default")

// Retry Policy
RetryPolicyService
  - ExecuteAsync<T>(Func<Task<T>> action, int? maxRetries = null, TimeSpan? baseDelay = null)
  - Execute<T>(Func<T> action, int? maxRetries = null, TimeSpan? baseDelay = null)

// HTTP Client Factory
ResilientHttpClientFactory
  - CreateClient()
  - CreateApiClient()
  - CreateExternalClient()
  - CreateLongRunningClient()
  - CreateClientWithTimeout(string name, TimeSpan timeout)

// Event Bus (Acontplus.Core.Abstractions.Messaging)
IEventPublisher
  - PublishAsync<T>(T eventData, CancellationToken cancellationToken = default)

IEventSubscriber
  - SubscribeAsync<T>(CancellationToken cancellationToken = default)

IEventBus : IEventPublisher, IEventSubscriber
```

## 🏗️ Architecture

### Folder Structure

```
Acontplus.Infrastructure/
├── Caching/
│   ├── MemoryCacheService.cs          # In-memory cache implementation
│   └── DistributedCacheService.cs     # Redis distributed cache
├── Resilience/
│   ├── CircuitBreakerService.cs       # Circuit breaker service
│   └── RetryPolicyService.cs          # Retry policy service
├── Http/
│   └── ResilientHttpClientFactory.cs  # HTTP client factory
├── Messaging/
│   ├── InMemoryEventBus.cs            # Channel-based event bus
│   ├── ChannelExtensions.cs           # Type-safe channel transformations
│   └── EventBusOptions.cs             # Event bus configuration
├── HealthChecks/
│   ├── CacheHealthCheck.cs            # Cache health check
│   └── CircuitBreakerHealthCheck.cs   # Circuit breaker health check
├── Configuration/
│   ├── CacheConfiguration.cs          # Cache config
│   └── ResilienceConfiguration.cs     # Resilience config
└── Extensions/
    ├── InfrastructureServiceExtensions.cs  # DI registration
    ├── RateLimitingExtensions.cs           # Rate limiting configuration
    └── EventBusExtensions.cs               # Event bus registration
```

### Dependencies

- **Polly**: Resilience and transient-fault-handling
- **Microsoft.Extensions.Caching.StackExchangeRedis**: Redis provider
- **Microsoft.Extensions.Http.Resilience**: HTTP resilience
- **.NET Rate Limiting**: Built-in ASP.NET Core rate limiting middleware

## 🤝 Contributing

We welcome contributions! Please see [Contributing Guidelines](../../CONTRIBUTING.md).

## 📧 Support

- **Email**: proyectos@acontplus.com
- **Issues**: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- **Documentation**: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

## 👨‍💻 Author

**Ivan Paz** – [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** – Software Solutions, Ecuador

---

**Built with ❤️ for enterprise-grade .NET applications**

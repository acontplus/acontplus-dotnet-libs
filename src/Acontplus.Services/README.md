# Acontplus.Services

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Services.svg)](https://www.nuget.org/packages/Acontplus.Services)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A comprehensive .NET service library providing business-grade patterns, security, device detection, request management, and **intelligent exception handling** for ASP.NET Core applications. Built with modern .NET 10 features and best practices.

> **💡 Infrastructure Services**: For caching, circuit breakers, resilience patterns, and HTTP client factory, use **[Acontplus.Infrastructure](https://www.nuget.org/packages/Acontplus.Infrastructure)**

## 🚀 Features

### 🏗️ Service Architecture Patterns

- **Service Layer**: Clean separation of concerns with dependency injection
- **Lookup Service**: Cached lookup/reference data management with flexible SQL mapping
- **Action Filters**: Reusable cross-cutting concerns (validation, logging, security)
- **Authorization Policies**: Fine-grained access control for multi-tenant scenarios
- **Middleware Pipeline**: Properly ordered middleware for security and context management

### 🛡️ Advanced Exception Handling **NEW!**

- **Flexible Design**: Works with or without catch blocks - your choice!
- **Smart Exception Translation**: Preserves custom error codes from business logic
- **DomainException Support**: Automatic handling of domain exceptions with proper HTTP status codes
- **Consistent API Responses**: Standardized error format with categories and severity
- **Intelligent Logging**: Context-aware logging with appropriate severity levels
- **Distributed Tracing**: Correlation IDs and trace IDs for request tracking
- **Multi-tenancy Support**: Tenant ID tracking across requests

📖 **[Complete Exception Handling Guide](Middleware/ApiExceptionMiddleware.README.md)**

### 🔒 Security & Compliance

- **Security Headers**: Comprehensive HTTP security header management
- **Content Security Policy**: CSP nonce generation and management
- **Client Validation**: Client-ID based access control
- **Tenant Isolation**: Multi-tenant security policies
- **JWT Authentication**: Enterprise-grade JWT token validation

### 📱 Device & Context Awareness

- **Device Detection**: Smart device type detection from headers and user agents
- **Request Context**: Correlation IDs, tenant isolation, and request tracking
- **Device-Aware Policies**: Mobile and tablet-aware authorization policies

### 📊 Observability

- **Request Logging**: Structured logging with performance metrics
- **Health Checks**: Comprehensive health monitoring for application services
- **Application Insights**: Optional integration for telemetry and monitoring

## 📦 Installation

### Required Packages

```bash
# Application services (this package)
dotnet add package Acontplus.Services

# Infrastructure services (caching, resilience, etc.)
dotnet add package Acontplus.Infrastructure
```

### NuGet Package Manager

```bash
Install-Package Acontplus.Services
Install-Package Acontplus.Infrastructure
```

### PackageReference

```xml
<PackageReference Include="Acontplus.Services" Version="1.5.0" />
<PackageReference Include="Acontplus.Infrastructure" Version="1.0.0" />
```

## 🎯 Quick Start

### 1. Add to Your Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add application services (authentication, security, device detection, exception handling)
builder.Services.AddApplicationServices(builder.Configuration);

// Add infrastructure services (caching, resilience, HTTP clients)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Use application middleware pipeline (includes exception handling)
app.UseApplicationMiddleware(builder.Environment);

app.MapControllers();
app.Run();
```

### 2. Exception Handling - No Catch Needed! **NEW!**

```csharp
// Business Layer - Just throw, middleware handles everything
public async Task<Customer> GetCustomerAsync(int id)
{
    var customer = await _repository.GetByIdAsync(id);

    if (customer is null)
    {
        throw new GenericDomainException(
            ErrorType.NotFound,
            "CUSTOMER_NOT_FOUND",
            "Customer not found");
    }

    return customer;
}
```

**Automatic Response:**
```json
{
  "success": false,
  "code": "404",
  "message": "Customer not found",
  "errors": [{
    "code": "CUSTOMER_NOT_FOUND",
    "message": "Customer not found",
    "category": "business",
    "severity": "warning"
  }],
  "correlationId": "abc-123"
}
```

**Or Use Result Pattern:**
```csharp
public async Task<Result<Customer, DomainError>> GetCustomerAsync(int id)
{
    try
    {
        var customer = await _repository.GetByIdAsync(id);
        return customer ?? DomainError.NotFound("CUSTOMER_NOT_FOUND", "Not found");
    }
    catch (SqlDomainException ex)
    {
        return ex.ToDomainError();
    }
}

// Controller
[HttpGet("{id}")]
public Task<IActionResult> GetCustomer(int id)
{
    return _service.GetCustomerAsync(id).ToActionResultAsync();
}
```

### 3. Basic Configuration

Add to your `appsettings.json`:

```json
{
  "RequestContext": {
    "EnableSecurityHeaders": true,
    "RequireClientId": false,
    "Csp": {
      "AllowedFrameSources": ["https://www.youtube-nocookie.com"],
      "AllowedScriptSources": ["https://cdn.jsdelivr.net"],
      "AllowedConnectSources": ["https://api.yourdomain.com"]
    }
  },
  "ExceptionHandling": {
    "IncludeDebugDetailsInResponse": false,
    "IncludeRequestDetails": true,
    "LogRequestBody": false
  },
  "Caching": {
    "UseDistributedCache": false
  }
}
```

### 4. Use in Your Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class HelloController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IRequestContextService _context;

    public HelloController(ICacheService cache, IRequestContextService context)
    {
        _cache = cache;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var message = await _cache.GetOrCreateAsync("hello",
            () => Task.FromResult("Hello from Acontplus.Services!"),
            TimeSpan.FromMinutes(5));

        return Ok(new {
            Message = message,
            CorrelationId = _context.GetCorrelationId()
        });
    }
}
```

## 🎯 Usage Examples

### 🟢 Basic Usage - Simple Setup

Perfect for small applications or getting started quickly.

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add application and infrastructure services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Complete middleware pipeline in one call
app.UseApplicationMiddleware(builder.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### Basic Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class BasicController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IRequestContextService _context;

    public BasicController(ICacheService cache, IRequestContextService context)
    {
        _cache = cache;
        _context = context;
    }

    [HttpGet("hello")]
    public async Task<IActionResult> Hello()
    {
        var message = await _cache.GetOrCreateAsync(
            "hello-message",
            () => Task.FromResult("Hello from Acontplus.Services!"),
            TimeSpan.FromMinutes(5)
        );

        return Ok(new {
            Message = message,
            CorrelationId = _context.GetCorrelationId()
        });
    }
}
```

### 🟡 Intermediate Usage - Granular Control

For applications that need fine-grained control over services and middleware.

```csharp
// Program.cs with granular control
var builder = WebApplication.CreateBuilder(args);

// Add services individually for more control
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddResilienceServices(builder.Configuration);
builder.Services.AddAuthorizationPolicies(new List<string> { "web-app", "mobile-app" });

// Add health checks
builder.Services.AddApplicationHealthChecks(builder.Configuration);
builder.Services.AddInfrastructureHealthChecks();

// Add controllers with custom filters
builder.Services.AddControllers(options =>
{
    options.Filters.Add<SecurityHeaderActionFilter>();
    options.Filters.Add<RequestLoggingActionFilter>();
    options.Filters.Add<ValidationActionFilter>();
});

var app = builder.Build();

// Configure middleware pipeline manually
app.UseSecurityHeaders(builder.Environment);
app.UseMiddleware<CspNonceMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<RequestContextMiddleware>();
app.UseAcontplusExceptionHandling();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

#### Intermediate Controller with Device Detection

```csharp
[ApiController]
[Route("api/[controller]")]
public class IntermediateController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IDeviceDetectionService _deviceDetection;
    private readonly ICircuitBreakerService _circuitBreaker;

    public IntermediateController(
        ICacheService cache,
        IDeviceDetectionService deviceDetection,
        ICircuitBreakerService circuitBreaker)
    {
        _cache = cache;
        _deviceDetection = deviceDetection;
        _circuitBreaker = circuitBreaker;
    }

    [HttpGet("content")]
    public async Task<IActionResult> GetContent()
    {
        var deviceType = _deviceDetection.DetectDeviceType(HttpContext);
        var cacheKey = $"content:{deviceType}";

        var content = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            // Simulate external API call with circuit breaker
            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                await Task.Delay(100); // Simulate API call
                return deviceType switch
                {
                    DeviceType.Mobile => "Mobile-optimized content",
                    DeviceType.Tablet => "Tablet-optimized content",
                    _ => "Desktop content"
                };
            }, "content-api");
        }, TimeSpan.FromMinutes(10));

        return Ok(new { Content = content, DeviceType = deviceType.ToString() });
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        var circuitBreakerStatus = _circuitBreaker.GetCircuitBreakerState("content-api");
        var cacheStats = _cache.GetStatistics();

        return Ok(new
        {
            CircuitBreaker = circuitBreakerStatus,
            Cache = new
            {
                TotalEntries = cacheStats.TotalEntries,
                HitRate = $"{cacheStats.HitRatePercentage:F1}%"
            }
        });
    }
}
```

### 🔴 Enterprise Usage - Full Configuration

Complete setup for enterprise applications with all features enabled.

```csharp
// Program.cs for enterprise applications
var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddApplicationInsights();

// Add all Acontplus services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add authentication and authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Add authorization policies
builder.Services.AddAuthorizationPolicies(new List<string>
{
    "web-app", "mobile-app", "admin-portal", "api-client"
});

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseApplicationMiddleware(app.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.Run();
```

## ⚙️ Configuration Examples

### Complete Configuration

```json
{
  "RequestContext": {
    "EnableSecurityHeaders": true,
    "FrameOptionsDeny": true,
    "ReferrerPolicy": "strict-origin-when-cross-origin",
    "RequireClientId": true,
    "AllowedClientIds": ["web-app", "mobile-app", "admin-portal"],
    "Csp": {
      "AllowedImageSources": ["https://cdn.example.com"],
      "AllowedStyleSources": ["https://fonts.googleapis.com"],
      "AllowedScriptSources": ["https://cdn.example.com"],
      "AllowedConnectSources": ["https://api.example.com"]
    }
  },
  "Caching": {
    "UseDistributedCache": false,
    "MemoryCacheSizeLimit": 104857600
  },
  "Resilience": {
    "CircuitBreaker": {
      "Enabled": true,
      "ExceptionsAllowedBeforeBreaking": 5
    },
    "RetryPolicy": {
      "Enabled": true,
      "MaxRetries": 3
    }
  },
  "JwtSettings": {
    "Issuer": "https://auth.acontplus.com",
    "Audience": "api.acontplus.com",
    "SecurityKey": "your-super-secret-key-at-least-32-characters-long",
    "ClockSkew": "5",
    "RequireHttps": "true"
  }
}
```

## 📚 Core Services Reference

### What's in Acontplus.Services

✅ **Application Services**
- `IRequestContextService` - Request context management and correlation
- `ISecurityHeaderService` - HTTP security headers and CSP management
- `IDeviceDetectionService` - Device type detection and capabilities
- `ILookupService` - Cached lookup/reference data management (NEW!)

✅ **Action Filters**
- `ValidationActionFilter` - Model validation
- `RequestLoggingActionFilter` - Request/response logging
- `SecurityHeaderActionFilter` - Security header injection

✅ **Authorization Policies**
- `RequireClientIdPolicy` - Client ID validation
- `TenantIsolationPolicy` - Multi-tenant isolation
- `DeviceTypePolicy` - Device-aware authorization

✅ **Middleware**
- `RequestContextMiddleware` - Request context extraction
- `CspNonceMiddleware` - CSP nonce generation
- `ApiExceptionMiddleware` - Global exception handling

### What's in Acontplus.Infrastructure

> **Note**: These services require `Acontplus.Infrastructure` package

✅ **Infrastructure Services** (from Acontplus.Infrastructure)
- `ICacheService` - Caching (in-memory and Redis)
- `ICircuitBreakerService` - Circuit breaker patterns
- `RetryPolicyService` - Retry policies
- `ResilientHttpClientFactory` - Resilient HTTP clients

✅ **Middleware** (from Acontplus.Infrastructure)
- `RateLimitingMiddleware` - Rate limiting

✅ **Health Checks** (from Acontplus.Infrastructure)
- `CacheHealthCheck` - Cache service health
- `CircuitBreakerHealthCheck` - Circuit breaker health

## 🚀 Features Examples

### Lookup Service (NEW!)

Manage cached lookup/reference data from database queries with automatic caching.

```csharp
// 1. Register in Program.cs
builder.Services.AddLookupService();

// 2. Use in controller
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLookups(
        [FromQuery] string? module = null,
        [FromQuery] string? context = null)
    {
        var filterRequest = new FilterRequest
        {
            Filters = new Dictionary<string, object>
            {
                ["module"] = module ?? "default",
                ["context"] = context ?? "general"
            }
        };

        var result = await _lookupService.GetLookupsAsync(
            "YourSchema.GetLookups", // Stored procedure name
            filterRequest);

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshLookups()
    {
        var result = await _lookupService.RefreshLookupsAsync(
            "YourSchema.GetLookups",
            new FilterRequest());

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }
}
```

**Features:**
- ✅ Automatic caching (30-minute TTL)
- ✅ Works with SQL Server and PostgreSQL
- ✅ Flexible SQL query mapping (all nullable properties)
- ✅ Supports hierarchical data (ParentId)
- ✅ Grouped results by table name
- ✅ Cache refresh on demand

**SQL Stored Procedure Example:**
```sql
CREATE PROCEDURE [YourSchema].[GetLookups]
    @Module NVARCHAR(100) = NULL,
    @Context NVARCHAR(100) = NULL
AS
BEGIN
    SELECT
        'Countries' AS TableName,
        Id, Code, [Name] AS [Value], DisplayOrder,
        NULL AS ParentId, IsDefault, IsActive,
        Description, NULL AS Metadata
    FROM Countries
    WHERE IsActive = 1
    ORDER BY DisplayOrder;
END
```

**Response Format:**
```json
{
  "status": "Success",
  "data": {
    "countries": [
      {
        "id": 1,
        "code": "US",
        "value": "United States",
        "displayOrder": 1,
        "isDefault": true,
        "isActive": true,
        "description": "United States of America",
        "metadata": null
      }
    ]
  }
}
```

## 📚 Lookup Service - Complete Guide

### Overview

The `LookupService` is a reusable, cached service for managing lookup/reference data across all Acontplus APIs. It's located in the `Acontplus.Services` NuGet package and works seamlessly with both PostgreSQL and SQL Server.

### Architecture

#### Package Structure

```
Acontplus.Services/
├── Services/
│   ├── Abstractions/
│   │   └── ILookupService.cs          # Interface
│   ├── Implementations/
│   │   └── LookupService.cs           # Implementation
│   └── README.md
├── Extensions/
│   └── ServiceExtensions.cs           # DI registration
└── GlobalUsings.cs

Acontplus.Core/
└── Dtos/
    └── Responses/
        └── LookupItem.cs               # Shared DTO

Acontplus.Infrastructure/
└── Caching/
    ├── ICacheService.cs                # Cache abstraction
    ├── MemoryCacheService.cs           # In-memory implementation
    └── DistributedCacheService.cs      # Redis implementation
```

#### Design Decisions

**Location**: `Acontplus.Services` Package

**Rationale**:
- ✅ **Database Agnostic**: Works with both PostgreSQL and SQL Server through `IUnitOfWork` abstraction
- ✅ **Reusable**: Available to all APIs via NuGet package
- ✅ **Proper Layer**: Application-level service, not infrastructure or persistence specific
- ✅ **Dependencies**: Already has access to Core and Infrastructure packages

#### Data Flow

```
Controller
    ↓
ILookupService.GetLookupsAsync()
    ↓
Check Cache (ICacheService)
    ↓ (cache miss)
IUnitOfWork.AdoRepository.GetFilteredDataSetAsync()
    ↓
Stored Procedure Execution
    ↓
Map DataSet → Dictionary<string, IEnumerable<LookupItem>>
    ↓
Store in Cache
    ↓
Return Result<T, DomainError>
```

### Quick Start

#### 1. Register Services (Program.cs)

```csharp
// For development/single server
builder.Services.AddMemoryCache();
builder.Services.AddMemoryCacheService();

// OR for production/multi-server
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddDistributedCacheService();

// Register persistence (choose your database)
builder.Services.AddSqlServerPersistence<YourDbContext>(connectionString);
// OR
builder.Services.AddPostgresPersistence<YourDbContext>(connectionString);

// Register lookup service
builder.Services.AddLookupService();
```

#### 2. Create Stored Procedure

**SQL Server Example:**

```sql
CREATE PROCEDURE [YourSchema].[GetLookups]
    @Module NVARCHAR(100) = NULL,
    @Context NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Return multiple lookup tables
    SELECT
        'OrderStatuses' AS TableName,
        Id,
        Code,
        [Name] AS [Value],
        DisplayOrder,
        NULL AS ParentId,
        CAST(0 AS BIT) AS IsDefault,
        CAST(1 AS BIT) AS IsActive,
        Description,
        NULL AS Metadata
    FROM YourSchema.OrderStatuses
    WHERE IsActive = 1

    UNION ALL

    SELECT
        'PaymentMethods' AS TableName,
        Id,
        Code,
        [Name] AS [Value],
        SortOrder AS DisplayOrder,
        NULL AS ParentId,
        IsDefault,
        IsActive,
        NULL AS Description,
        JSON_QUERY((SELECT Icon, Color FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)) AS Metadata
    FROM YourSchema.PaymentMethods
    WHERE IsActive = 1

    ORDER BY TableName, DisplayOrder;
END
```

**PostgreSQL Example:**

```sql
CREATE OR REPLACE FUNCTION your_schema.get_lookups(
    p_module VARCHAR DEFAULT NULL,
    p_context VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    table_name VARCHAR,
    id INTEGER,
    code VARCHAR,
    value VARCHAR,
    display_order INTEGER,
    parent_id INTEGER,
    is_default BOOLEAN,
    is_active BOOLEAN,
    description TEXT,
    metadata JSONB
) AS $$
BEGIN
    RETURN QUERY

    SELECT
        'orderStatuses'::VARCHAR AS table_name,
        os.id,
        os.code,
        os.value,
        os.display_order,
        NULL::INTEGER AS parent_id,
        FALSE AS is_default,
        TRUE AS is_active,
        os.description,
        NULL::JSONB AS metadata
    FROM your_schema.order_statuses os
    WHERE os.is_active = TRUE

    UNION ALL

    SELECT
        'paymentMethods'::VARCHAR,
        pm.id,
        pm.code,
        pm.name AS value,
        pm.sort_order AS display_order,
        NULL::INTEGER,
        pm.is_default,
        pm.is_active,
        NULL::TEXT,
        jsonb_build_object('icon', pm.icon, 'color', pm.color) AS metadata
    FROM your_schema.payment_methods pm
    WHERE pm.is_active = TRUE

    ORDER BY table_name, display_order;
END;
$$ LANGUAGE plpgsql;
```

#### 3. Use in Controller

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    /// <summary>
    /// Get all lookups with caching
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IDictionary<string, IEnumerable<LookupItem>>>), 200)]
    public async Task<IActionResult> GetLookups(
        [FromQuery] string? module = null,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        var filterRequest = new FilterRequest
        {
            Filters = new Dictionary<string, object>
            {
                ["module"] = module ?? "default",
                ["context"] = context ?? "general"
            }
        };

        var result = await _lookupService.GetLookupsAsync(
            "YourSchema.GetLookups", // SQL Server
            // OR "your_schema.get_lookups" for PostgreSQL
            filterRequest,
            cancellationToken);

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }

    /// <summary>
    /// Refresh lookups cache
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<IDictionary<string, IEnumerable<LookupItem>>>), 200)]
    public async Task<IActionResult> RefreshLookups(
        [FromQuery] string? module = null,
        [FromQuery] string? context = null,
        CancellationToken cancellationToken = default)
    {
        var filterRequest = new FilterRequest
        {
            Filters = new Dictionary<string, object>
            {
                ["module"] = module ?? "default",
                ["context"] = context ?? "general"
            }
        };

        var result = await _lookupService.RefreshLookupsAsync(
            "YourSchema.GetLookups",
            filterRequest,
            cancellationToken);

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }
}
```

### LookupItem DTO

All DTO properties are nullable for maximum flexibility:

```csharp
public record LookupItem
{
    public int? Id { get; init; }
    public string? Code { get; init; }
    public string? Value { get; init; }
    public int? DisplayOrder { get; init; }
    public int? ParentId { get; init; }
    public bool? IsDefault { get; init; }
    public bool? IsActive { get; init; }
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}
```

#### Required Columns

Your stored procedure MUST return these columns:

| Column | Type | Required | Description |
|--------|------|----------|-------------|
| `TableName` | string | ✅ Yes | Groups results (e.g., "Countries", "States") |
| `Id` | int? | No | Unique identifier |
| `Code` | string? | No | Short code (e.g., "US", "CA") |
| `Value` | string? | No | Display text |
| `DisplayOrder` | int? | No | Sort order |
| `ParentId` | int? | No | For hierarchical data |
| `IsDefault` | bool? | No | Default selection |
| `IsActive` | bool? | No | Active/inactive flag |
| `Description` | string? | No | Tooltip or help text |
| `Metadata` | string? | No | JSON string for custom data |

### Response Format

```json
{
  "status": "Success",
  "code": "200",
  "data": {
    "orderStatuses": [
      {
        "id": 1,
        "code": "PENDING",
        "value": "Pending",
        "displayOrder": 1,
        "parentId": null,
        "isDefault": true,
        "isActive": true,
        "description": "Order is pending confirmation",
        "metadata": null
      },
      {
        "id": 2,
        "code": "CONFIRMED",
        "value": "Confirmed",
        "displayOrder": 2,
        "parentId": null,
        "isDefault": false,
        "isActive": true,
        "description": "Order has been confirmed",
        "metadata": null
      }
    ],
    "paymentMethods": [
      {
        "id": 1,
        "code": "CASH",
        "value": "Cash",
        "displayOrder": 1,
        "parentId": null,
        "isDefault": true,
        "isActive": true,
        "description": null,
        "metadata": "{\"icon\":\"💵\",\"color\":\"#4CAF50\"}"
      }
    ]
  }
}
```

### Caching Strategy

#### Cache Key Format

**Format:** `lookups:{storedProcedure}:{module}:{context}`

**Examples:**
- `lookups:restaurant.getlookups:restaurant:general`
- `lookups:inventory.getlookups:warehouse:default`
- `lookups:hr.getlookups:employees:active`

**Benefits:**
- Unique per API and context
- Easy to invalidate specific lookups
- Supports multi-tenant scenarios

#### Cache Configuration

##### In-Memory Cache (Single Server)
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddMemoryCacheService();
```

##### Distributed Cache (Multi-Server)
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
});
builder.Services.AddDistributedCacheService();
```

#### Caching Behavior

- **Default TTL:** 30 minutes
- **Cache Type:** Configurable (in-memory or distributed)
- **Cache Invalidation:** Manual via `RefreshLookupsAsync()` (removes specific cache key)
- **Cache Miss:** Query hits database and populates cache
- **Cache Hit:** Returns data from cache (< 1ms)

### Performance Considerations

#### Caching Performance
- **Cache hit:** < 1ms response time
- **Cache miss:** SP execution time + mapping time
- **Cache expiration:** 30 minutes default

#### Database Performance
- **Query Type:** Stored procedures (optimized)
- **Connection:** Reuses existing `IUnitOfWork` connection
- **Result Mapping:** Efficient DataTable → LINQ projection

#### Scalability
- **In-Memory Cache:** Good for single-server deployments
- **Distributed Cache:** Required for multi-server/load-balanced scenarios
- **Cache Warming:** First request per key hits database

### Error Handling

**Strategy:** Return `Result<T, DomainError>` pattern

**Benefits:**
- ✅ Type-safe error handling
- ✅ No exceptions for business logic errors
- ✅ Consistent with Acontplus patterns
- ✅ Easy to map to HTTP responses

**Error Codes:**
- `LOOKUPS_GET_ERROR` - Error retrieving lookups
- `LOOKUPS_REFRESH_ERROR` - Error refreshing cache
- `LOOKUPS_EMPTY` - No data returned from query

### Migration Checklist

#### From Existing Code

1. ✅ Update Dependencies
   - Ensure your API references `Acontplus.Services` NuGet package
   - Ensure your API references `Acontplus.Infrastructure` NuGet package
   - Ensure your API references `Acontplus.Core` NuGet package

2. ✅ Register Services
   - Add cache service registration
   - Add lookup service registration

3. ✅ Update/Create Stored Procedure
   - Ensure it returns required columns
   - Test stored procedure returns data correctly

4. ✅ Update Controller
   - Inject `ILookupService`
   - Update GET endpoint
   - Add refresh endpoint

5. ✅ Remove Old Code
   - Remove old `LookupService` class (if exists in your API)
   - Remove old `ILookupService` interface (if exists in your API)
   - Remove old `LookupItem` DTO (if exists in your API)
   - Remove `ConcurrentDictionary` caching logic

6. ✅ Testing
   - Unit test: Service registration
   - Integration test: GET lookups endpoint
   - Integration test: Refresh lookups endpoint
   - Integration test: Cache is working
   - Load test: Multiple concurrent requests

### Security Considerations

#### SQL Injection
- ✅ Uses parameterized stored procedures
- ✅ Filter values are passed as parameters
- ✅ No dynamic SQL construction

#### Data Access
- ✅ Respects existing `IUnitOfWork` security
- ✅ No elevation of privileges
- ✅ Uses application's database context

#### Cache Poisoning
- ✅ Cache keys are deterministic
- ✅ No user input in cache keys (normalized)
- ✅ Cache expiration prevents stale data

### Troubleshooting

#### Cache not working
- Verify `ICacheService` is registered
- Check logs for cache errors
- Ensure Redis is running (if using distributed cache)

#### Missing columns
- Check stored procedure returns all required columns
- Verify column names match exactly (case-sensitive in PostgreSQL)

#### Slow performance
- Add indexes to lookup tables
- Check stored procedure execution plan
- Consider cache warming on startup

#### Memory issues
- Use distributed cache instead of in-memory
- Reduce cache TTL
- Limit lookup data size

### Live Demo

See `apps/src/Demo.Api/Endpoints/Core/LookupEndpoints.cs` for a working example.

### References

- **Live Example**: `apps/src/Demo.Api` - Complete working implementation
- **Package**: `Acontplus.Services` - Service implementation
- **DTO**: `Acontplus.Core/Dtos/Responses/LookupItem.cs` - Shared DTO

### Caching Service

> **Requires**: `Acontplus.Infrastructure` package

```csharp
public class ProductService
{
    private readonly ICacheService _cache;

    public ProductService(ICacheService cache) => _cache = cache;

    public async Task<Product?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";

        // Async caching with factory pattern
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () => await _repository.GetByIdAsync(id),
            TimeSpan.FromMinutes(30)
        );
    }
}
```

### Device Detection

```csharp
public class ProductController : ControllerBase
{
    private readonly IDeviceDetectionService _deviceDetection;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var capabilities = _deviceDetection.GetDeviceCapabilities(userAgent);

        var products = capabilities.IsMobile
            ? await _productService.GetMobileProductsAsync()
            : await _productService.GetDesktopProductsAsync();

        return Ok(products);
    }
}
```

### Request Context Management

```csharp
public class OrderController : ControllerBase
{
    private readonly IRequestContextService _requestContext;

    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var correlationId = _requestContext.GetCorrelationId();
        var tenantId = _requestContext.GetTenantId();
        var clientId = _requestContext.GetClientId();

        _logger.LogInformation("Creating order for tenant {TenantId}", tenantId);

        return Ok(new { OrderId = request.OrderId, CorrelationId = correlationId });
    }
}
```

### Context Extensions

```csharp
public class AdvancedController : ControllerBase
{
    [HttpGet("context-info")]
    public IActionResult GetContextInfo()
    {
        // HTTP context extensions
        var userAgent = HttpContext.GetUserAgent();
        var ipAddress = HttpContext.GetClientIpAddress();
        var requestPath = HttpContext.GetRequestPath();

        // Claims principal extensions
        var userId = User.GetUserId();
        var email = User.GetEmail();
        var roles = User.GetRoles();
        var isAdmin = User.HasRole("admin");

        return Ok(new
        {
            Request = new { UserAgent = userAgent, IpAddress = ipAddress, Path = requestPath },
            User = new { UserId = userId, Email = email, Roles = roles, IsAdmin = isAdmin }
        });
    }
}
```

### Security Headers

```csharp
public class SecurityController : ControllerBase
{
    private readonly ISecurityHeaderService _securityHeaders;

    [HttpGet("headers")]
    public IActionResult GetRecommendedHeaders()
    {
        var headers = _securityHeaders.GetRecommendedHeaders(isDevelopment: false);
        var cspNonce = _securityHeaders.GenerateCspNonce();

        return Ok(new { Headers = headers, CspNonce = cspNonce });
    }
}
```

## 🔒 Security & Authorization

### Authorization Policies

```csharp
[Authorize(Policy = "RequireClientId")]
[HttpGet("secure")]
public IActionResult SecureEndpoint()
{
    return Ok("Access granted");
}

[Authorize(Policy = "RequireTenant")]
[HttpGet("tenant-data")]
public IActionResult GetTenantData()
{
    return Ok("Tenant-specific data");
}

[Authorize(Policy = "MobileOnly")]
[HttpGet("mobile-only")]
public IActionResult MobileOnlyEndpoint()
{
    return Ok("Mobile access only");
}
```

## 📊 Health Checks

Access comprehensive health information at `/health`:

```json
{
  "status": "Healthy",
  "results": {
    "request-context": {
      "status": "Healthy",
      "description": "Request context service is fully operational"
    },
    "security-headers": {
      "status": "Healthy",
      "description": "Security header service is operational"
    },
    "device-detection": {
      "status": "Healthy",
      "description": "Device detection service is fully operational"
    },
    "cache": {
      "status": "Healthy",
      "description": "Cache service is fully operational",
      "data": {
        "totalEntries": 150,
        "hitRatePercentage": 85.5
      }
    }
  }
}
```

## 🔐 JWT Authentication Usage

### Quick Start

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add JWT authentication with one line
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Configuration

```json
{
  "JwtSettings": {
    "Issuer": "https://auth.acontplus.com",
    "Audience": "api.acontplus.com",
    "SecurityKey": "your-super-secret-key-at-least-32-characters-long",
    "ClockSkew": "5",
    "RequireHttps": "true"
  }
}
```

### Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecureController : ControllerBase
{
    [HttpGet("data")]
    public IActionResult GetSecureData()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        return Ok(new {
            Message = "Secure data accessed",
            UserId = userId,
            Email = email
        });
    }
}
```

## 📚 API Reference

### Core Services

- `IRequestContextService` - Request context management and correlation
- `ISecurityHeaderService` - HTTP security headers and CSP management
- `IDeviceDetectionService` - Device type detection and capabilities

### Configuration

- `RequestContextConfiguration` - Request context and security settings
- `JwtSettings` - JWT authentication configuration

### Middleware

- `RequestContextMiddleware` - Request context extraction
- `CspNonceMiddleware` - CSP nonce generation
- `ApiExceptionMiddleware` - Global exception handling

## 🤝 Contributing

When adding new features:

1. Follow the established patterns (Services, Filters, Policies)
2. Add comprehensive logging
3. Include functional health checks for new services
4. Update this documentation with configuration examples
5. Add unit tests for new functionality

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## 📋 Package Comparison

| Feature | Acontplus.Services | Acontplus.Infrastructure |
|---------|-------------------|-------------------------|
| Request Context | ✅ | ❌ |
| Security Headers | ✅ | ❌ |
| Device Detection | ✅ | ❌ |
| JWT Authentication | ✅ | ❌ |
| Authorization Policies | ✅ | ❌ |
| Caching | ❌ | ✅ |
| Circuit Breaker | ❌ | ✅ |
| Retry Policies | ❌ | ✅ |
| HTTP Client Factory | ❌ | ✅ |
| Rate Limiting | ❌ | ✅ |

## 🎯 Best Practices

### ✅ Do's
- Use `AddApplicationServices()` for application-level concerns
- Use `AddInfrastructureServices()` for infrastructure concerns
- **NEW**: Let DomainExceptions bubble up for simpler code
- **NEW**: Use Result pattern for complex workflows
- Always validate client IDs and tenant IDs in multi-tenant scenarios
- Configure CSP policies carefully to avoid breaking functionality
- Monitor health check endpoints regularly
- Use correlation IDs for request tracking across services

### ❌ Don'ts
- Don't disable security headers in production
- Don't use weak JWT security keys (minimum 32 characters)
- Don't expose internal errors in API responses
- Don't cache sensitive user data
- Don't ignore health check failures
- Don't use generic cache keys
- **NEW**: Don't catch and swallow DomainExceptions (let middleware handle them)

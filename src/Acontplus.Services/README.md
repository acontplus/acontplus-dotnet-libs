# Acontplus.Services

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Services.svg)](https://www.nuget.org/packages/Acontplus.Services)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A comprehensive .NET service library providing business-grade patterns, security, device detection, request management, audit context, and **intelligent exception handling** for ASP.NET Core applications. Built with modern .NET 10 features and best practices.

> **💡 Infrastructure Services**: For caching, circuit breakers, resilience patterns, and HTTP client factory, use **[Acontplus.Infrastructure](https://www.nuget.org/packages/Acontplus.Infrastructure)**

## 🚀 Features

### 🏗️ Service Architecture Patterns

- **Service Layer**: Clean separation of concerns with dependency injection
- **Lookup Service**: Cached lookup/reference data management with flexible SQL mapping
- **Action Filters**: Reusable cross-cutting concerns (validation, logging, security)
- **Authorization Policies**: Fine-grained access control for multi-tenant scenarios
- **Middleware Pipeline**: Properly ordered middleware for security and context management
- **Audit Context**: Automatic audit field population from HTTP request claims

### 🛡️ Advanced Exception Handling

- **Flexible Design**: Works with or without catch blocks — your choice
- **Smart Exception Translation**: Preserves custom error codes from business logic
- **DomainException Support**: Automatic handling of domain exceptions with proper HTTP status codes
- **Inner Exception Unwrapping**: Finds DomainException/ValidationException/ApiException in nested exception chains
- **Consistent API Responses**: Standardized error format with categories and severity
- **Intelligent Logging**: Context-aware logging with appropriate severity levels
- **Distributed Tracing**: Correlation IDs and trace IDs for request tracking
- **Multi-tenancy Support**: Tenant ID tracking across requests

### 🔒 Security & Compliance

- **Security Headers**: Comprehensive HTTP security header management (via NetEscapades.AspNetCore.SecurityHeaders)
- **Content Security Policy**: Permissive and Strict CSP modes with nonce generation
- **Client Validation**: Client-ID based access control with whitelist support
- **Tenant Isolation**: Multi-tenant security policies with user-tenant cross-validation
- **JWT Authentication**: Enterprise-grade JWT token validation with multi-audience support

### 📱 Device & Context Awareness

- **Device Detection**: Smart device type detection from headers, legacy headers, and user agents
- **Device Capabilities**: OS, browser, version, and touch support detection
- **Request Context**: Correlation IDs, tenant isolation, and request tracking
- **Device-Aware Policies**: Mobile, tablet, and desktop-aware authorization policies

### 👤 User & Audit Context

- **UserContext**: Typed access to user claims (userId, email, role, custom claims)
- **HttpAuditContext**: Automatic audit identity resolution from JWT claims for persistence layers
- **ClaimsPrincipal Extensions**: Generic `GetClaimValue<T>()` for any claim type

### ⚙️ Configuration

- **ApplicationConfigurationBuilder**: Merged configuration with Azure Key Vault, shared settings, and environment-specific files
- **JsonConfigurationService**: Centralized JSON serialization with strict/permissive modes
- **RequestContextConfiguration**: Security headers, CSP, and client validation settings

### 📊 Observability

- **Request Logging**: Structured logging with performance metrics and slow-request warnings
- **Health Checks**: Comprehensive health monitoring for request context, security headers, and device detection

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
<PackageReference Include="Acontplus.Services" Version="x.x.x" />
<PackageReference Include="Acontplus.Infrastructure" Version="x.x.x" />
```

## 🎯 Quick Start

### 1. Add to Your Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Load merged configuration (appsettings + shared settings + Azure Key Vault)
var configuration = ApplicationConfigurationBuilder.Load();

// Add application services (context, security, device detection)
builder.Services.AddApplicationServices(configuration);

// Add JWT authentication
builder.Services.AddJwtAuthentication(configuration);

// Add authorization policies
builder.Services.AddAuthorizationPolicies(new List<string> { "web-app", "mobile-app" });

// Add infrastructure services (caching, resilience, HTTP clients)
builder.Services.AddInfrastructureServices(configuration);

// Add controllers with application filters and JSON config
builder.Services.AddApplicationMvc();

var app = builder.Build();

// Use application middleware pipeline (security headers + CSP + context + exception handling)
app.UseApplicationMiddleware(builder.Environment);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### 2. Exception Handling — No Catch Needed

```csharp
// Business Layer — Just throw, middleware handles everything
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
  "errors": [
    {
      "code": "CUSTOMER_NOT_FOUND",
      "message": "Customer not found",
      "category": "business",
      "severity": "warning"
    }
  ],
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
  "JwtSettings": {
    "Issuer": "https://auth.yourapp.com",
    "Audience": ["api.yourapp.com", "admin.yourapp.com"],
    "SecurityKey": "your-super-secret-key-at-least-32-characters-long",
    "ClockSkew": "5",
    "RequireHttps": "true"
  },
  "RequestContext": {
    "EnableSecurityHeaders": true,
    "FrameOptionsDeny": true,
    "ReferrerPolicy": "strict-origin-when-cross-origin",
    "RequireClientId": false,
    "AnonymousClientId": "anonymous",
    "AllowedClientIds": ["web-app", "mobile-app"],
    "Csp": {
      "AllowedImageSources": ["https://cdn.example.com"],
      "AllowedStyleSources": ["https://fonts.googleapis.com"],
      "AllowedFontSources": ["https://fonts.gstatic.com"],
      "AllowedScriptSources": ["https://cdn.jsdelivr.net"],
      "AllowedConnectSources": ["https://api.yourdomain.com"],
      "AllowedFrameSources": ["https://www.youtube-nocookie.com"],
      "AllowedMediaSources": [],
      "AllowedBaseUriSources": [],
      "AllowedFormActionSources": []
    }
  },
  "Security": {
    "UseStrictCSP": false
  }
}
```

## 🎯 Usage Examples

### 🟢 Basic Usage — Simple Setup

Perfect for small applications or getting started quickly.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationMvc();

var app = builder.Build();

app.UseApplicationMiddleware(builder.Environment);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### 🟡 Intermediate Usage — Granular Control

For applications that need fine-grained control over services and middleware.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services individually
builder.Services.AddRequestContext(builder.Configuration);
builder.Services.AddSecurityHeaders();
builder.Services.AddDeviceDetection();
builder.Services.AddLookupService();

// Add JWT + authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies(new List<string> { "web-app", "mobile-app" });

// Add health checks
builder.Services.AddApplicationHealthChecks(builder.Configuration);

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
app.UseMiddleware<RequestContextMiddleware>();
app.UseAcontplusExceptionHandling(options =>
{
    options.IncludeRequestDetails = true;
    options.LogRequestBody = app.Environment.IsDevelopment();
    options.IncludeDebugDetailsInResponse = app.Environment.IsDevelopment();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
```

### 🔴 Enterprise Usage — Full Configuration

Complete setup for enterprise applications with all features enabled.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Load merged configuration (appsettings + shared settings + Azure Key Vault)
var configuration = ApplicationConfigurationBuilder.Load();

// Application services
builder.Services.AddApplicationServices(configuration);
builder.Services.AddInfrastructureServices(configuration);

// Authentication & authorization
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddAuthorizationPolicies(new List<string>
{
    "web-app", "mobile-app", "admin-portal", "api-client"
});

// Health checks
builder.Services.AddApplicationHealthChecks(configuration);

// MVC with global filters and JSON config
builder.Services.AddApplicationMvc();
builder.Services.AddApiExplorer();

var app = builder.Build();

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
app.Run();
```

## 📚 Core Services Reference

### Services & Interfaces

| Service                  | Interface                 | Description                                                         |
| ------------------------ | ------------------------- | ------------------------------------------------------------------- |
| `RequestContextService`  | `IRequestContextService`  | Request context management, correlation IDs, tenant/client tracking |
| `SecurityHeaderService`  | `ISecurityHeaderService`  | HTTP security headers, CSP nonce generation, header validation      |
| `DeviceDetectionService` | `IDeviceDetectionService` | Device type detection, capabilities, header validation              |
| `LookupService`          | `ILookupService`          | Cached lookup/reference data from stored procedures                 |
| `UserContext`            | `IUserContext`            | Typed access to current user claims from HTTP context               |
| `HttpAuditContext`       | `IAuditContext`           | Audit identity resolution for persistence layers                    |

### Extension Methods

| Extension                                    | Description                                                                |
| -------------------------------------------- | -------------------------------------------------------------------------- |
| `AddApplicationServices(config)`             | Registers all core services (context, security, device detection, filters) |
| `AddApplicationMvc(enableGlobalFilters)`     | Configures MVC with filters and JSON serialization                         |
| `AddApplicationHealthChecks(config)`         | Adds health checks for application services                                |
| `AddAuthorizationPolicies(allowedClientIds)` | Registers all authorization policies                                       |
| `AddApiExplorer()`                           | Configures API explorer for documentation tools                            |
| `UseApplicationMiddleware(env)`              | Configures complete middleware pipeline                                    |
| `AddJwtAuthentication(config)`               | Configures JWT Bearer authentication with multi-audience support           |
| `AddRequestContext(config)`                  | Registers request context service individually                             |
| `AddSecurityHeaders()`                       | Registers security header service individually                             |
| `AddDeviceDetection()`                       | Registers device detection service individually                            |
| `AddLookupService()`                         | Registers lookup service individually                                      |
| `UseSecurityHeaders(env)`                    | Applies security header policies (HSTS in production)                      |
| `UseAcontplusExceptionHandling(options)`     | Adds global exception handling middleware                                  |

### Action Filters

| Filter                       | Description                                                            |
| ---------------------------- | ---------------------------------------------------------------------- |
| `ValidationActionFilter`     | Automatic model validation with standardized `ApiResponse` errors      |
| `RequestLoggingActionFilter` | Request/response logging with duration and slow-request warnings (>5s) |
| `SecurityHeaderActionFilter` | Security header injection and post-action validation                   |

### Authorization Policies

| Policy                       | Description                                          |
| ---------------------------- | ---------------------------------------------------- |
| `RequireClientId`            | Validates Client-Id header presence and whitelist    |
| `RequireClientIdOrAnonymous` | Client-Id validation with anonymous fallback         |
| `RequireTenant`              | Requires Tenant-Id header                            |
| `ValidateTenantAccess`       | Validates user's tenant claim matches request tenant |
| `OptionalTenant`             | Tenant-Id header is optional                         |
| `MobileOnly`                 | Restricts access to mobile devices                   |
| `MobileAndTablet`            | Restricts access to mobile and tablet devices        |
| `DesktopOnly`                | Restricts access to desktop devices                  |
| `KnownDevicesOnly`           | Excludes unknown device types                        |

### Middleware Pipeline

The `UseApplicationMiddleware()` extension configures middleware in this order:

1. **SecurityHeaders** — HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, CSP
2. **CspNonceMiddleware** — Generates per-request CSP nonce (accessible via `HttpContext.GetCspNonce()`)
3. **RequestContextMiddleware** — Extracts request/correlation/tenant IDs, client ID, device type
4. **ApiExceptionMiddleware** — Global exception handling with standardized responses

## 🚀 Feature Details

### ApplicationConfigurationBuilder

Builds a merged `IConfiguration` with the following priority (lowest → highest):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. Shared settings file — `sharedsettings.{Environment}.json` from platform shared folder
5. Azure Key Vault — activated when `KeyVault:VaultUri` or `KEYVAULT_URI` env var is set

```csharp
// Program.cs
var configuration = ApplicationConfigurationBuilder.Load();
```

**Key Vault secret naming convention:** use double-dash as hierarchy separator:

- `JwtSettings--SecurityKey`
- `ConnectionStrings--DefaultConnection`

**Shared settings path resolution:**

- Windows: `SharedPaths:Windows` from appsettings
- Linux: `SharedPaths:Linux` from appsettings
- macOS: `SharedPaths:OSX` from appsettings
- Override: `SHARED_SETTINGS_PATH` environment variable

**Azure Key Vault setup:**

- Local development: leave `KeyVault:VaultUri` unset, use User Secrets
- Azure-hosted: set `KeyVault:VaultUri` and assign Managed Identity the _Key Vault Secrets User_ role
- User-assigned identity: also set `KeyVault:ManagedIdentityClientId`

### JWT Authentication

Supports single or multiple audiences, configurable clock skew, and HTTPS enforcement.

```csharp
// One-line registration
builder.Services.AddJwtAuthentication(builder.Configuration);
```

```json
{
  "JwtSettings": {
    "Issuer": "https://auth.yourapp.com",
    "Audience": ["api.yourapp.com", "admin.yourapp.com"],
    "SecurityKey": "your-super-secret-key-at-least-32-characters-long",
    "ClockSkew": "5",
    "RequireHttps": "true"
  }
}
```

**Security features enabled by default:**

- `RequireExpirationTime`
- `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`
- `RequireSignedTokens`, `ValidateTokenReplay`
- `SaveToken = false`, `IncludeErrorDetails = false`

### User & Audit Context

```csharp
// Register in DI (done automatically by AddApplicationServices)
services.AddScoped<IUserContext, UserContext>();
services.AddScoped<IAuditContext, HttpAuditContext>();

// Use in services
public class OrderService
{
    private readonly IUserContext _userContext;

    public OrderService(IUserContext userContext) => _userContext = userContext;

    public async Task CreateOrderAsync(OrderRequest request)
    {
        var userId = _userContext.GetUserId();
        var email = _userContext.GetEmail();
        var companyId = _userContext.GetClaimValue<int>("companyId");
        // ...
    }
}
```

`HttpAuditContext` is designed for persistence layers (e.g., EF Core `SaveChangesAsync`) to automatically populate audit fields (`CreatedBy`, `ModifiedBy`, `IsMobile`). It safely returns `null` for unauthenticated or background operations.

### JsonConfigurationService

Centralized JSON serialization configuration for both Minimal APIs and MVC controllers.

```csharp
// Configure for ASP.NET Core (both HttpJsonOptions and MVC JsonOptions)
JsonConfigurationService.ConfigureAspNetCore(services);

// Or with environment-aware formatting
JsonConfigurationService.ConfigureAspNetCore(services, isDevelopment: true);

// Or get options directly for manual use
var options = JsonConfigurationService.GetOptions(prettyFormat: false, strictMode: false);
```

**Default behavior:**

- `camelCase` property naming
- Enums serialized as camelCase strings
- Null values omitted (`WhenWritingNull`)
- Numbers allowed from strings
- Comments and trailing commas allowed

**Strict mode:**

- Case-sensitive property names
- No trailing commas or comments
- Numbers must be actual JSON numbers
- Null values included

### Content Security Policy (CSP)

Two CSP modes are available, controlled by `Security:UseStrictCSP`:

**Permissive mode** (default): Uses `unsafe-inline` and `unsafe-eval` — suitable for Angular/React SPAs.

**Strict mode**: Uses nonces instead of `unsafe-inline` — requires nonce injection in scripts/styles:

```csharp
// Access the per-request nonce in views or middleware
var nonce = HttpContext.GetCspNonce();
// Use in script tags: <script nonce="@nonce">...</script>
```

CSP sources are fully configurable via `RequestContext:Csp` in appsettings.

### Device Detection

Detects device type using a priority chain:

1. `Device-Type` header (preferred)
2. `X-Is-Mobile` header (legacy)
3. User-Agent string analysis

```csharp
public class ProductController : ControllerBase
{
    private readonly IDeviceDetectionService _deviceDetection;

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        // Full capabilities from user agent
        var userAgent = Request.Headers.UserAgent.ToString();
        var capabilities = _deviceDetection.GetDeviceCapabilities(userAgent);
        // capabilities.Type, capabilities.IsMobile, capabilities.Browser, capabilities.OperatingSystem

        // Or quick check from context (uses header priority chain)
        var deviceType = _deviceDetection.DetectDeviceType(HttpContext);
        var isMobile = _deviceDetection.IsMobileDevice(HttpContext);

        return Ok(new { DeviceType = deviceType, IsMobile = isMobile });
    }
}
```

### Request Context Management

The `RequestContextMiddleware` extracts and stores context from incoming requests:

| Header           | Context Item  | Fallback                               |
| ---------------- | ------------- | -------------------------------------- |
| `Request-Id`     | RequestId     | New GUID                               |
| `Correlation-Id` | CorrelationId | RequestId                              |
| `Tenant-Id`      | TenantId      | RequestId                              |
| `Client-Id`      | ClientId      | AnonymousClientId (if RequireClientId) |
| `Issuer`         | Issuer        | null                                   |
| `Device-Type`    | DeviceType    | User-Agent detection                   |

All headers are sanitized (trimmed, control characters removed).

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
        var deviceType = _requestContext.GetDeviceType();
        var isMobile = _requestContext.IsMobileRequest();

        // Full context as dictionary
        var context = _requestContext.GetRequestContext();

        return Ok(new { CorrelationId = correlationId });
    }
}
```

### Security Headers

Applied automatically via `UseSecurityHeaders(env)`:

| Header                      | Value                                                  | Notes                               |
| --------------------------- | ------------------------------------------------------ | ----------------------------------- |
| `X-Content-Type-Options`    | `nosniff`                                              | Always                              |
| `X-Frame-Options`           | `DENY`                                                 | Configurable via `FrameOptionsDeny` |
| `Referrer-Policy`           | `strict-origin-when-cross-origin`                      | Configurable                        |
| `X-XSS-Protection`          | `1; mode=block`                                        | Always                              |
| `Permissions-Policy`        | `camera=(), microphone=(), geolocation=(), payment=()` | Always                              |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains`                  | Production only                     |
| `Content-Security-Policy`   | Configurable                                           | Permissive or Strict mode           |
| Server header               | Removed                                                | Always                              |

### Authorization Policies

```csharp
// Register policies
builder.Services.AddAuthorizationPolicies(new List<string> { "web-app", "mobile-app" });

// Use in controllers
[Authorize(Policy = "RequireClientId")]
[HttpGet("secure")]
public IActionResult SecureEndpoint() => Ok("Access granted");

[Authorize(Policy = "RequireTenant")]
[HttpGet("tenant-data")]
public IActionResult GetTenantData() => Ok("Tenant-specific data");

[Authorize(Policy = "MobileOnly")]
[HttpGet("mobile-only")]
public IActionResult MobileOnlyEndpoint() => Ok("Mobile access only");

[Authorize(Policy = "ValidateTenantAccess")]
[HttpGet("my-data")]
public IActionResult GetMyData() => Ok("Tenant-validated data");
```

### ClaimsPrincipal Extensions

```csharp
// Built-in typed accessors
var userId = User.GetUserId();           // int (from NameIdentifier)
var email = User.GetEmail();             // string? (from Email claim)
var username = User.GetUsername();       // string? (from Name claim)
var roleName = User.GetRoleName();       // string? (from Role claim)

// Generic claim accessor — works with any claim and type
var companyId = User.GetClaimValue<int>("companyId");
var isAdmin = User.GetClaimValue<bool>("isAdmin");
var tenantGuid = User.GetClaimValue<Guid>("tenantId");
```

### Exception Handling Details

The `ApiExceptionMiddleware` handles exceptions in this priority:

1. **ValidationException** → 400 with field-level errors
2. **DomainException** → HTTP status mapped from `ErrorType` (NotFound→404, Forbidden→403, etc.)
3. **ApiException** → Uses the exception's `StatusCode` directly
4. **Unhandled exceptions** → Searches inner exception chain for known types, falls back to 500

**Features:**

- Correlation ID and Tenant ID in every error response
- Activity/trace ID for distributed tracing
- Debug details (stack trace, inner exception) only in Development
- Request body logging (opt-in, Development only)
- Appropriate log levels per exception type (Warning for 4xx, Error for 5xx)

```csharp
// Configure exception handling options
app.UseAcontplusExceptionHandling(options =>
{
    options.IncludeRequestDetails = true;           // Log method + path
    options.LogRequestBody = false;                 // Caution: sensitive data
    options.IncludeDebugDetailsInResponse = false;  // Stack traces in response
});
```

## 📚 Lookup Service — Complete Guide

### Overview

The `LookupService` is a reusable, cached service for managing lookup/reference data across all Acontplus APIs. It works seamlessly with both PostgreSQL and SQL Server through the `IUnitOfWork` abstraction.

### Architecture

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
Store in Cache (30-min TTL)
    ↓
Return Result<T, DomainError>
```

### Quick Start

```csharp
// 1. Register services
builder.Services.AddLookupService();

// 2. Use in controller
[ApiController]
[Route("api/[controller]")]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
        => _lookupService = lookupService;

    [HttpGet]
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
                ["context"] = context ?? "general",
                ["userRoleId"] = User.GetClaimValue<int>("userRoleId"),
                ["userId"] = User.GetClaimValue<int>("userId"),
                ["companyId"] = User.GetClaimValue<int>("companyId")
            }
        };

        var result = await _lookupService.GetLookupsAsync(
            "YourSchema.GetLookups", filterRequest, cancellationToken);

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshLookups(CancellationToken ct)
    {
        var result = await _lookupService.RefreshLookupsAsync(
            "YourSchema.GetLookups", new FilterRequest(), ct);

        return result.Match(
            success => Ok(ApiResponse.Success(success)),
            error => BadRequest(ApiResponse.Failure(error)));
    }
}
```

### LookupItem DTO

All properties are nullable for maximum flexibility:

```csharp
public record LookupItem(
    int? Id,
    string? Code,
    string? Value,
    int? DisplayOrder,
    int? ParentId,
    bool? IsDefault,
    bool? IsActive,
    string? Description,
    string? Metadata
);
```

#### Required Columns from Stored Procedure

| Column         | Type    | Required | Description                                  |
| -------------- | ------- | -------- | -------------------------------------------- |
| `TableName`    | string  | ✅ Yes   | Groups results (e.g., "Countries", "States") |
| `Id`           | int?    | No       | Unique identifier                            |
| `Code`         | string? | No       | Short code (e.g., "US", "CA")                |
| `Value`        | string? | No       | Display text                                 |
| `DisplayOrder` | int?    | No       | Sort order                                   |
| `ParentId`     | int?    | No       | For hierarchical data                        |
| `IsDefault`    | bool?   | No       | Default selection                            |
| `IsActive`     | bool?   | No       | Active/inactive flag                         |
| `Description`  | string? | No       | Tooltip or help text                         |
| `Metadata`     | string? | No       | JSON string for custom data                  |

### SQL Examples

**SQL Server:**

```sql
CREATE PROCEDURE [YourSchema].[GetLookups]
    @Module NVARCHAR(100) = NULL,
    @Context NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'Countries' AS TableName,
           Id, Code, [Name] AS [Value], DisplayOrder,
           NULL AS ParentId, IsDefault, IsActive,
           Description, NULL AS Metadata
    FROM Countries WHERE IsActive = 1
    ORDER BY DisplayOrder;
END
```

**PostgreSQL:**

```sql
CREATE OR REPLACE FUNCTION your_schema.get_lookups(
    p_module VARCHAR DEFAULT NULL,
    p_context VARCHAR DEFAULT NULL
)
RETURNS TABLE (
    table_name VARCHAR, id INTEGER, code VARCHAR, value VARCHAR,
    display_order INTEGER, parent_id INTEGER, is_default BOOLEAN,
    is_active BOOLEAN, description TEXT, metadata JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 'countries'::VARCHAR, c.id, c.code, c.name, c.display_order,
           NULL::INTEGER, c.is_default, c.is_active, c.description, NULL::JSONB
    FROM your_schema.countries c WHERE c.is_active = TRUE
    ORDER BY c.display_order;
END;
$$ LANGUAGE plpgsql;
```

### Caching Strategy

**Cache key format:** `lookups:{storedProcedure}:{module}:{context}:{userRoleId}:{userId}:{companyId}`

- All segments are normalized (trimmed, lowercased, or defaulted to `"default"`)
- Each user/role/company combination gets isolated cache entries
- Prevents cache poisoning across tenants

**Configuration:**

```csharp
// In-memory (single server)
builder.Services.AddMemoryCache();
builder.Services.AddMemoryCacheService();

// Distributed (multi-server)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddDistributedCacheService();
```

**Behavior:**

- Default TTL: 30 minutes
- Cache invalidation: Manual via `RefreshLookupsAsync()` (removes specific key, then re-fetches)
- Cache hit: < 1ms response time
- Cache miss: SP execution time + mapping

### Response Format

```json
{
  "status": "Success",
  "code": "200",
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
    ],
    "paymentMethods": [
      {
        "id": 1,
        "code": "CASH",
        "value": "Cash",
        "displayOrder": 1,
        "isDefault": true,
        "isActive": true,
        "metadata": "{\"icon\":\"💵\",\"color\":\"#4CAF50\"}"
      }
    ]
  }
}
```

> **Note:** Table names are converted to camelCase in the response (e.g., `"OrderStatuses"` → `"orderStatuses"`).

## 📊 Health Checks

```csharp
builder.Services.AddApplicationHealthChecks(builder.Configuration);
app.MapHealthChecks("/health");
```

Registered health checks:

| Check              | Description                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| `request-context`  | Validates RequestContextService can resolve context data                |
| `security-headers` | Validates SecurityHeaderService can generate headers                    |
| `device-detection` | Tests device detection against known user agents (Chrome, Safari, iPad) |

Response example:

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
    }
  }
}
```

## 📋 Package Comparison

| Feature                   | Acontplus.Services | Acontplus.Infrastructure |
| ------------------------- | ------------------ | ------------------------ |
| Request Context           | ✅                 | ❌                       |
| Security Headers          | ✅                 | ❌                       |
| Device Detection          | ✅                 | ❌                       |
| JWT Authentication        | ✅                 | ❌                       |
| Authorization Policies    | ✅                 | ❌                       |
| Exception Handling        | ✅                 | ❌                       |
| User/Audit Context        | ✅                 | ❌                       |
| JSON Configuration        | ✅                 | ❌                       |
| App Configuration Builder | ✅                 | ❌                       |
| Lookup Service            | ✅                 | ❌                       |
| Caching                   | ❌                 | ✅                       |
| Circuit Breaker           | ❌                 | ✅                       |
| Retry Policies            | ❌                 | ✅                       |
| HTTP Client Factory       | ❌                 | ✅                       |
| Rate Limiting             | ❌                 | ✅                       |

## 🎯 Best Practices

### ✅ Do's

- Use `AddApplicationServices()` for the full application-level stack
- Use `AddJwtAuthentication()` instead of manual JWT configuration
- Let DomainExceptions bubble up — middleware handles them automatically
- Use Result pattern for complex workflows with multiple failure modes
- Use `ApplicationConfigurationBuilder.Load()` for merged configuration with Key Vault
- Configure CSP policies carefully per environment (strict for production, permissive for dev)
- Use correlation IDs for request tracking across services
- Register `HttpAuditContext` for automatic audit field population
- Use `GetClaimValue<T>()` for type-safe claim access

### ❌ Don'ts

- Don't disable security headers in production
- Don't use weak JWT security keys (minimum 32 characters)
- Don't expose internal errors in API responses (disable `IncludeDebugDetailsInResponse` in production)
- Don't catch and swallow DomainExceptions — let middleware handle them
- Don't use `ICacheService` or `ICircuitBreakerService` without referencing `Acontplus.Infrastructure`
- Don't use generic cache keys — include user/role/company for tenant isolation
- Don't ignore health check failures
- Don't enable `LogRequestBody` in production (sensitive data risk)

## 📁 Project Structure

```
Acontplus.Services/
├── Configuration/
│   ├── ApplicationConfigurationBuilder.cs   # Merged config (appsettings + Key Vault)
│   ├── JsonConfigurationService.cs          # Centralized JSON serialization
│   └── RequestContextConfiguration.cs      # Security headers & CSP config
├── Extensions/
│   ├── Authentication/
│   │   └── JwtAuthenticationExtensions.cs   # AddJwtAuthentication()
│   ├── Context/
│   │   ├── ClaimsPrincipalExtensions.cs     # GetClaimValue<T>(), GetUserId(), etc.
│   │   ├── HttpAuditContext.cs              # IAuditContext implementation for persistence layers
│   │   ├── HttpContextExtensions.cs         # Set/Get context items
│   │   └── UserContext.cs                   # IUserContext implementation
│   ├── Middleware/
│   │   └── GlobalExceptionHandlingExtensions.cs  # UseAcontplusExceptionHandling()
│   ├── Security/
│   │   └── SecurityHeaderPolicyExtensions.cs     # UseSecurityHeaders()
│   ├── ApplicationServiceExtensions.cs      # AddApplicationServices(), UseApplicationMiddleware()
│   └── ServiceExtensions.cs                 # Individual service registration
├── Filters/
│   ├── RequestLoggingActionFilter.cs        # Request logging with duration
│   ├── SecurityHeaderActionFilter.cs        # Security header injection
│   └── ValidationActionFilter.cs            # Model validation
├── Middleware/
│   ├── ApiExceptionMiddleware.cs            # Global exception handling
│   ├── CspNonceMiddleware.cs                # CSP nonce generation
│   ├── RequestContextMiddleware.cs          # Request context extraction
│   └── RequestLoggingMiddleware.cs          # HTTP request/response logging
├── Policies/
│   ├── DeviceTypePolicy.cs                  # Device-aware authorization
│   ├── RequireClientIdPolicy.cs             # Client-Id validation
│   └── TenantIsolationPolicy.cs            # Multi-tenant isolation
├── Services/
│   ├── Abstractions/
│   │   ├── IDeviceDetectionService.cs
│   │   ├── ILookupService.cs
│   │   └── ISecurityHeaderService.cs
│   └── Implementations/
│       ├── DeviceDetectionService.cs
│       ├── LookupService.cs
│       ├── RequestContextService.cs
│       └── SecurityHeaderService.cs
└── GlobalUsings.cs
```

## 🔗 Dependencies

- `Acontplus.Core` — Domain abstractions, DTOs, exceptions, Result pattern
- `Azure.Extensions.AspNetCore.Configuration.Secrets` — Azure Key Vault integration
- `Azure.Identity` — DefaultAzureCredential for Key Vault
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT Bearer authentication
- `Microsoft.IdentityModel.Tokens` — Token validation
- `NetEscapades.AspNetCore.SecurityHeaders` — Security header policies
- `System.IdentityModel.Tokens.Jwt` — JWT token handling

## 📖 Live Demo

See `apps/src/Demo.Api/Endpoints/Core/LookupEndpoints.cs` for a working implementation example.

# Acontplus.Core

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Core.svg)](https://www.nuget.org/packages/Acontplus.Core)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A cutting-edge .NET foundational library leveraging the latest C# language features and business patterns. Built with performance, type safety, and developer experience in mind. Focuses on pure domain logic with clean separation from API concerns.

> **Changelog:** see [`CHANGELOG.md`](../../CHANGELOG.md) at the repository root for the full version history.

## 🚀 Features

### 🎯 Language & Runtime Capabilities

- **Collection Expressions** - `[]` syntax for efficient collection initialization
- **Primary Constructors** - Concise record and class definitions
- **Required Properties** - Compile-time null safety with `required` keyword
- **Pattern Matching** - Advanced `switch` expressions and `is` patterns
- **Record Structs** - High-performance value types for DTOs and results
- **Nullable Reference Types** - Full compile-time null safety
- **Source Generators** - JSON serialization with AOT compilation support
- **Global Usings** - Clean namespace management with global using directives

### 🏗️ Architecture Patterns

- **Domain-Driven Design (DDD)** - Complete DDD implementation with C# features
- **Functional Result Pattern** - Railway-oriented programming with record structs
- **Repository Pattern** - Comprehensive data access with bulk operations
- **Dapper Repository** - Lightweight micro-ORM interface for high-performance queries
- **Specification Pattern** - Type-safe query composition with expressions
- **Event Sourcing Ready** - Domain events with event patterns
- **Warnings System** - Success with warnings pattern for complex business operations

### 📊 Advanced Data Patterns

- **Async Streaming** - `IAsyncEnumerable<T>` for memory-efficient processing
- **Projections** - Expression-based data transfer for performance
- **Bulk Operations** - High-performance batch processing interfaces
- **Smart Pagination** - Advanced pagination with search and filtering
- **JSON Utilities** - System.Text.Json with source generation
- **Repository Interfaces** - Complete repository abstractions with CRUD, specifications, and bulk operations
- **Clean Architecture** - No persistence dependencies, implementations provided in separate packages

## 🔥 Core Features

### 🌟 **Global Business Enums**

**17 comprehensive business enums** available globally across all applications - no more duplicate definitions!

#### **🔄 Process & Status Management**

- **`BusinessStatus`** - 13 lifecycle states (Draft → Active → Archived)
- **`Priority`** - 5 priority levels (Low → Emergency)
- **`DocumentType`** - 16 document types (Invoice, Contract, Report, etc.)
- **`EventType`** - 19 event types (Authentication, CRUD operations, Workflow, etc.)

#### **👤 Person & Demographics**

- **`Gender`** - 5 inclusive options (Male, Female, NonBinary, Other, NotSpecified)
- **`MaritalStatus`** - 8 relationship states (Single, Married, Divorced, etc.)
- **`Title`** - 12 honorifics (Mr, Mrs, Dr, Prof, Sir, Dame, etc.)

#### **🏢 Business & Organization**

- **`Industry`** - 19 industry classifications (Technology, Healthcare, Finance, etc.)
- **`CompanySize`** - 11 size categories (Startup → Multinational Corporation)

#### **💰 Financial & Commerce**

- **`Currency`** - 15 international currencies (USD, EUR + Latin American)
- **`PaymentMethodType`** - 12 payment method types (Cash, Cards, DigitalWallet, BNPL, etc.)

#### **🔐 Security & Access**

- **`UserRoleType`** - 7 role levels (Guest → User → Employee → Manager → Administrator → SuperAdmin → ServiceAccount)

#### **🌍 Internationalization**

- **`Language`** - 20 languages (Major world languages + Latin American Spanish)
- **`TimeZone`** - 16 time zones (UTC, regional + Latin American zones)

#### **📱 Communication & Content**

- **`CommunicationChannelType`** - 8 channel types (Email, SMS, Phone, Push, InstantMessaging, etc.)
- **`AddressType`** - 12 address categories (Home, Work, Billing, Shipping, etc.)
- **`ContentType`** - 20 media types (Text, Images, Videos, Documents, Archives)

```csharp
// ✅ Available everywhere via global using
public class Customer : BaseEntity
{
    public Gender Gender { get; set; }                    // 🌟 Global enum
    public Title Title { get; set; }                      // 🌟 Global enum
    public MaritalStatus MaritalStatus { get; set; }      // 🌟 Global enum
    public Language PreferredLanguage { get; set; }       // 🌟 Global enum
    public CommunicationChannelType PreferredChannel { get; set; } // 🌟 Global enum
}

public class Order : BaseEntity
{
    public BusinessStatus Status { get; set; }            // 🌟 Global enum
    public Priority Priority { get; set; }                // 🌟 Global enum
    public Currency Currency { get; set; }                // 🌟 Global enum
    public PaymentMethodType PaymentMethod { get; set; }  // 🌟 Global enum
}

public class UserAccount : BaseEntity
{
    public UserRoleType Role { get; set; }                // 🌟 Global enum
    public BusinessStatus Status { get; set; }            // 🌟 Global enum
}
```

### 🔄 **Comprehensive Result Pattern System**

**Complete Railway-Oriented Programming implementation** with functional composition, multiple error handling, and clean separation of concerns.

#### **🎯 Core Result Types**

```csharp
// Generic Result with custom error type
Result<TValue, TError>

// Result with fixed DomainError (most common)
Result<TValue>

// Multiple errors support
Result<TValue, DomainErrors>

// Success with warnings pattern
SuccessWithWarnings<TValue>
```

#### **✨ Current API — Create Results Properly**

```csharp
// ✅ CURRENT: Single error
public static Result<User> GetUser(int id) =>
    id <= 0
        ? Result<User>.Failure(DomainError.Validation("INVALID_ID", "ID must be positive"))
        : Result<User>.Success(new User { Id = id });

// ✅ CURRENT: Multiple errors
public static Result<User, DomainErrors> ValidateUser(CreateUserRequest request)
{
    var errors = new List<DomainError>();

    if (string.IsNullOrEmpty(request.Name))
        errors.Add(DomainError.Validation("NAME_REQUIRED", "Name required"));

    if (string.IsNullOrEmpty(request.Email))
        errors.Add(DomainError.Validation("EMAIL_REQUIRED", "Email required"));

    return errors.Count > 0
        ? Result<User, DomainErrors>.Failure(new DomainErrors(errors))
        : Result<User, DomainErrors>.Success(new User { Name = request.Name, Email = request.Email });
}
```

#### **🔧 Result Factory Methods**

```csharp
// Single error results
Result<Product>.Success(product);
Result<Product>.Failure(domainError);

// Multiple error results
Result<Product, DomainErrors>.Success(product);
Result<Product, DomainErrors>.Failure(domainErrors);

// Success with message
Result<Product, DomainErrors>.Success(product, "Product created successfully");
```

#### **⚡ Enhanced Functional Composition**

```csharp
// Railway-oriented programming with async/ValueTask support
public async Task<Result<OrderConfirmation>> ProcessOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
{
    return await ValidateOrderRequest(request)
        .Map(order => CalculateTotal(order))
        .MapAsync(order => ProcessPaymentAsync(order))
        .MapAsync(async (order, token) => await ReserveStockAsync(order, token), ct)
        .Map(order => GenerateConfirmation(order))
        .OnFailure(error => _logger.LogError("Order processing failed: {Error}", error));
}

// Pattern matching with improved ergonomics
public IActionResult HandleOrderResult(Result<Order> result)
{
    var (isSuccess, value, error) = result; // Deconstruct support

    return result.Match(
        success: order => Ok(order),
        failure: error => BadRequest(error.ToApiResponse<Order>())
    );
}

// Safe value access
public string GetOrderStatus(Result<Order> result)
{
    if (result.TryGetValue(out var order))
        return order.Status.ToString();

    if (result.TryGetError(out var error))
        return $"Error: {error.Message}";

    return "Unknown";
}

// Access success message
public IActionResult HandleOrderResultWithMessage(Result<Order, DomainErrors> result)
{
    if (result.IsSuccess)
    {
        var message = result.SuccessMessage ?? "Order processed successfully";
        return Ok(new { order = result.Value, message });
    }

    return BadRequest(result.Error);
}
```

#### **🔗 Advanced Chaining Operations**

```csharp
// Chain operations with enhanced error handling
var result = await GetUserAsync(userId)
    .Map(user => ValidateUser(user))
    .MapAsync(user => EnrichUserDataAsync(user))
    .MapAsync(async (user, ct) => await CallExternalApiAsync(user, ct), CancellationToken.None)
    .MapError(error => DomainError.External("API_ERROR", $"External service failed: {error.Code}"))
    .OnSuccess(user => _logger.LogInformation("User processed: {UserId}", user.Id))
    .OnFailure(error => _logger.LogError("User processing failed: {Error}", error));

// ValueTask support for high-performance scenarios
public async ValueTask<Result<ProcessedData>> ProcessDataAsync(RawData data)
{
    return await ValidateData(data)
        .BindAsync(async validData => await TransformDataAsync(validData))
        .TapAsync(async processedData => await LogProcessingAsync(processedData));
}
```

#### **🚨 Comprehensive Error Handling**

```csharp
// Error severity analysis and HTTP mapping
var errors = DomainErrors.Multiple(
    DomainError.Internal("DB_ERROR", "Database connection failed"),
    DomainError.Validation("INVALID_EMAIL", "Invalid email format")
);

var mostSevere = errors.GetMostSevereErrorType(); // Returns ErrorType.Internal
var httpStatus = mostSevere.ToHttpStatusCode();   // Returns 500

// Error filtering and analysis
var validationErrors = errors.GetErrorsOfType(ErrorType.Validation);
var hasServerErrors = errors.HasErrorsOfType(ErrorType.Internal);
var summary = errors.GetAggregateErrorMessage();

// Convert to API responses
var apiResponse = errors.ToApiResponse<ProductDto>();
```

#### **⚠️ Success with Warnings Pattern**

```csharp
// Enhanced success with warnings support
public async Task<Result<SuccessWithWarnings<List<Product>>>> ImportProductsAsync(List<ProductDto> dtos)
{
    var products = new List<Product>();
    var warnings = new List<DomainError>();

    foreach (var dto in dtos)
    {
        try
        {
            var product = await CreateProductAsync(dto);
            products.Add(product);
        }
        catch (ValidationException ex)
        {
            warnings.Add(DomainError.Validation("IMPORT_WARNING",
                $"Product {dto.Name} skipped: {ex.Message}"));
        }
    }

    var successWithWarnings = new SuccessWithWarnings<List<Product>>(
        products,
        new DomainWarnings(warnings)
    );

    return Result<SuccessWithWarnings<List<Product>>>.Success(successWithWarnings);
}

// Using extension helpers
var result = products.ToSuccessWithWarningsResult(warnings);
var resultWithMultiple = products.ToSuccessWithWarningsResult(warning1, warning2, warning3);
```

#### **🌐 HTTP Integration & Status Mapping**

```csharp
// Comprehensive HTTP status code mapping
var error = DomainError.Validation("INVALID_INPUT", "Input validation failed");
var statusCode = error.GetHttpStatusCode(); // Returns 422 (Unprocessable Entity)

// Built-in error type mappings:
ErrorType.Validation      → 422 Unprocessable Entity
ErrorType.NotFound        → 404 Not Found
ErrorType.Unauthorized    → 401 Unauthorized
ErrorType.Forbidden       → 403 Forbidden
ErrorType.Conflict        → 409 Conflict
ErrorType.RateLimited     → 429 Too Many Requests
ErrorType.RequestTimeout  → 408 Request Timeout   // client-side timeout
ErrorType.Internal        → 500 Internal Server Error
ErrorType.External        → 502 Bad Gateway
ErrorType.ServiceUnavailable → 503 Service Unavailable
ErrorType.Timeout         → 504 Gateway Timeout   // upstream/external timeout
// ... and more (full mapping in ErrorTypeExtensions.cs)
```

#### **🎨 Real-World Usage Examples**

```csharp
// ✅ Simple validation with current API
public Result<User> CreateUser(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        return DomainError.Validation("NAME_REQUIRED", "Name is required").ToResult<User>();

    if (!IsValidEmail(email))
        return DomainError.Validation("EMAIL_INVALID", "Invalid email format").ToResult<User>();

    return new User { Name = name, Email = email }.ToResult();
}

// ✅ Complex business logic with multiple validation
public async Task<Result<Order, DomainErrors>> ProcessOrderAsync(OrderRequest request)
{
    var validationErrors = new List<DomainError>();

    // Validate customer
    var customer = await _customerService.GetByIdAsync(request.CustomerId);
    if (customer is null)
        validationErrors.Add(DomainError.NotFound("CUSTOMER_NOT_FOUND", "Customer not found"));

    // Validate products
    foreach (var item in request.Items)
    {
        var product = await _productService.GetByIdAsync(item.ProductId);
        if (product is null)
            validationErrors.Add(DomainError.NotFound("PRODUCT_NOT_FOUND", $"Product {item.ProductId} not found"));
        else if (product.Stock < item.Quantity)
            validationErrors.Add(DomainError.Conflict("INSUFFICIENT_STOCK", $"Not enough stock for {product.Name}"));
    }

    if (validationErrors.Count > 0)
        return validationErrors.ToFailureResult<Order>();

    // Process order
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Items = request.Items,
        Status = BusinessStatus.Active
    };

    return Result<Order, DomainErrors>.Success(await _orderRepository.CreateAsync(order));
}

// ✅ Functional composition for complex workflows
public async Task<Result<InvoiceDto>> GenerateInvoiceAsync(int orderId, CancellationToken ct = default)
{
    return await GetOrderAsync(orderId)
        .MapAsync(order => ValidateOrderForInvoicingAsync(order))
        .MapAsync(order => CalculateInvoiceAmountsAsync(order))
        .MapAsync(async (invoice, token) => await ApplyTaxCalculationsAsync(invoice, token), ct)
        .MapAsync(invoice => GeneratePdfAsync(invoice))
        .Map(invoice => ConvertToDto(invoice))
        .OnSuccess(invoice => _logger.LogInformation("Invoice generated: {InvoiceId}", invoice.Id))
        .OnFailure(error => _logger.LogError("Invoice generation failed: {Error}", error));
}
```

#### **🔍 Advanced Repository Queries**

_For detailed examples of advanced repository queries, complex joins, and custom projections, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation._

#### **🔧 Dynamic Filtering with Predicates**

_For comprehensive examples of dynamic filtering, predicate creation, and filter utilities, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation._

#### **📄 Request Models for Clean APIs**

Following Clean Architecture principles, the library provides semantic request models without the "Dto" suffix for frontend-facing contracts:

```csharp
// ✅ FilterRequest - For non-paginated scenarios
public record FilterRequest
{
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
    public string? SearchTerm { get; init; }
    public IReadOnlyDictionary<string, object>? Filters { get; init; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(SearchTerm) && (Filters is null || !Filters.Any());
    public bool HasCriteria => !IsEmpty;

    // Helper methods for SQL parameter building
    public Dictionary<string, object>? BuildSqlParameters() => BuildFiltersWithPrefix("@");
}

// ✅ PaginationRequest - Extends FilterRequest with pagination
public record PaginationRequest : FilterRequest
{
    public int PageIndex { get; init; } = 1;        // Auto-validated (min: 1)
    public int PageSize { get; init; } = 10;        // Auto-validated (1-1000)

    public int Skip => (PageIndex - 1) * PageSize;
    public int Take => PageSize;
}

// 📊 Use Cases

// Non-Paginated Scenarios (FilterRequest):
// - Reports (export all matching records)
// - Data exports (CSV, Excel, PDF)
// - Autocomplete/dropdowns
// - Simple searches returning all results
// - Analytics queries
[HttpGet("products/export")]
public async Task<IActionResult> ExportProducts([FromQuery] FilterRequest filter)
{
    var products = await _productService.GetAllAsync(filter);
    return File(products.ToCsv(), "text/csv", "products.csv");
}

[HttpGet("customers/autocomplete")]
public async Task<IActionResult> AutocompleteCustomers([FromQuery] FilterRequest filter)
{
    var customers = await _customerService.SearchAsync(filter);
    return Ok(customers.Select(c => new { c.Id, c.Name }));
}

// Paginated Scenarios (PaginationRequest):
// - Product lists with pagination
// - Search results tables
// - Infinite scroll feeds
// - Large datasets requiring pagination
[HttpGet("products")]
public async Task<IActionResult> GetProducts([FromQuery] PaginationRequest pagination)
{
    var result = await _productService.GetPagedAsync(pagination);
    return Ok(result); // Returns PagedResult<Product>
}

[HttpPost("orders/search")]
public async Task<IActionResult> SearchOrders([FromBody] PaginationRequest request)
{
    var result = await _orderService.SearchPagedAsync(request);
    return Ok(result);
}

// 🎯 Naming Convention

// ✅ API Contracts (Frontend-facing) - No "Dto" suffix
FilterRequest           // Request from frontend
PaginationRequest       // Request from frontend
CreateOrderRequest      // Request from frontend
OrderResponse          // Response to frontend
PagedResult<T>         // Response to frontend

// ✅ Internal/Infrastructure - Keep "Dto" suffix
CommandOptionsDto      // Internal ADO.NET configuration
SqlParameterDto       // Internal SQL mapping
CacheEntryDto         // Internal caching structure

// This separation follows Clean Architecture:
// - Domain/API Layer: Business language, no technical suffixes
// - Infrastructure Layer: Technical implementation details, "Dto" acceptable
```

#### **🔧 Filter Request Helpers**

```csharp
// Building SQL parameters from filters
var filter = new FilterRequest
{
    SearchTerm = "laptop",
    Filters = new Dictionary<string, object>
    {
        ["category"] = "electronics",
        ["inStock"] = true,
        ["minPrice"] = 100
    }
};

// Get parameters with "@" prefix for SQL
var sqlParams = filter.BuildSqlParameters();
// Returns: { "@category": "electronics", "@inStock": true, "@minPrice": 100 }

// Check if filter has criteria
if (filter.HasCriteria)
{
    // Apply filters to query
    query = query.Where(p => /* filter logic */);
}
```

#### **📄 Request Extensions with Fluent APIs**

**FilterRequest Extensions** - For non-paginated scenarios:

```csharp
// Create filter requests with fluent API
var filter = new FilterRequest()
    .WithSearch("laptop")
    .WithSort("price", SortDirection.Asc)
    .WithFilter("category", "electronics")
    .WithFilter("inStock", true);

// Merge multiple filters
var additionalFilters = new Dictionary<string, object>
{
    ["brand"] = "Dell",
    ["rating"] = 4.5
};
var enrichedFilter = filter.WithFilters(additionalFilters);

// Usage in services
[HttpGet("products/export")]
public async Task<IActionResult> ExportProducts([FromQuery] FilterRequest filter)
{
    var products = await _productService.GetAllAsync(filter);
    return File(products.ToCsv(), "text/csv", "products.csv");
}
```

**PaginationRequest Extensions** - For paginated scenarios:

```csharp
// Create pagination with fluent API
var pagination = PaginationExtensions.Create(pageIndex: 1, pageSize: 20)
    .WithSearch("laptop")
    .WithSort("price", SortDirection.Asc)
    .WithFilter("category", "electronics")
    .WithFilter("inStock", true);

// Navigation helpers
var nextPage = pagination.NextPage();        // Page 2
var previousPage = pagination.PreviousPage(); // Page 1 (min)
var specificPage = pagination.ToPage(5);      // Jump to page 5

// Validation
var validated = pagination.Validate(); // Ensures valid page index and size

// Factory methods for common scenarios
var searchPagination = PaginationExtensions.CreateWithSearch("laptop", pageIndex: 1, pageSize: 10);
var sortedPagination = PaginationExtensions.CreateWithSort("price", SortDirection.Desc);
var filteredPagination = PaginationExtensions.CreateWithFilters(
    new Dictionary<string, object> { ["category"] = "electronics" }
);
```

#### **🌐 Minimal API Support**

For Minimal APIs, use the corresponding Query models from `Acontplus.Utilities`:

```csharp
using Acontplus.Utilities.Dtos;

// Automatic binding for Minimal APIs
app.MapGet("/api/products/export",
    async (FilterQuery filter, IProductService service) =>
    {
        var products = await service.GetAllAsync(filter);
        return Results.Ok(products);
    });

app.MapGet("/api/products",
    async (PaginationQuery pagination, IProductService service) =>
    {
        var result = await service.GetPagedAsync(pagination);
        return Results.Ok(result);
    });

// Query string example:
// GET /api/products?pageIndex=2&pageSize=20&sortBy=price&sortDirection=desc&searchTerm=laptop&filters[category]=electronics
```

_For detailed repository implementation examples and advanced queries, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation._

#### **Current Best Practices**

```csharp
// ✅ DO: Use Result factory methods or extension helpers
return Result<User>.Failure(DomainError.NotFound("USER_NOT_FOUND", $"User with ID {id} was not found"));
// OR
return DomainError.NotFound("USER_NOT_FOUND", $"User with ID {id} was not found").ToResult<User>();

// ✅ DO: Use pattern matching and deconstruction
var (isSuccess, user, error) = result;
if (isSuccess)
    ProcessUser(user!);

// ✅ DO: Use TryGet methods for safe access
if (result.TryGetValue(out var user))
    ProcessUser(user);

// ✅ DO: Chain operations for complex workflows
var result = await ValidateInput(input)
    .MapAsync(data => ProcessDataAsync(data))
    .Map(processed => FormatOutput(processed))
    .OnFailure(error => LogError(error));

// ✅ DO: Use DomainErrors for multiple validation errors
var errors = new List<DomainError>();
if (IsInvalid(name)) errors.Add(DomainError.Validation("INVALID_NAME", "Name invalid"));
if (IsInvalid(email)) errors.Add(DomainError.Validation("INVALID_EMAIL", "Email invalid"));

return errors.Count > 0
    ? errors.ToFailureResult<User>()
    : Result<User>.Success(CreateUser(name, email));
```

### 🔍 **Validation Utilities**

Comprehensive validation utilities for common business scenarios:

```csharp
// ── DataValidation (static helpers) ─────────────────────────────────────────
public static class DataValidation
{
    // String / format validators
    public static bool IsValidEmail(string? email);
    public static bool IsValidUrl(string? url);
    public static bool IsValidPhoneNumber(string? phoneNumber);
    public static bool IsValidJson(string? jsonString);
    public static bool IsValidXml(string? xml);

    // Sanitisation
    public static string RemoveSpecialCharacters(string text);

    // IP address
    public static string ValidateIpAddress(string? ipAddress); // strips ::ffff: prefix, returns "0.0.0.0" on invalid

    // ADO.NET / DataSet helpers
    public static object ToDbNullOrDefault(this object? obj);
    public static bool DataTableIsNull(DataTable? dt);
    public static bool DataSetIsNull(DataSet? ds, bool removeEmptyTables = false);
}

// ── XmlValidator (schema validation for SRI/XML documents) ─────────────────
public static class XmlValidator
{
    // Validate an XmlDocument against an XSD schema stream
    public static List<ValidationError> Validate(XmlDocument xmlDocument, Stream xsdStream);

    // Export validation errors to a JSON file
    public static void ExportErrorsToJson(List<ValidationError> errors, string outputFilePath);

    // Clean/sanitise XML for SQL Server import
    public static string CleanXmlForSqlServer(string xml);
}

// Usage example
if (!DataValidation.IsValidEmail(request.Email))
    return DomainError.Validation("INVALID_EMAIL", "Invalid email format").ToResult<User>();

if (!DataValidation.IsValidUrl(request.WebsiteUrl))
    return DomainError.Validation("INVALID_URL", "Invalid website URL").ToResult<User>();
```

### 📡 **Event Bus Abstractions (NEW)**

**High-performance event-driven architecture** for Clean Architecture + DDD + CQRS patterns. Complete pub/sub abstractions for scalable, async event processing.

#### **🎯 Core Interfaces**

Located in `Acontplus.Core.Abstractions.Messaging`:

```csharp
// Publish events to subscribers
public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default)
        where T : class;
}

// Subscribe to events asynchronously
public interface IEventSubscriber
{
    IAsyncEnumerable<T> SubscribeAsync<T>(CancellationToken cancellationToken = default)
        where T : class;
}

// Combined pub/sub interface
public interface IEventBus : IEventPublisher, IEventSubscriber
{
}
```

#### **✨ Key Features**

- **Clean Architecture**: Domain abstractions separate from infrastructure
- **CQRS Ready**: Perfect for command/query separation with events
- **DDD Alignment**: Publish domain events for cross-aggregate communication
- **Async Streaming**: `IAsyncEnumerable<T>` for efficient event consumption
- **Type-Safe**: Generic event types with compile-time safety
- **Scalable**: Designed for horizontal and vertical scaling

#### **🚀 Quick Example**

```csharp
// Define an event (record recommended for immutability)
public record OrderCreatedEvent(Guid OrderId, string CustomerName, decimal Total);

// Publish event (in command handler)
public class OrderService
{
    private readonly IEventPublisher _eventPublisher;

    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        // Create order...
        await _eventPublisher.PublishAsync(new OrderCreatedEvent(
            orderId,
            command.CustomerName,
            total));
    }
}

// Subscribe to events (background service)
public class OrderNotificationHandler : BackgroundService
{
    private readonly IEventSubscriber _eventSubscriber;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _eventSubscriber
            .SubscribeAsync<OrderCreatedEvent>(stoppingToken))
        {
            // Process event (send email, update analytics, etc.)
            await SendOrderConfirmationAsync(evt);
        }
    }
}
```

#### **📦 Implementation**

The implementation is provided in **`Acontplus.Infrastructure`**:

- `InMemoryEventBus` - High-performance channel-based implementation
- Supports ~1M events/sec with multi-producer/multi-consumer channels
- Thread-safe concurrent operations
- Perfect for monoliths and single-instance apps

For distributed scenarios, drop-in replacements available:

- Azure Service Bus, RabbitMQ, Kafka (future)
- Same interfaces, no code changes needed!

#### **📚 Complete Documentation**

- See `Acontplus.Infrastructure` README for full event bus setup and CQRS examples
- Demo.Api samples are available under `apps/src/Demo.Api/Endpoints/`

### 🔥 **Advanced JSON Extensions**

Business-optimized JSON handling with multiple serialization options:

```csharp
// JSON Serialization Options (static readonly — reuse for performance)
public static class JsonExtensions
{
    public static readonly JsonSerializerOptions DefaultOptions; // camelCase, null-ignoring, comment-tolerant
    public static readonly JsonSerializerOptions PrettyOptions;  // same + WriteIndented = true
    public static readonly JsonSerializerOptions StrictOptions;  // case-sensitive, no trailing commas
}

// Serialization Methods
var json = myObject.SerializeOptimized();             // Uses DefaultOptions
var prettyJson = myObject.SerializeOptimized(pretty: true); // Uses PrettyOptions

// Deserialization with Error Handling
try
{
    var obj = jsonString.DeserializeOptimized<MyType>();
}
catch (JsonException ex)
{
    var error = DomainError.Validation("JSON_DESERIALIZE_ERROR", ex.Message);
    return Result<MyType>.Failure(error);
}

// Safe Deserialization with Fallback
var obj = jsonString.DeserializeSafe<MyType>(fallback: new MyType());

// Deep Cloning via JSON
var clone = myObject.CloneDeep(); // Creates deep copy via JSON round-trip
```

> **Performance note:** `DefaultOptions`, `PrettyOptions`, and `StrictOptions` are `static readonly` fields.
> `System.Text.Json` caches internal reflection/source-gen metadata per options instance — passing a new
> instance on every call discards that cache. Always use these shared instances.

### 🧩 **Extension Methods**

#### **Result Extensions**

```csharp
// Success-with-warnings helpers (extension on any value)
value.ToSuccessWithWarningsResult(warnings);                    // DomainWarnings
value.ToSuccessWithWarningsResult(warning1, warning2, ...);     // params DomainError[]

// Error-to-Result shorthand (extension on DomainError)
DomainError.NotFound("X", "Not found").ToResult<User>();        // → Result<User>

// Failure from a list (extension on List<DomainError>)
errors.ToFailureResult<Order>();                                // → Result<Order, DomainErrors>
```

#### **Nullable Extensions**

```csharp
value.IsNull<T>()
value.IsNotNull<T>()
value.OrDefault<T>(fallback)
value.OrThrow<T>(exception)
value.OrThrow<T>(message)
```

#### **Enum Extensions**

```csharp
myEnum.DisplayName() // Returns [Description] attribute text, or ToString() as fallback
```

### 🌐 **HTTP Client Abstraction**

Decouple outbound HTTP calls from `HttpClient` — enables retry policies, circuit-breakers, and unit-testing without touching call sites:

```csharp
// Located in Acontplus.Core.Abstractions.Infrastructure.Http
public interface IHttpClientService
{
    Task<TResponse?> GetAsync<TResponse>(string url,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    Task PostAsync<TRequest>(string url, TRequest body,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest body,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(string url,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default);
}
// Implementation provided in Acontplus.Infrastructure
```

### �️ **Cache Abstraction**

```csharp
// Located in Acontplus.Core.Abstractions.Infrastructure.Caching
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    bool TryGetValue<T>(string key, out T? value);
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);

    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
    Task<CacheStatistics?> GetStatisticsAsync(CancellationToken ct = default);
}
// Implementation provided in Acontplus.Infrastructure
```

### 📚 Constants

```csharp
// API response metadata keys
ApiMetadataKeys.Page, .PageSize, .TotalItems, .TotalPages, .HasNextPage, .HasPreviousPage, .CorrelationId

// Pagination metadata keys
PaginationMetadataKeys.Skip, .Take, .TotalCount, .PageIndex, .PageSize

// Debug / health-check keys
DebugMetadataKeys.*, HealthCheckMetadataKeys.*
```

## 📖 Documentation

For detailed implementation guides and best practices, see:

- Each library's own README for usage examples
- `Acontplus.Persistence.Common` for repository and filtering patterns
- `Acontplus.Infrastructure` for event bus and caching implementation guides
- `apps/src/Demo.Api` for end-to-end usage examples

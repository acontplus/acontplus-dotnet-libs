# Acontplus.Core

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Core.svg)](https://www.nuget.org/packages/Acontplus.Core)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A cutting-edge .NET foundational library leveraging the latest C# language features and business patterns. Built with performance, type safety, and developer experience in mind. Focuses on pure domain logic with clean separation from API concerns.

## 🚀 What's New (Latest Version)

- **�️ Dapper Repository Interface** - New `IDapperRepository` for lightweight micro-ORM data access
  - Complete query methods: `QueryAsync<T>`, `QueryFirstOrDefaultAsync<T>`, `QuerySingleOrDefaultAsync<T>`
  - Execute methods: `ExecuteAsync`, `ExecuteScalarAsync<T>`
  - Pagination support: `GetPagedAsync<T>`, `GetPagedFromStoredProcedureAsync<T>`
  - Filtering support: `GetFilteredAsync<T>`, `GetFilteredFromStoredProcedureAsync<T>`
  - Multiple result sets: `QueryMultipleAsync<T1, T2>`, `QueryMultipleAsync<T1, T2, T3>`
  - Transaction coordination: `SetTransaction()`, `SetConnection()`, `ClearTransaction()`
  - *Implementations provided in Acontplus.Persistence.SqlServer and Acontplus.Persistence.PostgreSQL*
- **�📡 Two Event Systems** - Complete event-driven architecture support
  - **Domain Event Dispatcher** (`IDomainEventDispatcher` + `IDomainEventHandler<T>`) for DDD domain events
    - Generic entity events: `EntityCreatedEvent`, `EntityModifiedEvent`, `EntityDeletedEvent`, etc.
    - **Synchronous** execution within same transaction/Unit of Work
    - Perfect for domain invariants, audit logging, and transactional consistency
    - Use when second insert depends on first insert's ID (within same transaction)
  - **Application Event Bus** (`IEventPublisher` + `IEventSubscriber`) for cross-service communication
    - Custom application events for business workflows
    - **Asynchronous** background processing with `System.Threading.Channels`
    - Implementation in `Acontplus.Infrastructure` with ~1M events/sec throughput
    - Perfect for notifications, analytics, microservices integration
  - *See [Event Systems Comparison](../../docs/EVENT_SYSTEMS_COMPARISON.md) for choosing the right one*
  - *See [Event Bus Guide](../../docs/EVENT_BUS_GUIDE.md) for complete documentation*
- **📢 Success Message Support** - Enhanced Result type with optional success messages
  - New `SuccessMessage` property on `Result<T, TError>`
  - `Result<T, TError>.Success(value, successMessage)` overload
  - Success messages preserved through functional composition (Map, Bind, etc.)
- **🔍 Advanced Query Operations** - Enhanced IRepository with complex query capabilities
  - `GetQueryable()` for building custom queries with joins and projections
  - `ExecuteQueryAsync<T>()` for executing custom query expressions
  - `ExecuteQueryToListAsync<T>()` and `ExecutePagedQueryAsync<T>()` for complex result handling
  - *See [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) for implementation details and examples*
- **🔧 Filter Predicate Extensions** - New utilities for dynamic filtering
  - `ToPredicate<T>()` converts filter dictionaries to LINQ expressions
  - `CreatePredicate<T>()`, `CreateContainsPredicate<T>()`, `CreateRangePredicate<T>()` for specific filter types
  - Support for string, enum, and boolean comparisons
  - *See [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) for implementation details and examples*
- **📄 Request Models** - Clean API contracts with semantic naming
  - `FilterRequest` - Non-paginated filtering, sorting, and searching (for reports, exports, autocomplete)
  - `PaginationRequest` - Paginated queries extending FilterRequest (for lists, tables, infinite scroll)
  - Semantic naming without "Dto" suffix for frontend-facing API contracts
  - Internal DTOs (like `CommandOptionsDto`) retain "Dto" suffix for infrastructure concerns
- **📄 Enhanced Request Extensions** - Fluent APIs for request management
  - **FilterRequestExtensions**: `WithSearch()`, `WithSort()`, `WithFilters()`, `WithFilter()` for building filter requests
    - `GetFilterValue<T>()` - Type-safe filter value extraction with automatic conversion and default values
    - `TryGetFilterValue<T>()` - Safe filter retrieval with out parameter pattern
  - **PaginationExtensions**: `WithSearch()`, `WithSort()`, `WithFilters()` for building pagination requests
  - `Create()`, `CreateWithSearch()`, `CreateWithSort()`, `CreateWithFilters()` factory methods
  - `Validate()`, `NextPage()`, `PreviousPage()`, `ToPage()` for pagination navigation
  - *See [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) for implementation details and examples*
- **✨ Improved Separation of Concerns** - `DomainError`/`DomainErrors` no longer create `Result` instances directly
  - Use `Result<T>.Failure(error)`, `Result<T, DomainErrors>.Failure(errors)` or extension helpers
  - New helpers: `error.ToResult<T>()`, `errors.ToFailureResult<T>()`
- **⚡ Enhanced Async Performance** - ValueTask and CancellationToken support
  - `MapAsync/BindAsync/TapAsync/MatchAsync` now have `ValueTask` variants and CT overloads
- **🛡️ Safer Default Handling** - Better default(Result) protection
  - Default guard, `TryGetValue`, `TryGetError`, and `Deconstruct(out bool, out TValue?, out TError?)`
- **🎯 Success-with-Warnings Helpers** - Enhanced warning pattern support
  - `value.ToSuccessWithWarningsResult(warnings)`

## 🚀 .NET 10 Features

### 🎯 Latest C# Language Features
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

#### **✨ Current API - Create Results Properly**

```csharp
```csharp
// ✅ CURRENT: Single error using Result factory
public static Result<User> GetUser(int id) =>
    id <= 0
        ? Result<User>.Failure(DomainError.Validation("INVALID_ID", "ID must be positive"))
        : Result<User>.Success(new User { Id = id });

// ✅ CURRENT: Single error using Result factory (alternative)
public static Result<User> GetUserAlt(int id) =>
    id <= 0
        ? Result<User>.Failure(DomainError.Validation("INVALID_ID", "ID must be positive"))
        : Result<User>.Success(new User { Id = id });
```

// ✅ CURRENT: Multiple errors using Result factory
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
ErrorType.Internal        → 500 Internal Server Error
ErrorType.External        → 502 Bad Gateway
ErrorType.RateLimited     → 429 Too Many Requests
ErrorType.Timeout         → 408 Request Timeout
// ... and more
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

*For detailed examples of advanced repository queries, complex joins, and custom projections, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation.*

#### **🔧 Dynamic Filtering with Predicates**

*For comprehensive examples of dynamic filtering, predicate creation, and filter utilities, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation.*

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

*For detailed repository implementation examples and advanced queries, see the [Acontplus.Persistence.Common](../Acontplus.Persistence.Common/) documentation.*

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
// Data Validation
public static class DataValidation
{
    public static bool IsValidJson(string json);
    public static bool IsValidXml(string xml);
    public static bool IsValidEmail(string email);
    public static bool IsValidUrl(string url);
    public static bool IsValidPhoneNumber(string phoneNumber);
}

// XML Validation with Schemas
public static class XmlValidator
{
    public static IEnumerable<ValidationError> Validate(string xmlContent, string xsdSchema);
    public static bool IsValid(string xmlContent, string xsdSchema);
    public static ValidationResult ValidateWithDetails(string xmlContent, string xsdSchema);
}

// Usage Examples
var validationResult = input switch
{
    { Length: 0 } => DomainError.Validation("EMPTY_INPUT", "Input cannot be empty").ToResult<ProcessedData>(),
    { Length: > 100 } => DomainError.Validation("TOO_LONG", "Input too long").ToResult<ProcessedData>(),
    _ when !DataValidation.IsValidEmail(input) => DomainError.Validation("INVALID_EMAIL", "Invalid email format").ToResult<ProcessedData>(),
    _ => ProcessInput(input)
};
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

- **[Event Bus Guide](../../docs/EVENT_BUS_GUIDE.md)** - Complete usage guide with CQRS examples
- **[Quick Reference](../../docs/EVENT_BUS_QUICK_REFERENCE.md)** - 30-second setup guide
- **[Demo.Api Example](../../apps/src/Demo.Api/Features/Orders/)** - Full CQRS implementation

### 🔥 **Advanced JSON Extensions**

Business-optimized JSON handling with multiple serialization options:

```csharp
// JSON Serialization Options
public static class JsonExtensions
{
    public static JsonSerializerOptions DefaultOptions { get; } // Production-optimized
    public static JsonSerializerOptions PrettyOptions { get; }  // Development-friendly
    public static JsonSerializerOptions StrictOptions { get; }  // API-strict validation
}

// Serialization Methods
var json = myObject.SerializeOptimized(); // Uses DefaultOptions
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
var clone = myObject.CloneDeep(); // Creates deep copy via JSON serialization
```

### 🧩 **Powerful Extension Methods**

#### **Result Extensions**
```csharp
public static class ResultExtensions
{
    // Success with warnings helpers
    public static Result<SuccessWithWarnings<T>> ToSuccessWithWarningsResult<T>(this T value, DomainWarnings warnings);
    public static Result<SuccessWithWarnings<T>> ToSuccessWithWarningsResult<T>(this T value, params DomainError[] warnings);

    // Fluent factory methods for common error types
    public static Result<T> ValidationError<T>(string code, string message, string? target = null);
    public static Result<T> NotFoundError<T>(string code, string message, string? target = null);
    public static Result<T> ConflictError<T>(string code, string message, string? target = null);
    public static Result<T> UnauthorizedError<T>(string code, string message, string? target = null);
}
```

#### **Other Extension Methods**
```csharp
// Nullable Extensions
public static class NullableExtensions
{
    public static bool IsNull<T>(this T? value) where T : class;
    public static bool IsNotNull<T>(this T? value) where T : class;
    public static T OrDefault<T>(this T? value, T defaultValue) where T : class;
    public static T OrThrow<T>(this T? value, Exception exception) where T : class;
}

// Enum Extensions
public static class EnumExtensions
{
    public static string DisplayName(this Enum value); // Gets Description attribute or ToString()
}
```

### 📚 Constants & Helpers

#### **API Metadata Keys**
```csharp
public static class ApiMetadataKeys
{
    public const string Page = "page";
    public const string PageSize = "pageSize";
    public const string TotalItems = "totalItems";
    public const string TotalPages = "totalPages";
    public const string HasNextPage = "hasNextPage";
    public const string HasPreviousPage = "hasPreviousPage";
    public const string CorrelationId = "correlationId";
    // ... and more
}
```

#### **API Response Helpers**
```csharp
public static class ApiResponseHelpers
{
    public static ApiResponse<T> CreateSuccessResponse<T>(T data, string message);
    public static ApiResponse<T> CreateErrorResponse<T>(string message, string errorCode);
    public static ApiResponse<T> CreateValidationErrorResponse<T>(IEnumerable<ValidationError> errors);
    public static ApiResponse<T> CreateNotFoundResponse<T>(string message);
}
```

## 📖 Documentation

For detailed implementation guides and best practices, see:
- [Domain Error & Result Usage Guide](docs/DomainError-Result-Usage-Guide.md)
- [API Integration Examples](docs/api-integration-examples.md)
- [Performance Best Practices](docs/performance-guide.md)

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup
```bash
git clone https://github.com/acontplus/acontplus-dotnet-libs.git
cd acontplus-dotnet-libs
dotnet restore
dotnet build
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 Email: proyectos@acontplus.com
- 🐛 Issues: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- 📖 Documentation: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

## 👨‍💻 Author

**Ivan Paz** - [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** - Software solutions

---

**Built with ❤️ for the .NET community using the latest .NET features**

# Acontplus.Utilities

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Utilities.svg)](https://www.nuget.org/packages/Acontplus.Utilities)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A comprehensive .NET utility library providing common functionality for business applications. Includes compiled-delegate object mapping, API response extensions, pagination, encryption, and more.

> Version history: [CHANGELOG.md](../../CHANGELOG.md)

## Features

- **Compiled Object Mapper** — Expression-tree compiled delegates; zero reflection on the hot path; convention-based, profile-optional; DI-registered as `IObjectMapper` singleton
- **API Response Extensions** — `Result<T>` to `IActionResult`/`IResult` conversions with domain error handling, pagination, and warnings support
- **FilterQuery & PaginationQuery** — Minimal API query binding; type-safe `GetFilterValue<T>()` / `TryGetFilterValue<T>()`; `.ToPaginationRequest()` / `.ToFilterRequest()` extension conversions
- **Encryption** — AES data encryption/decryption utilities with BCrypt support
- **External Validations** — Third-party validation integrations and data validation helpers
- **Enum Extensions** — Enhanced enum functionality
- **Text Handlers** — Text manipulation and processing utilities
- **Pagination & Metadata** — Helpers for API metadata, pagination links, and diagnostics
- **Data Utilities** — JSON manipulation, DataTable mapping, and data converters
- **File Extensions** — File handling, MIME types, and compression utilities

> For barcode generation see [Acontplus.Barcode](https://www.nuget.org/packages/Acontplus.Barcode).

## Installation

```bash
dotnet add package Acontplus.Utilities
```

```xml
<PackageReference Include="Acontplus.Utilities" />
```

## Quick Start

### Register services

```csharp
// Program.cs / Startup.cs

// Zero profiles — convention mapping on demand
builder.Services.AddObjectMapper();

// With explicit profiles — delegates compiled at startup, fails fast on misconfiguration
builder.Services.AddObjectMapper(new OrderMappingProfile(), new UserMappingProfile());
```

### Inject and use

```csharp
public class OrderService(IObjectMapper mapper)
{
    public OrderDto ToDto(Order order) =>
        mapper.Map<Order, OrderDto>(order);

    public IEnumerable<OrderDto> ToDtos(IEnumerable<Order> orders) =>
        mapper.Map<Order, OrderDto>(orders);
}
```

## Usage Examples

### Object Mapping

#### Convention mapping — zero configuration

When source and target share property names and compatible types, no profile is needed. The mapper compiles a delegate on first use and caches it forever.

```csharp
// No profile registration required
var dto = mapper.Map<Order, OrderDto>(order);
var dtos = mapper.Map<Order, OrderDto>(orders);           // IEnumerable<T> overload
var dto = mapper.Map<Order, OrderDto>(order, existing);   // map onto existing instance
```

#### Mapping profile — explicit configuration

Create a profile when you need `Ignore`, `ForCtorParam`, or to pre-compile and validate at startup.

```csharp
public sealed class OrderMappingProfile : MappingProfile
{
    public OrderMappingProfile()
    {
        // Flat convention — maps all name-matched properties automatically
        CreateMap<Order, OrderListItemDto>();

        // Ignore a destination member
        CreateMap<Order, OrderDetailDto>()
            .Ignore(d => d.InternalNotes);

        // Constructor parameter binding for immutable records
        // record OrderSummary(int Id, string CustomerName, decimal Total, string Status)
        CreateMap<Order, OrderSummary>()
            .ForCtorParam("Status", (Order src) => src.Status.ToString());

        // Collection elements — convention handles element mapping
        CreateMap<OrderLineItem, OrderLineItemDto>();
    }
}
```

Register it at startup:

```csharp
builder.Services.AddObjectMapper(new OrderMappingProfile());
```

#### When a profile is optional vs required

| Scenario                                          | Profile needed?                  |
| ------------------------------------------------- | -------------------------------- |
| Same property names, compatible types             | No — convention handles it       |
| Immutable `record` with matching ctor param names | No — convention resolves by name |
| `Ignore` a destination property                   | Yes                              |
| `ForCtorParam` with custom source expression      | Yes                              |
| Pre-compilation and fail-fast at startup          | Yes                              |

#### Mapping in minimal API endpoints

```csharp
app.MapPost("/orders", (CreateOrderRequest req, IObjectMapper mapper, IOrderService svc) =>
{
    var command = mapper.Map<CreateOrderRequest, CreateOrderCommand>(req);
    return svc.CreateAsync(command).ToMinimalApiResultAsync();
});
```

#### Mapping in application services

```csharp
public sealed class UserService(IObjectMapper mapper, IRepository<User> repo) : IUserService
{
    public async Task<Result<UserDto>> GetByIdAsync(int id)
    {
        var user = await repo.GetByIdAsync(id);
        return user is null
            ? Result<UserDto>.Failure(DomainError.NotFound("USER_NOT_FOUND", $"User {id} not found"))
            : Result<UserDto>.Success(mapper.Map<User, UserDto>(user));
    }
}
```

### FilterQuery & PaginationQuery

`PaginationQuery` extends `FilterQuery`; both support automatic minimal API query-string binding.

```csharp
app.MapGet("/api/users", async (PaginationQuery pagination, IUserService service) =>
{
    // Type-safe filter extraction — never throws, returns default on missing/invalid
    var isActive = pagination.GetFilterValue<bool>("isActive", true);
    var role     = pagination.GetFilterValue<string>("role", "User");

    // Convert HTTP binding model → domain request
    var request = pagination.ToPaginationRequest()
        .WithFilter("IsActive", isActive)
        .WithFilter("Role", role);

    return await service.GetPaginatedUsersAsync(request).ToGetMinimalApiResultAsync();
});

// Non-paginated (FilterQuery only)
app.MapGet("/api/lookups", async (FilterQuery filter, ILookupService service, CancellationToken ct) =>
{
    var request = filter.ToFilterRequest();
    return await service.GetLookupsAsync("dbo.GetLookups", request, ct).ToGetMinimalApiResultAsync();
});
```

**Frontend query string format:**

```
GET /api/users?pageIndex=1&pageSize=20&searchTerm=john&sortBy=createdAt&sortDirection=desc
               &filters[status]=active&filters[role]=admin&filters[isActive]=true
```

### API Response Extensions

```csharp
// Minimal API
app.MapGet("/users/{id}", async (int id, IUserService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.ToGetMinimalApiResultAsync(); // 200 / 404 / 500 automatically
});

// With custom message
app.MapPost("/users", async (CreateUserDto dto, IUserService service) =>
{
    var result = await service.CreateAsync(dto);
    return result.ToMinimalApiResultAsync("User created successfully.");
});
```

### Encryption

```csharp
var svc = new SensitiveDataEncryptionService();
byte[] encrypted = await svc.EncryptToBytesAsync("passphrase", "sensitive-data");
string decrypted = await svc.DecryptFromBytesAsync("passphrase", encrypted);
```

### Data Utilities

```csharp
// DataTable ↔ JSON
string json   = DataConverters.DataTableToJson(myDataTable);
DataTable tbl = DataConverters.JsonToDataTable(jsonString);

// Map DataRow → strongly-typed model
var model = DataTableMapper.MapDataRowToModel<MyModel>(dataRow);
List<MyModel> list = DataTableMapper.MapDataTableToList<MyModel>(dataTable);
```

### JSON Utilities

```csharp
var result = JsonHelper.ValidateJson(jsonString);
string? value = JsonHelper.GetJsonProperty<string>(jsonString, "propertyName");
string merged = JsonHelper.MergeJson(json1, json2);
bool equal    = JsonHelper.AreEqual(json1, json2);
```

## Configuration

`AddObjectMapper` takes zero or more `MappingProfile` instances. With zero profiles the mapper resolves convention mappings on demand. With profiles, all registered type-pairs are compiled during `IServiceProvider` construction so any misconfiguration throws at startup, not at first use.

```csharp
// Zero profiles — lazy convention mapping
services.AddObjectMapper();

// One or more profiles — eager compilation, fail-fast validation
services.AddObjectMapper(
    new OrderMappingProfile(),
    new UserMappingProfile());
```

## API Reference

### Mapping

| Type                     | Description                                                             |
| ------------------------ | ----------------------------------------------------------------------- |
| `IObjectMapper`          | Interface — inject this singleton anywhere mapping is needed            |
| `MappingProfile`         | Abstract base — derive and call `CreateMap<S,T>()` in the constructor   |
| `MappingExpression<S,T>` | Fluent builder returned by `CreateMap` — chain `ForCtorParam`, `Ignore` |
| `MapperConfiguration`    | Holds profiles; call `Build()` to produce a compiled registry           |
| `TypePair`               | Value-type key `(SourceType, TargetType)` used in the registry          |

### Extensions

| Method                                     | Description                                           |
| ------------------------------------------ | ----------------------------------------------------- |
| `AddObjectMapper(params MappingProfile[])` | Registers `IObjectMapper` as singleton                |
| `PaginationQuery.ToPaginationRequest()`    | Converts query-binding model to domain request        |
| `FilterQuery.ToFilterRequest()`            | Converts query-binding model to domain filter request |
| `GetFilterValue<T>(key, default)`          | Type-safe filter extraction with fallback             |
| `TryGetFilterValue<T>(key, out value)`     | Non-throwing filter presence check                    |
| `result.ToMinimalApiResultAsync()`         | `Result<T>` → minimal API `IResult`                   |
| `result.ToGetMinimalApiResultAsync()`      | Same, using 200 on empty collections                  |

## Requirements

- .NET 10.0
- ASP.NET Core (via `FrameworkReference`) — `IServiceCollection`, `HttpContext`

## License

MIT © Acontplus

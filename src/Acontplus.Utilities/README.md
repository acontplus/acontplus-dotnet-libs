# Acontplus.Utilities

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Utilities.svg)](https://www.nuget.org/packages/Acontplus.Utilities)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A comprehensive .NET utility library providing common functionality for business applications. Async, extension methods, minimal API support, and more.

## 🚀 Features

- **API Response Extensions** - Comprehensive Result<T> to IActionResult/IResult conversions with domain error handling, pagination, and warnings support
- **Domain Extensions** - Consolidated domain-to-API conversion logic for clean architectural separation
- **FilterQuery Extensions** - Type-safe filter value extraction from FilterQuery with `GetFilterValue<T>()` and `TryGetFilterValue<T>()`
- **MAC Security** - HMAC-SHA256/SHA512 message authentication for API integrity and webhook verification
- **Encryption** - Data encryption/decryption utilities with BCrypt support
- **External Validations** - Third-party validation integrations and data validation helpers
- **Custom Logging** - Enhanced logging capabilities
- **Enum Extensions** - Enhanced enum functionality
- **Picture Helper** - Image processing utilities
- **Text Handlers** - Text manipulation and processing
- **Pagination & Metadata** - Helpers for API metadata, pagination, and diagnostics
- **Data Utilities** - JSON manipulation, DataTable mapping, and data converters
- **Object Mapping** - AutoMapper-like object mapping utilities
- **File Extensions** - File handling, MIME types, and compression utilities

> **Note:** For barcode generation features, see the separate [**Acontplus.Barcode**](https://www.nuget.org/packages/Acontplus.Barcode) package which provides QR codes, Code 128, EAN-13, and other barcode formats.

## 🏗️ Architecture & Domain Extensions

This package provides a clean architectural separation between domain logic (in `Acontplus.Core`) and API conversion logic (in `Acontplus.Utilities`). All domain-to-API conversions are consolidated here for maintainability.

### Consolidated Domain Extensions

The following domain extensions have been consolidated into this package for clean separation:

- **Domain Error Extensions**: Convert `DomainError`, `DomainErrors`, and `Result<T>` to `ApiResponse<T>` with proper HTTP status codes
- **Pagination Extensions**: Build pagination links for `PagedResult<T>` with navigation metadata
- **Domain Warnings Extensions**: Handle `DomainWarnings` and `SuccessWithWarnings<T>` conversions
- **Error Analysis**: Severity-based error prioritization and aggregate error messaging

### Result-to-API Conversion Pipeline

```csharp
// Domain layer (Acontplus.Core) - Pure domain logic
public Result<User, DomainError> GetUser(int id) { /* domain logic */ }

// API layer (Acontplus.Utilities) - Clean conversions
public IActionResult GetUser(int id)
{
    var result = _userService.GetUser(id);
    return result.ToActionResult(); // Automatic conversion with error handling
}

// Minimal API
app.MapGet("/users/{id}", (int id) =>
{
    var result = userService.GetUser(id);
    return result.ToMinimalApiResult(); // Clean, type-safe responses
});
```

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package Acontplus.Utilities
```

### .NET CLI
```bash
dotnet add package Acontplus.Utilities
```

### PackageReference
```xml
<ItemGroup>
  <PackageReference Include="Acontplus.Utilities" Version="1.3.7" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Consolidated API Response Extensions

The `ResultApiExtensions` class provides comprehensive conversions from domain `Result<T>` types to API responses:

```csharp
using Acontplus.Utilities.Extensions;

// Domain result to API response (automatic error handling)
public IActionResult GetUser(int id)
{
    var result = userService.GetUser(id);
    return result.ToActionResult(); // Handles success/error automatically
}

// With custom success message
var result = userService.CreateUser(user);
return result.ToActionResult("User created successfully");

// Minimal API support
app.MapGet("/users/{id}", (int id) =>
{
    var result = userService.GetUser(id);
    return result.ToMinimalApiResult();
});
```

### 2. Domain Error Handling

```csharp
using Acontplus.Utilities.Extensions;

// Automatic domain error to API response conversion
public IActionResult UpdateUser(int id, UserDto dto)
{
    var result = userService.UpdateUser(id, dto);
    return result.ToActionResult(); // DomainError automatically becomes 400/404/500 etc.
}

// Multiple errors with severity-based HTTP status
var errors = DomainErrors.Multiple(new[] {
    DomainError.Validation("EMAIL_INVALID", "Invalid email format"),
    DomainError.NotFound("USER_NOT_FOUND", "User not found")
});
return errors.ToActionResult<UserDto>();
```

### 3. Pagination with Links

```csharp
using Acontplus.Utilities.Extensions;

// Build pagination links automatically
public IActionResult GetUsers(PaginationQuery query)
{
    var result = userService.GetUsers(query);
    var links = result.BuildPaginationLinks("/api/users", query.PageSize);
    return result.ToActionResult();
}
```

### 4. Encryption Example

```csharp
var encryptionService = new SensitiveDataEncryptionService();
byte[] encrypted = await encryptionService.EncryptToBytesAsync("password", "data");
string decrypted = await encryptionService.DecryptFromBytesAsync("password", encrypted);
```

### 5. MAC (Message Authentication Code) Security

The MAC security service provides HMAC-based message authentication for API security, ensuring data integrity and authenticity.

```csharp
using Acontplus.Utilities.Security.Interfaces;
using Acontplus.Utilities.Security.Services;

// Register the service in your DI container
services.AddScoped<IMacSecurityService, MacSecurityService>();

// Use in your API
public class SecureApiController : ControllerBase
{
    private readonly IMacSecurityService _macService;
    
    public SecureApiController(IMacSecurityService macService)
    {
        _macService = macService;
    }

    [HttpPost("secure-endpoint")]
    public IActionResult ProcessSecureData([FromBody] SecureRequest request)
    {
        // Verify MAC signature from client
        var isValid = _macService.VerifyMac(
            request.Data, 
            request.MacSignature, 
            _configuration["ApiSecretKey"]);
        
        if (!isValid)
            return Unauthorized("Invalid MAC signature");
        
        // Process data...
        var responseData = ProcessData(request.Data);
        
        // Generate MAC for response
        var responseMac = _macService.GenerateMac(
            responseData, 
            _configuration["ApiSecretKey"]);
        
        return Ok(new { Data = responseData, Mac = responseMac });
    }
    
    [HttpPost("secure-json")]
    public IActionResult ProcessJsonData([FromBody] object jsonData)
    {
        // Generate MAC for JSON object
        var mac = _macService.GenerateMacForJson(jsonData, _configuration["ApiSecretKey"]);
        
        return Ok(new { Data = jsonData, Mac = mac });
    }
}
```

**MAC Service Features:**
- ✅ **HMAC-SHA256** - Fast and secure for most use cases
- ✅ **HMAC-SHA512** - Enhanced security for sensitive operations
- ✅ **JSON Support** - Automatic serialization for object signing
- ✅ **Timing-Safe Verification** - Protection against timing attacks using `CryptographicOperations.FixedTimeEquals`
- ✅ **API Security** - Verify request authenticity and prevent tampering
- ✅ **Webhook Signatures** - Validate incoming webhook payloads
- ✅ **Message Integrity** - Ensure data hasn't been modified in transit

**Use Cases:**
- API request/response signing
- Webhook payload verification
- Message queue integrity
- Token generation and validation
- Secure inter-service communication

### 6. Pagination Metadata Example

```csharp
var metadata = new Dictionary<string, object>()
    .WithPagination(page: 1, pageSize: 10, totalItems: 100);
```

### 6. FilterQuery Extensions - Type-Safe Filter Extraction

```csharp
using Acontplus.Utilities.Extensions;

app.MapGet("/api/lookups", async (FilterQuery filterQuery, ILookupService service) =>
{
    // Extract typed filter values with defaults - never throws
    var category = filterQuery.GetFilterValue<string>("category");
    var isActive = filterQuery.GetFilterValue<bool>("isActive", true);
    var minPriority = filterQuery.GetFilterValue<int>("minPriority", 1);

    // Safe retrieval with try pattern
    if (filterQuery.TryGetFilterValue<Guid>("entityId", out var entityId))
    {
        // entityId successfully converted to Guid
    }

    return await service.GetLookupsAsync(filterQuery);
});
```

**Benefits:**
- ✅ **Type-safe** - Automatic type conversion with fallback to defaults
- ✅ **Never throws** - Handles missing keys and conversion failures gracefully
- ✅ **Clean code** - No manual dictionary checks or type casting
- ✅ **Flexible** - Works with any type that supports `Convert.ChangeType()`

## 📄 Core Examples

### PaginationQuery with Minimal APIs

The `PaginationQuery` record provides automatic parameter binding in minimal APIs with support for multiple filters, sorting, and pagination.

#### Backend (Minimal API)

```csharp
// Program.cs or endpoint definition
app.MapGet("/api/users", async (PaginationQuery pagination, IUserService userService) =>
{
    var result = await userService.GetPaginatedUsersAsync(pagination);
    return Results.Ok(result);
})
.WithName("GetUsers")
.WithOpenApi();

// Service implementation
public async Task<PagedResult<UserDto>> GetPaginatedUsersAsync(PaginationQuery pagination)
{
    var spParameters = new Dictionary<string, object>
    {
        ["@PageIndex"] = pagination.PageIndex,
        ["@PageSize"] = pagination.PageSize,
        ["@SearchTerm"] = pagination.SearchTerm ?? (object)DBNull.Value,
        ["@SortBy"] = pagination.SortBy ?? "CreatedAt",
        ["@SortDirection"] = (pagination.SortDirection ?? SortDirection.Asc).ToString()
    };

    // Add filters from PaginationQuery.Filters
    if (pagination.Filters != null)
    {
        foreach (var filter in pagination.Filters)
        {
            var paramName = $"@{filter.Key}";
            spParameters[paramName] = filter.Value ?? DBNull.Value;
        }
    }

    // Execute stored procedure with all parameters
    var dataSet = await _adoRepository.GetDataSetAsync("sp_GetPaginatedUsers", spParameters);
    // Process results and return PagedResult
}
```

#### Frontend (JavaScript/TypeScript)

```typescript
// API client function
async function getUsers(filters: UserFilters = {}) {
    const params = new URLSearchParams({
        pageIndex: '1',
        pageSize: '20',
        searchTerm: filters.searchTerm || '',
        sortBy: 'createdAt',
        sortDirection: 'desc'
    });

    // Add filters
    if (filters.status) params.append('filters[status]', filters.status);
    if (filters.role) params.append('filters[role]', filters.role);
    if (filters.isActive !== undefined) params.append('filters[isActive]', filters.isActive.toString());
    if (filters.createdDate) params.append('filters[createdDate]', filters.createdDate);

    const response = await fetch(`/api/users?${params.toString()}`);
    return response.json();
}

// Usage examples
const users = await getUsers({
    searchTerm: 'john',
    status: 'active',
    role: 'admin',
    isActive: true
});

// URL generated: /api/users?pageIndex=1&pageSize=20&searchTerm=john&sortBy=createdAt&sortDirection=desc&filters[status]=active&filters[role]=admin&filters[isActive]=true
```

#### React Component Example

```tsx
import React, { useState, useEffect } from 'react';

interface UserFilters {
    searchTerm?: string;
    status?: string;
    role?: string;
    isActive?: boolean;
}

const UserList: React.FC = () => {
    const [users, setUsers] = useState([]);
    const [filters, setFilters] = useState<UserFilters>({});
    const [pagination, setPagination] = useState({ pageIndex: 1, pageSize: 20 });

    const fetchUsers = async () => {
        const params = new URLSearchParams({
            pageIndex: pagination.pageIndex.toString(),
            pageSize: pagination.pageSize.toString(),
            sortBy: 'createdAt',
            sortDirection: 'desc'
        });

        // Add search term
        if (filters.searchTerm) {
            params.append('searchTerm', filters.searchTerm);
        }

        // Add filters
        Object.entries(filters).forEach(([key, value]) => {
            if (value !== undefined && key !== 'searchTerm') {
                params.append(`filters[${key}]`, value.toString());
            }
        });

        const response = await fetch(`/api/users?${params.toString()}`);
        const data = await response.json();
        setUsers(data.items);
    };

    useEffect(() => {
        fetchUsers();
    }, [filters, pagination]);

    return (
        <div>
            {/* Filter controls */}
            <input
                type="text"
                placeholder="Search users..."
                onChange={(e) => setFilters(prev => ({ ...prev, searchTerm: e.target.value }))}
            />
            <select onChange={(e) => setFilters(prev => ({ ...prev, status: e.target.value }))}>
                <option value="">All Status</option>
                <option value="active">Active</option>
                <option value="inactive">Inactive</option>
            </select>

            {/* User list */}
            {users.map(user => (
                <div key={user.id}>{user.name}</div>
            ))}
        </div>
    );
};
```

#### Query Parameter Examples

```
# Basic pagination
GET /api/users?pageIndex=1&pageSize=20

# With search and sorting
GET /api/users?pageIndex=1&pageSize=20&searchTerm=john&sortBy=name&sortDirection=asc

# With multiple filters
GET /api/users?pageIndex=1&pageSize=20&filters[status]=active&filters[role]=admin&filters[isActive]=true

# Complex filter combinations
GET /api/users?pageIndex=1&pageSize=20&searchTerm=john&filters[status]=active&filters[role]=admin&filters[createdDate]=2024-01-01&filters[departments][]=IT&filters[departments][]=HR
```

### Result & API Response Patterns with Usuario Module

#### Controller Usage

```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> GetUsuario(int id)
{
    var result = await _usuarioService.GetByIdAsync(id);
    return result.ToGetActionResult();
}

[HttpPost]
public async Task<IActionResult> CreateUsuario([FromBody] UsuarioDto dto)
{
    var usuario = ObjectMapper.Map<UsuarioDto, Usuario>(dto);
    var result = await _usuarioService.AddAsync(usuario);
    if (result.IsSuccess && result.Value is not null)
    {
        var locationUri = $"/api/Usuario/{result.Value.Id}";
        return ApiResponse<Usuario>.Success(result.Value, new ApiResponseOptions { Message = "Usuario creado exitosamente." }).ToActionResult();
    }
    return result.ToActionResult();
}

[HttpPut("{id:int}")]
public async Task<IActionResult> UpdateUsuario(int id, [FromBody] UsuarioDto dto)
{
    var usuario = ObjectMapper.Map<UsuarioDto, Usuario>(dto);
    var result = await _usuarioService.UpdateAsync(id, usuario);
    if (result.IsSuccess)
    {
        return ApiResponse<Usuario>.Success(result.Value, new ApiResponseOptions { Message = "Usuario actualizado correctamente." }).ToActionResult();
    }
    return result.ToActionResult();
}

[HttpDelete("{id:int}")]
public async Task<IActionResult> DeleteUsuario(int id)
{
    var result = await _usuarioService.DeleteAsync(id);
    return result.ToDeleteActionResult();
}
```

#### Minimal API Usage

```csharp
app.MapGet("/usuarios/{id:int}", async (int id, IUsuarioService service) =>
{
    var result = await service.GetByIdAsync(id);
    return result.ToMinimalApiResult();
});
```

#### Service Layer Example

```csharp
public async Task<Result<Usuario, DomainErrors>> AddAsync(Usuario usuario)
{
    var errors = new List<DomainError>();
    if (string.IsNullOrWhiteSpace(usuario.Username))
        errors.Add(DomainError.Validation("USERNAME_REQUIRED", "Username is required"));
    if (string.IsNullOrWhiteSpace(usuario.Email))
        errors.Add(DomainError.Validation("EMAIL_REQUIRED", "Email is required"));

    if (errors.Count > 0)
        return DomainErrors.Multiple(errors);

    // ... check for existing user, add, etc.
}
```

## 🗃️ Data Utilities

### DataConverters
```csharp
using Acontplus.Utilities.Data;

// Convert DataTable to JSON
string json = DataConverters.DataTableToJson(myDataTable);

// Convert DataSet to JSON
string json = DataConverters.DataSetToJson(myDataSet);

// Convert JSON to DataTable
DataTable table = DataConverters.JsonToDataTable(jsonString);

// Serialize any object (with DataTable/DataSet support)
string json = DataConverters.SerializeObjectCustom(myObject);

// Serialize and sanitize complex objects
string json = DataConverters.SerializeSanitizedData(myObject);
```

### DataTableMapper
```csharp
using Acontplus.Utilities.Data;

// Map a DataRow to a strongly-typed model
var model = DataTableMapper.MapDataRowToModel<MyModel>(dataRow);

// Map a DataTable to a list of models
List<MyModel> models = DataTableMapper.MapDataTableToList<MyModel>(dataTable);
```

## 🧩 JSON Utilities

### JsonHelper
```csharp
using Acontplus.Utilities.Json;

// Validate JSON
var result = JsonHelper.ValidateJson(jsonString);
if (!result.IsValid) Console.WriteLine(result.ErrorMessage);

// Get a property value from JSON
string? value = JsonHelper.GetJsonProperty<string>(jsonString, "propertyName");

// Merge two JSON objects
string merged = JsonHelper.MergeJson(json1, json2);

// Compare two JSON strings (ignoring property order)
bool areEqual = JsonHelper.AreEqual(json1, json2);
```

### JsonManipulationExtensions
```csharp
using Acontplus.Utilities.Json;

// Validate JSON using extension
bool isValid = jsonString.IsValidJson();

// Get property value using extension
int? id = jsonString.GetJsonProperty<int>("id");

// Merge JSON using extension
string merged = json1.MergeJson(json2);

// Compare JSON using extension
bool equal = json1.JsonEquals(json2);
```

## 🔄 Object Mapping

### ObjectMapper
```csharp
using Acontplus.Utilities.Mapping;

// Map between objects (AutoMapper-like)
var target = ObjectMapper.Map<SourceType, TargetType>(sourceObject);

// Configure custom mapping
ObjectMapper.CreateMap<SourceType, TargetType>()
    .ForMember(dest => dest.SomeProperty, src => src.OtherProperty)
    .Ignore(dest => dest.IgnoredProperty);

// Map with configuration
var mapped = ObjectMapper.Map<SourceType, TargetType>(sourceObject);
```

## 🔧 Advanced Usage

### File Name Sanitization
```csharp
string safeName = FileExtensions.SanitizeFileName("my*illegal:file?.txt");
```

### Base64 Conversion
```csharp
string base64 = FileExtensions.GetBase64FromByte(myBytes);
```

### Compression Utilities
```csharp
byte[] compressed = CompressionUtils.CompressGZip(data);
byte[] decompressed = CompressionUtils.DecompressGZip(compressed);
```

## 📚 API Documentation

### Consolidated Domain Extensions
- `ResultApiExtensions` - Comprehensive Result<T> to IActionResult/IResult conversions with domain error handling
- `DomainErrorExtensions` - Convert DomainError/DomainErrors to ApiResponse with HTTP status mapping
- `PagedResultExtensions` - Build pagination links and metadata for API responses
- `DomainWarningsExtensions` - Handle DomainWarnings and SuccessWithWarnings conversions

### Core Utilities
- `SensitiveDataEncryptionService` - AES encryption/decryption helpers with BCrypt support
- `MacSecurityService` - HMAC-SHA256/SHA512 message authentication for API security and data integrity
- `FileExtensions` - File name sanitization and byte array utilities
- `CompressionUtils` - GZip/Deflate compression helpers
- `TextHandlers` - String formatting and splitting utilities
- `DirectoryHelper` / `EnvironmentHelper` - Runtime and environment utilities
- `DataConverters` - DataTable/DataSet JSON conversion
- `JsonHelper` - JSON validation and manipulation
- `ObjectMapper` - Object-to-object mapping utilities

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

**Built with ❤️ for the .NET community**

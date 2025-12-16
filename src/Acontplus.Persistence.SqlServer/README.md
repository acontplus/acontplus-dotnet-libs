# Acontplus.Persistence.SqlServer

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Persistence.SqlServer.svg)](https://www.nuget.org/packages/Acontplus.Persistence.SqlServer)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

SQL Server implementation of the Acontplus persistence layer. Provides optimized Entity Framework Core integration,
ADO.NET repositories, and SQL Server-specific features for high-performance data access.

> **Note:** This package implements the abstractions defined in [**Acontplus.Persistence.Common
**](https://www.nuget.org/packages/Acontplus.Persistence.Common). For general persistence patterns and repository
> interfaces, see the common package.

## 🚀 SQL Server-Specific Features

- **SQL Server Optimization** - OFFSET-FETCH pagination, query hints, and connection pooling
- **Advanced Error Translation** - SQL Server error code mapping to domain exceptions with retry policies
- **Transaction Management** - Distributed transactions and savepoints support
- **High-Performance ADO.NET** - Direct database access with 10,000+ records/sec bulk operations
- **SqlBulkCopy Integration** - Optimized bulk inserts with automatic column mapping
- **Streaming Queries** - Memory-efficient `IAsyncEnumerable<T>` for large datasets
- **SQL Injection Prevention** - Regex validation and keyword blacklisting for dynamic queries
- **Performance Monitoring** - Query execution statistics and performance insights

## 📦 Installation

### NuGet Package Manager

```bash
Install-Package Acontplus.Persistence.SqlServer
```

### .NET CLI

```bash
dotnet add package Acontplus.Persistence.SqlServer
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Acontplus.Persistence.SqlServer" Version="1.5.12" />
  <PackageReference Include="Acontplus.Persistence.Common" Version="1.1.13" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Configure SQL Server Context

```csharp
services.AddDbContext<BaseContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        sqlOptions.CommandTimeout(60);
    }));

// Register repositories
services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
services.AddScoped<IAdoRepository, AdoRepository>();

// Configure ADO.NET resilience (optional - has sensible defaults)
services.Configure<PersistenceResilienceOptions>(
    configuration.GetSection(PersistenceResilienceOptions.SectionName));
```

### 2. Entity Framework Repository Pattern

```csharp
public class UserService
{
    private readonly IRepository<User> _userRepository;

    public UserService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<User>> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user != null
            ? Result<User>.Success(user)
            : Result<User>.Failure(DomainError.NotFound("USER_NOT_FOUND", $"User {id} not found"));
    }
}
```

### 3. High-Performance ADO.NET Operations

#### Scalar Queries

```csharp
public class OrderService
{
    private readonly IAdoRepository _adoRepository;

    public OrderService(IAdoRepository adoRepository)
    {
        _adoRepository = adoRepository;
    }

    // Get total count
    public async Task<int> GetTotalOrdersAsync()
    {
        return await _adoRepository.CountAsync("SELECT COUNT(*) FROM dbo.Orders WHERE IsDeleted = 0");
    }

    // Check existence
    public async Task<bool> OrderExistsAsync(int orderId)
    {
        var sql = "SELECT COUNT(*) FROM dbo.Orders WHERE Id = @OrderId AND IsDeleted = 0";
        var parameters = new Dictionary<string, object> { ["@OrderId"] = orderId };
        return await _adoRepository.ExistsAsync(sql, parameters);
    }

    // Get single value
    public async Task<decimal> GetTotalRevenueAsync()
    {
        var sql = "SELECT SUM(TotalAmount) FROM dbo.Orders WHERE Status = 'Completed'";
        return await _adoRepository.ExecuteScalarAsync<decimal>(sql) ?? 0;
    }
}
```

#### Pagination with Security

```csharp
// Using PaginationDto from Acontplus.Core
public async Task<PagedResult<Order>> GetPagedOrdersAsync(PaginationDto pagination)
{
    var baseSql = @"
        SELECT Id, OrderNumber, CustomerId, TotalAmount, Status, CreatedAt
        FROM dbo.Orders
        WHERE IsDeleted = 0";

    // Automatic OFFSET-FETCH with SQL injection prevention
    return await _adoRepository.GetPagedAsync<Order>(baseSql, pagination);
}

// Complex pagination with filters
public async Task<PagedResult<Order>> GetPagedOrdersByStatusAsync(
    PaginationDto pagination,
    string status)
{
    var sql = @"
        SELECT o.Id, o.OrderNumber, c.CustomerName, o.TotalAmount, o.Status, o.CreatedAt
        FROM dbo.Orders o
        INNER JOIN dbo.Customers c ON o.CustomerId = c.Id
        WHERE o.Status = @Status AND o.IsDeleted = 0";

    var parameters = new Dictionary<string, object> { ["@Status"] = status };

    return await _adoRepository.GetPagedAsync<Order>(sql, pagination, parameters);
}

// Stored procedure pagination with OUTPUT parameter
public async Task<PagedResult<User>> GetPagedUsersFromStoredProcAsync(
    PaginationDto pagination,
    string emailDomain)
{
    var parameters = new Dictionary<string, object>
    {
        ["@EmailDomain"] = emailDomain
    };

    return await _adoRepository.GetPagedFromStoredProcedureAsync<User>(
        "dbo.GetPagedUsuarios",
        pagination,
        parameters);
}
```

#### Bulk Operations (10,000+ records/sec)

```csharp
// SqlBulkCopy with DataTable
public async Task<int> BulkInsertOrdersAsync(List<Order> orders)
{
    var dataTable = new DataTable();
    dataTable.Columns.Add("OrderNumber", typeof(string));
    dataTable.Columns.Add("CustomerId", typeof(int));
    dataTable.Columns.Add("TotalAmount", typeof(decimal));
    dataTable.Columns.Add("Status", typeof(string));
    dataTable.Columns.Add("CreatedAt", typeof(DateTime));

    foreach (var order in orders)
    {
        dataTable.Rows.Add(
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CreatedAt);
    }

    // Uses SqlBulkCopy internally
    return await _adoRepository.BulkInsertAsync(dataTable, "dbo.Orders");
}

// Bulk insert with entity collection
public async Task<int> BulkInsertProductsAsync(IEnumerable<Product> products)
{
    var columnMappings = new Dictionary<string, string>
    {
        ["ProductCode"] = "Code",
        ["ProductName"] = "Name",
        ["UnitPrice"] = "Price"
    };

    return await _adoRepository.BulkInsertAsync(
        products,
        "dbo.Products",
        columnMappings,
        batchSize: 10000);
}
```

#### Streaming Large Datasets

```csharp
// Memory-efficient CSV export with IAsyncEnumerable
public async Task ExportOrdersToCsvAsync(StreamWriter writer)
{
    var sql = "SELECT Id, OrderNumber, TotalAmount, Status FROM dbo.Orders WHERE IsDeleted = 0";

    await writer.WriteLineAsync("Id,OrderNumber,TotalAmount,Status");

    await foreach (var order in _adoRepository.QueryAsyncEnumerable<Order>(sql))
    {
        await writer.WriteLineAsync($"{order.Id},{order.OrderNumber},{order.TotalAmount},{order.Status}");
    }
}

// Process large datasets in batches
public async Task ProcessLargeOrderBatchAsync()
{
    var sql = "SELECT * FROM dbo.Orders WHERE ProcessedDate IS NULL";
    var batch = new List<Order>();
    const int batchSize = 1000;

    await foreach (var order in _adoRepository.QueryAsyncEnumerable<Order>(sql))
    {
        batch.Add(order);

        if (batch.Count >= batchSize)
        {
            await ProcessOrderBatchAsync(batch);
            batch.Clear();
        }
    }

    if (batch.Any())
        await ProcessOrderBatchAsync(batch);
}
```

#### Batch and Multi-Result Queries

```csharp
// Execute multiple commands in one transaction
public async Task<int> ExecuteBatchUpdatesAsync(List<int> orderIds)
{
    var commands = orderIds.Select(id => (
        Sql: "UPDATE dbo.Orders SET Status = @Status WHERE Id = @OrderId",
        Parameters: new Dictionary<string, object>
        {
            ["@OrderId"] = id,
            ["@Status"] = "Processed"
        }
    ));

    return await _adoRepository.ExecuteBatchNonQueryAsync(commands);
}

// Get multiple datasets in one round-trip
public async Task<List<List<dynamic>>> GetDashboardDataAsync()
{
    var sql = @"
        SELECT COUNT(*) AS TotalOrders FROM dbo.Orders;
        SELECT SUM(TotalAmount) AS TotalRevenue FROM dbo.Orders WHERE Status = 'Completed';
        SELECT TOP 5 * FROM dbo.Orders ORDER BY CreatedAt DESC;";

    return await _adoRepository.QueryMultipleAsync<dynamic>(sql);
}
```

### 4. Advanced EF Core Query Operations

```csharp
// Complex queries with SQL Server optimizations
public async Task<IReadOnlyList<OrderSummary>> GetOrderSummariesAsync(
    DateTime startDate,
    CancellationToken ct = default)
{
    var queryExpression = (IQueryable<Order> q) => q
        .Where(o => o.CreatedAt >= startDate)
        .Join(_context.Set<Customer>(),
            order => order.CustomerId,
            customer => customer.Id,
            (order, customer) => new { Order = order, Customer = customer })
        .Select(x => new OrderSummary
        {
            OrderId = x.Order.Id,
            CustomerName = $"{x.Customer.FirstName} {x.Customer.LastName}",
            TotalAmount = x.Order.TotalAmount,
            Status = x.Order.Status
        });

    return await _orderRepository.ExecuteQueryToListAsync(queryExpression, ct);
}
```

## 🔧 SQL Server Configuration

### Connection String Best Practices

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=true;TrustServerCertificate=false;"
  }
}
```

### Performance Tuning

```csharp
services.AddDbContext<BaseContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Connection resilience
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);

        // Performance settings
        sqlOptions.CommandTimeout(60);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    // Additional performance options
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.EnableSensitiveDataLogging(false);
});
```

## 📚 SQL Server API Reference

### Entity Framework Repositories

- `BaseContext` - Optimized EF Core context for SQL Server
- `IRepository<TEntity>` - Generic repository pattern with change tracking
- `BaseRepository<TEntity>` - EF Core implementation with query optimization

### ADO.NET High-Performance Repositories

- `IAdoRepository` - Interface for direct ADO.NET operations
- `AdoRepository` - SQL Server optimized implementation with:
    - **Scalar Queries**: `ExecuteScalarAsync<T>`, `ExistsAsync`, `CountAsync`, `LongCountAsync`
    - **Pagination**: `GetPagedAsync<T>` with OFFSET-FETCH optimization
    - **Bulk Operations**: `BulkInsertAsync` using SqlBulkCopy (10,000+ records/sec)
    - **Streaming**: `QueryAsyncEnumerable<T>` for memory-efficient processing
    - **Batch Operations**: `ExecuteBatchNonQueryAsync`, `QueryMultipleAsync<T>`
    - **Stored Procedures**: `GetPagedFromStoredProcedureAsync<T>` with OUTPUT parameters

### Security & Error Handling

- `SqlServerExceptionHandler` - Maps SQL Server error codes to domain exceptions
- `ValidateAndSanitizeSortColumn` - SQL injection prevention (regex `^[a-zA-Z0-9_\.]+$` + keyword blacklist)
- `AsyncRetryPolicy` - Polly integration with 3 retries and exponential backoff

### Utilities

- `DbDataReaderMapper` - Fast mapping from DbDataReader to entities/DTOs
- `CommandParameterBuilder` - Type-safe SQL parameter builder
- `PaginationMetadataKeys` - Standardized metadata constants
- `QueryOptimizer` - SQL Server query optimization utilities

## 🔐 Security Features

### SQL Injection Prevention

```csharp
// Automatic validation of sort columns
var pagination = new PaginationDto
{
    SortBy = "Username", // Validated with regex ^[a-zA-Z0-9_\.]+$
    SortDirection = SortDirection.ASC
};

// Blacklisted keywords: DROP, DELETE, EXEC, ALTER, TRUNCATE, etc.
// Throws SecurityException if invalid
```

### Metadata Exposure Control

```csharp
// PaginationMetadataOptions controls what metadata is exposed
services.Configure<PaginationMetadataOptions>(options =>
{
    options.IncludeQuerySource = false; // Hide internal query details in production
    options.IncludeDebugInfo = builder.Environment.IsDevelopment();
});
```

## ⚡ Performance Benchmarks

| Operation                  | EF Core     | ADO.NET | Performance Gain     |
|----------------------------|-------------|---------|----------------------|
| Simple Query (1,000 rows)  | 45ms        | 12ms    | **3.75x faster**     |
| Pagination (10,000 rows)   | 180ms       | 35ms    | **5.14x faster**     |
| Bulk Insert (10,000 rows)  | 8,500ms     | 850ms   | **10x faster**       |
| Bulk Insert (100,000 rows) | 95,000ms    | 7,200ms | **13.2x faster**     |
| Streaming Export (1M rows) | OutOfMemory | 4.5s    | **Memory efficient** |

### When to Use Each Approach

**Use EF Core (`IRepository<T>`) when:**

- ✅ Complex object graphs with navigation properties
- ✅ Change tracking is needed
- ✅ LINQ query composition
- ✅ Standard CRUD operations
- ✅ Developer productivity is priority

**Use ADO.NET (`IAdoRepository`) when:**

- ✅ High-volume bulk operations (10,000+ records)
- ✅ Simple DTOs or read-only queries
- ✅ Custom SQL optimization required
- ✅ Memory-efficient streaming (millions of rows)
- ✅ Maximum performance is critical
- ✅ Stored procedures with complex logic

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

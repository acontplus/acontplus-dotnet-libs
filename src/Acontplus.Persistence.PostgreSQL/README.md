# Acontplus.Persistence.PostgreSQL

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Persistence.PostgreSQL.svg)](https://www.nuget.org/packages/Acontplus.Persistence.PostgreSQL)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

PostgreSQL implementation of the Acontplus persistence layer. Provides optimized Entity Framework Core integration, ADO.NET repositories, and PostgreSQL-specific features for high-performance data access.

> **Note:** This package implements the abstractions defined in [**Acontplus.Persistence.Common**](https://www.nuget.org/packages/Acontplus.Persistence.Common). For general persistence patterns and repository interfaces, see the common package.

## 🚀 PostgreSQL-Specific Features

- **PostgreSQL Optimization** - LIMIT-OFFSET pagination, parallel queries, and connection pooling
- **Advanced Error Translation** - PostgreSQL error code mapping to domain exceptions with retry policies
- **High-Performance ADO.NET** - Direct database access with 100,000+ records/sec COPY operations
- **COPY Command Integration** - Optimized bulk inserts with NpgsqlBinaryImporter
- **Streaming Queries** - Memory-efficient `IAsyncEnumerable<T>` for large datasets
- **JSON/JSONB Support** - Native JSON operations and indexing
- **Array Types** - PostgreSQL array type handling and operations
- **Full-Text Search** - PostgreSQL full-text search integration
- **SQL Injection Prevention** - Regex validation and keyword blacklisting for dynamic queries
- **Performance Monitoring** - Query execution statistics and performance insights

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package Acontplus.Persistence.PostgreSQL
```

### .NET CLI
```bash
dotnet add package Acontplus.Persistence.PostgreSQL
```

### PackageReference
```xml
<ItemGroup>
  <PackageReference Include="Acontplus.Persistence.PostgreSQL" Version="1.0.10" />
  <PackageReference Include="Acontplus.Persistence.Common" Version="1.1.13" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Configure PostgreSQL Context
```csharp
services.AddDbContext<BaseContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
        npgsqlOptions.CommandTimeout(60);
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
        return await _adoRepository.CountAsync("SELECT COUNT(*) FROM orders WHERE is_deleted = false");
    }

    // Check existence
    public async Task<bool> OrderExistsAsync(int orderId)
    {
        var sql = "SELECT COUNT(*) FROM orders WHERE id = @OrderId AND is_deleted = false";
        var parameters = new Dictionary<string, object> { ["@OrderId"] = orderId };
        return await _adoRepository.ExistsAsync(sql, parameters);
    }

    // Get single value
    public async Task<decimal> GetTotalRevenueAsync()
    {
        var sql = "SELECT SUM(total_amount) FROM orders WHERE status = 'Completed'";
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
        SELECT id, order_number, customer_id, total_amount, status, created_at
        FROM orders
        WHERE is_deleted = false";

    // Automatic LIMIT-OFFSET with SQL injection prevention
    return await _adoRepository.GetPagedAsync<Order>(baseSql, pagination);
}

// Complex pagination with filters
public async Task<PagedResult<Order>> GetPagedOrdersByStatusAsync(
    PaginationDto pagination,
    string status)
{
    var sql = @"
        SELECT o.id, o.order_number, c.customer_name, o.total_amount, o.status, o.created_at
        FROM orders o
        INNER JOIN customers c ON o.customer_id = c.id
        WHERE o.status = @Status AND o.is_deleted = false";

    var parameters = new Dictionary<string, object> { ["@Status"] = status };

    return await _adoRepository.GetPagedAsync<Order>(sql, pagination, parameters);
}

// Stored procedure/function pagination with OUTPUT parameter
public async Task<PagedResult<User>> GetPagedUsersFromFunctionAsync(
    PaginationDto pagination,
    string emailDomain)
{
    var parameters = new Dictionary<string, object>
    {
        ["@EmailDomain"] = emailDomain
    };

    return await _adoRepository.GetPagedFromStoredProcedureAsync<User>(
        "get_paged_users",
        pagination,
        parameters);
}
```

#### Bulk Operations (100,000+ records/sec with COPY)
```csharp
// PostgreSQL COPY command with DataTable
public async Task<int> BulkInsertOrdersAsync(List<Order> orders)
{
    var dataTable = new DataTable();
    dataTable.Columns.Add("order_number", typeof(string));
    dataTable.Columns.Add("customer_id", typeof(int));
    dataTable.Columns.Add("total_amount", typeof(decimal));
    dataTable.Columns.Add("status", typeof(string));
    dataTable.Columns.Add("created_at", typeof(DateTime));

    foreach (var order in orders)
    {
        dataTable.Rows.Add(
            order.OrderNumber,
            order.CustomerId,
            order.TotalAmount,
            order.Status,
            order.CreatedAt);
    }

    // Uses NpgsqlBinaryImporter (COPY) internally
    return await _adoRepository.BulkInsertAsync(dataTable, "orders");
}

// Bulk insert with entity collection
public async Task<int> BulkInsertProductsAsync(IEnumerable<Product> products)
{
    var columnMappings = new Dictionary<string, string>
    {
        ["ProductCode"] = "product_code",
        ["ProductName"] = "product_name",
        ["UnitPrice"] = "unit_price"
    };

    return await _adoRepository.BulkInsertAsync(
        products,
        "products",
        columnMappings,
        batchSize: 50000);
}
```

#### Streaming Large Datasets
```csharp
// Memory-efficient CSV export with IAsyncEnumerable
public async Task ExportOrdersToCsvAsync(StreamWriter writer)
{
    var sql = "SELECT id, order_number, total_amount, status FROM orders WHERE is_deleted = false";

    await writer.WriteLineAsync("Id,OrderNumber,TotalAmount,Status");

    await foreach (var order in _adoRepository.QueryAsyncEnumerable<Order>(sql))
    {
        await writer.WriteLineAsync($"{order.Id},{order.OrderNumber},{order.TotalAmount},{order.Status}");
    }
}

// Process large datasets in batches
public async Task ProcessLargeOrderBatchAsync()
{
    var sql = "SELECT * FROM orders WHERE processed_date IS NULL";
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
        Sql: "UPDATE orders SET status = @Status WHERE id = @OrderId",
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
        SELECT COUNT(*) AS total_orders FROM orders;
        SELECT SUM(total_amount) AS total_revenue FROM orders WHERE status = 'Completed';
        SELECT * FROM orders ORDER BY created_at DESC LIMIT 5;";

    return await _adoRepository.QueryMultipleAsync<dynamic>(sql);
}
```

### 4. Advanced EF Core Query Operations
```csharp
// Complex queries with PostgreSQL optimizations
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

## 🔧 PostgreSQL Configuration

### Connection String Best Practices
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=MyApp;Username=myuser;Password=mypass;SSL Mode=Require;Trust Server Certificate=true;"
  }
}
```

### Performance Tuning
```csharp
services.AddDbContext<BaseContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Connection resilience
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        // Performance settings
        npgsqlOptions.CommandTimeout(60);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    // Additional performance options
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.EnableSensitiveDataLogging(false);
});
```

## 📚 PostgreSQL API Reference

### Entity Framework Repositories
- `BaseContext` - Optimized EF Core context for PostgreSQL
- `IRepository<TEntity>` - Generic repository pattern with change tracking
- `BaseRepository<TEntity>` - EF Core implementation with query optimization

### ADO.NET High-Performance Repositories
- `IAdoRepository` - Interface for direct ADO.NET operations
- `AdoRepository` - PostgreSQL-optimized implementation with:
  - **Scalar Queries**: `ExecuteScalarAsync<T>`, `ExistsAsync`, `CountAsync`, `LongCountAsync`
  - **Pagination**: `GetPagedAsync<T>` with LIMIT-OFFSET optimization
  - **Bulk Operations**: `BulkInsertAsync` using PostgreSQL COPY (100,000+ records/sec)
  - **Streaming**: `QueryAsyncEnumerable<T>` for memory-efficient processing
  - **Batch Operations**: `ExecuteBatchNonQueryAsync`, `QueryMultipleAsync<T>`
  - **Functions/Procedures**: `GetPagedFromStoredProcedureAsync<T>` with OUTPUT parameters

### Security & Error Handling
- `PostgreSqlExceptionHandler` - Maps PostgreSQL error codes to domain exceptions
- `ValidateAndSanitizeSortColumn` - SQL injection prevention (regex `^[a-zA-Z0-9_\.]+$` + keyword blacklist)
- `AsyncRetryPolicy` - Polly integration with 3 retries and exponential backoff

### PostgreSQL-Specific Features
- `DbDataReaderMapper` - Fast mapping from DbDataReader to entities/DTOs
- `NpgsqlBinaryImporter` - Ultra-fast bulk insert with COPY command
- `CommandParameterBuilder` - Type-safe parameter builder
- `PaginationMetadataKeys` - Standardized metadata constants
- `JsonOperations` - JSON/JSONB query and manipulation utilities
- `FullTextSearch` - PostgreSQL full-text search integration

## 🔐 Security Features

### SQL Injection Prevention
```csharp
// Automatic validation of sort columns
var pagination = new PaginationDto
{
    SortBy = "username", // Validated with regex ^[a-zA-Z0-9_\.]+$
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

| Operation | EF Core | ADO.NET (COPY) | Performance Gain |
|-----------|---------|----------------|------------------|
| Simple Query (1,000 rows) | 48ms | 14ms | **3.43x faster** |
| Pagination (10,000 rows) | 195ms | 42ms | **4.64x faster** |
| Bulk Insert (10,000 rows) | 9,200ms | 320ms | **28.75x faster** |
| Bulk Insert (100,000 rows) | 98,000ms | 2,800ms | **35x faster** |
| Streaming Export (1M rows) | OutOfMemory | 3.8s | **Memory efficient** |

> **Note:** PostgreSQL COPY command significantly outperforms SqlBulkCopy for bulk operations

### When to Use Each Approach

**Use EF Core (`IRepository<T>`) when:**
- ✅ Complex object graphs with navigation properties
- ✅ Change tracking is needed
- ✅ LINQ query composition
- ✅ Standard CRUD operations
- ✅ Developer productivity is priority
- ✅ JSON/JSONB operations with entity mapping

**Use ADO.NET (`IAdoRepository`) when:**
- ✅ High-volume bulk operations (50,000+ records)
- ✅ Simple DTOs or read-only queries
- ✅ Custom SQL optimization required
- ✅ Memory-efficient streaming (millions of rows)
- ✅ Maximum performance is critical
- ✅ PostgreSQL functions with complex logic
- ✅ Direct COPY command usage

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

# Acontplus.Persistence.Common

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Persistence.Common.svg)](https://www.nuget.org/packages/Acontplus.Persistence.Common)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Advanced persistence abstractions and infrastructure. Includes generic repository patterns, context factory, connection
string providers, and multi-provider support for SQL Server, PostgreSQL, and other databases with business-ready
abstractions.

## 🚀 Features

### 🏗️ Core Abstractions

- **Generic Repository Pattern** - Type-safe data access with C# features
- **Context Factory** - Flexible database context creation and management
- **Connection String Provider** - Hierarchical and environment-based connection management
- **Multi-Provider Support** - SQL Server, PostgreSQL, and extensible for other databases
- **Business Patterns** - Unit of work, specification pattern, and audit trail support

### 🔧 Contemporary Architecture

- **.NET 10+ Compatible** - Latest C# features and performance optimizations
- **Async/Await Support** - Full asynchronous operation support
- **Dependency Injection** - Seamless integration with Microsoft DI container
- **Configuration Driven** - Flexible configuration through appsettings.json
- **Error Handling** - Comprehensive error handling with domain mapping
- **Resilience Patterns** - Configurable retry policies, circuit breakers, and timeouts

## 📦 Installation

### NuGet Package Manager

```bash
Install-Package Acontplus.Persistence.Common
```

### .NET CLI

```bash
dotnet add package Acontplus.Persistence.Common
```

### PackageReference

```xml
<PackageReference Include="Acontplus.Persistence.Common" Version="1.1.0" />
```

## 🎯 Quick Start

### 1. Register Services in DI

```csharp
using Acontplus.Persistence.Common;

// In Program.cs or Startup.cs
builder.Services.AddSingleton<IConnectionStringProvider, ConfigurationConnectionStringProvider>();
builder.Services.AddSingleton<IDbContextFactory<MyDbContext>, DbContextFactory<MyDbContext>>();
```

### 2. Configure Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
    "TenantA": "Server=localhost;Database=TenantA;Trusted_Connection=true;",
    "TenantB": "Server=localhost;Database=TenantB;Trusted_Connection=true;"
  },
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetries": 3,
        "BaseDelaySeconds": 2,
        "ExponentialBackoff": true,
        "MaxDelaySeconds": 30
      },
      "CircuitBreaker": {
        "Enabled": true,
        "ExceptionsAllowedBeforeBreaking": 5,
        "DurationOfBreakSeconds": 30
      },
      "Timeout": {
        "Enabled": true,
        "DefaultCommandTimeoutSeconds": 30,
        "ComplexQueryTimeoutSeconds": 60,
        "BulkOperationTimeoutSeconds": 300
      }
    }
  }
}
```

### 3. Use in Your Application

```csharp
public class ProductService
{
    private readonly IDbContextFactory<MyDbContext> _contextFactory;
    private readonly IConnectionStringProvider _connectionProvider;

    public ProductService(
        IDbContextFactory<MyDbContext> contextFactory,
        IConnectionStringProvider connectionProvider)
    {
        _contextFactory = contextFactory;
        _connectionProvider = connectionProvider;
    }

    public async Task<IEnumerable<Product>> GetProductsAsync(string contextName = null)
    {
        var context = _contextFactory.GetContext(contextName ?? "Default");
        var repository = new BaseRepository<Product>(context);

        return await repository.FindAsync(p => p.IsActive);
    }
}
```

## 🔧 Advanced Usage

### Multi-Tenant Database Access

```csharp
public class MultiTenantService
{
    private readonly IDbContextFactory<MyDbContext> _contextFactory;

    public MultiTenantService(IDbContextFactory<MyDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Product> GetProductAsync(int productId, string contextName)
    {
        // Get context for specific context
        var context = _contextFactory.GetContext(contextName);
        var repository = new BaseRepository<Product>(context);

        return await repository.GetByIdAsync(productId);
    }

    public async Task<IEnumerable<Product>> GetProductsForAllContextsAsync()
    {
        var allProducts = new List<Product>();
        var contexts = new[] { "ContextA", "ContextB", "ContextC" };

        foreach (var ctx in contexts)
        {
            var context = _contextFactory.GetContext(ctx);
            var repository = new BaseRepository<Product>(context);
            var products = await repository.FindAsync(p => p.IsActive);
            allProducts.AddRange(products);
        }

        return allProducts;
    }
}
```

### Custom Connection String Provider

```csharp
public class CustomConnectionStringProvider : IConnectionStringProvider
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomConnectionStringProvider(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetConnectionString(string name)
    {
        // Get tenant from HTTP context
        var tenant = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;

        if (!string.IsNullOrEmpty(tenant))
        {
            return _configuration.GetConnectionString($"{name}_{tenant}");
        }

        return _configuration.GetConnectionString(name);
    }
}
```

### Repository with Pagination

```csharp
public class ProductRepository : BaseRepository<Product>
{
    public ProductRepository(DbContext context) : base(context) { }

    // Basic pagination
    public async Task<PagedResult<Product>> GetPagedProductsAsync(PaginationDto pagination)
    {
        return await GetPagedAsync(pagination);
    }

    // Pagination with filtering
    public async Task<PagedResult<Product>> GetActiveProductsAsync(PaginationDto pagination)
    {
        Expression<Func<Product, bool>> filter = p => p.IsActive;
        return await GetPagedAsync(pagination, filter);
    }

    // Pagination with filtering and sorting
    public async Task<PagedResult<Product>> GetProductsByCategoryAsync(
        PaginationDto pagination,
        int categoryId)
    {
        Expression<Func<Product, bool>> filter = p => p.CategoryId == categoryId;
        Expression<Func<Product, object>> orderBy = p => p.Name;

        return await GetPagedAsync(pagination, filter, orderBy: orderBy);
    }

    // Using filter-to-predicate conversion
    public async Task<PagedResult<Product>> GetFilteredProductsAsync(PaginationDto pagination)
    {
        // Convert PaginationDto.Filters to predicate automatically
        var predicate = pagination.Filters?.ToPredicate<Product>() ?? (p => true);
        return await GetPagedAsync(pagination, predicate);
    }
}
```

### Advanced Pagination Examples

```csharp
public class ProductService
{
    private readonly BaseRepository<Product> _productRepository;

    public ProductService(BaseRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    // 1. Basic pagination
    public async Task<PagedResult<Product>> GetProductsAsync(int page = 1, int pageSize = 20)
    {
        var pagination = PaginationExtensions.Create(page, pageSize);
        return await _productRepository.GetPagedAsync(pagination);
    }

    // 2. Pagination with search
    public async Task<PagedResult<Product>> SearchProductsAsync(string searchTerm, int page = 1)
    {
        var pagination = PaginationExtensions.CreateWithSearch(searchTerm, page);
        Expression<Func<Product, bool>> searchFilter = p =>
            p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm);

        return await _productRepository.GetPagedAsync(pagination, searchFilter);
    }

    // 3. Pagination with filters using fluent API
    public async Task<PagedResult<Product>> GetFilteredProductsAsync(
        int? categoryId = null,
        bool? isActive = null,
        decimal? minPrice = null,
        int page = 1)
    {
        var pagination = PaginationExtensions.Create(page)
            .WithSort("Name", SortDirection.Asc);

        // Add filters if provided
        if (categoryId.HasValue)
            pagination = pagination.WithFilter("CategoryId", categoryId.Value);

        if (isActive.HasValue)
            pagination = pagination.WithFilter("IsActive", isActive.Value);

        if (minPrice.HasValue)
            pagination = pagination.WithFilter("MinPrice", minPrice.Value);

        // Convert filters to predicate
        var predicate = pagination.Filters?.ToPredicate<Product>() ?? (p => true);

        return await _productRepository.GetPagedAsync(pagination, predicate);
    }

    // 4. Pagination with complex filtering
    public async Task<PagedResult<Product>> GetAdvancedFilteredProductsAsync(
        PaginationDto pagination)
    {
        Expression<Func<Product, bool>> predicate = p => true;

        // Build complex predicate from filters
        if (pagination.Filters != null)
        {
            foreach (var filter in pagination.Filters)
            {
                predicate = filter.Key switch
                {
                    "categoryId" when filter.Value is int categoryId =>
                        CombinePredicates(predicate, p => p.CategoryId == categoryId),
                    "isActive" when filter.Value is bool isActive =>
                        CombinePredicates(predicate, p => p.IsActive == isActive),
                    "minPrice" when filter.Value is decimal minPrice =>
                        CombinePredicates(predicate, p => p.Price >= minPrice),
                    "maxPrice" when filter.Value is decimal maxPrice =>
                        CombinePredicates(predicate, p => p.Price <= maxPrice),
                    "searchTerm" when filter.Value is string searchTerm =>
                        CombinePredicates(predicate, p => p.Name.Contains(searchTerm)),
                    _ => predicate
                };
            }
        }

        return await _productRepository.GetPagedAsync(pagination, predicate);
    }

    // 5. Using projection for performance
    public async Task<PagedResult<ProductSummaryDto>> GetProductSummariesAsync(
        PaginationDto pagination)
    {
        Expression<Func<Product, ProductSummaryDto>> projection = p => new ProductSummaryDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryName = p.Category.Name
        };

        return await _productRepository.GetPagedProjectionAsync(pagination, projection);
    }

    private Expression<Func<Product, bool>> CombinePredicates(
        Expression<Func<Product, bool>> existing,
        Expression<Func<Product, bool>> newPredicate)
    {
        var parameter = Expression.Parameter(typeof(Product), "p");
        var left = Expression.Invoke(existing, parameter);
        var right = Expression.Invoke(newPredicate, parameter);
        var combined = Expression.AndAlso(left, right);
        return Expression.Lambda<Func<Product, bool>>(combined, parameter);
    }
}
```

### Controller Usage Examples

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    // Basic pagination endpoint
    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _productService.GetProductsAsync(page, pageSize);
        return Ok(result);
    }

    // Advanced filtering endpoint
    [HttpGet("filtered")]
    public async Task<ActionResult<PagedResult<Product>>> GetFilteredProducts(
        [FromQuery] PaginationDto pagination,
        [FromQuery] int? categoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] decimal? minPrice = null)
    {
        var result = await _productService.GetFilteredProductsAsync(
            categoryId, isActive, minPrice, pagination.PageIndex);
        return Ok(result);
    }

    // Search endpoint
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<Product>>> SearchProducts(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest("Search term is required");

        var result = await _productService.SearchProductsAsync(searchTerm, page);
        return Ok(result);
    }
}
```

### Complex Queries with GetQueryable

```csharp
public class ProductRepository : BaseRepository<Product>
{
    public ProductRepository(DbContext context) : base(context) { }

    // Complex multi-table join with custom projection
    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrderSummariesWithCustomerInfoAsync(
        int? customerId = null, DateTime? fromDate = null)
    {
        var query = GetQueryable(tracking: false)
            .Join(_context.Set<Customer>(),
                order => order.CustomerId,
                customer => customer.Id,
                (order, customer) => new { Order = order, Customer = customer })
            .Join(_context.Set<OrderItem>(),
                combined => combined.Order.Id,
                item => item.OrderId,
                (combined, item) => new { combined.Order, combined.Customer, Item = item })
            .Where(combined =>
                (!customerId.HasValue || combined.Order.CustomerId == customerId.Value) &&
                (!fromDate.HasValue || combined.Order.OrderDate >= fromDate.Value))
            .GroupBy(combined => new
            {
                combined.Order.Id,
                combined.Order.OrderNumber,
                CustomerName = combined.Customer.FirstName + " " + combined.Customer.LastName
            })
            .Select(group => new OrderSummaryDto
            {
                OrderId = group.Key.Id,
                OrderNumber = group.Key.OrderNumber,
                CustomerName = group.Key.CustomerName,
                ItemCount = group.Count()
            });

        return await query.ToListAsync();
    }

    // Using ExecuteQueryToListAsync for complex aggregation
    public async Task<IReadOnlyList<SalesReportDto>> GetSalesReportAsync(
        DateTime fromDate, DateTime toDate)
    {
        return await ExecuteQueryToListAsync<SalesReportDto>(
            query => query
                .Where(p => p.OrderDate >= fromDate && p.OrderDate <= toDate)
                .Join(_context.Set<OrderItem>(),
                    order => order.Id,
                    item => item.OrderId,
                    (order, item) => new { Order = order, Item = item })
                .GroupBy(combined => new
                {
                    Month = combined.Order.OrderDate.Month,
                    Year = combined.Order.OrderDate.Year
                })
                .Select(group => new SalesReportDto
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    TotalSales = group.Sum(x => x.Item.Quantity * x.Item.UnitPrice)
                })
        );
    }

    // Complex paged query with multiple joins
    public async Task<PagedResult<CustomerOrderHistoryDto>> GetCustomerOrderHistoryAsync(
        PaginationDto pagination, string? searchTerm = null)
    {
        return await ExecutePagedQueryAsync<CustomerOrderHistoryDto>(
            query => query
                .Where(c => string.IsNullOrEmpty(searchTerm) ||
                           c.FirstName.Contains(searchTerm) ||
                           c.LastName.Contains(searchTerm))
                .Select(customer => new CustomerOrderHistoryDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.FirstName + " " + customer.LastName,
                    TotalOrders = customer.Orders.Count(),
                    TotalSpent = customer.Orders.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent),
            pagination
        );
    }
}
```

### Repository with Specifications

```csharp
public class ProductRepository : BaseRepository<Product>
{
    public ProductRepository(DbContext context) : base(context) { }

    public async Task<IEnumerable<Product>> GetActiveProductsByCategoryAsync(int categoryId)
    {
        var spec = new ActiveProductsByCategorySpecification(categoryId);
        return await FindWithSpecificationAsync(spec);
    }

    public async Task<PagedResult<Product>> GetPagedProductsAsync(PaginationDto pagination)
    {
        var spec = new ProductPaginationSpecification(pagination);
        return await GetPagedWithSpecificationAsync(spec);
    }
}

public class ActiveProductsByCategorySpecification : BaseSpecification<Product>
{
    public ActiveProductsByCategorySpecification(int categoryId)
        : base(p => p.IsActive && p.CategoryId == categoryId)
    {
        AddInclude(p => p.Category);
        AddOrderBy(p => p.Name);
    }
}

public class ProductPaginationSpecification : BaseSpecification<Product>
{
    public ProductPaginationSpecification(PaginationDto pagination)
        : base(BuildCriteria(pagination))
    {
        ApplyPaging(pagination);
        AddInclude(p => p.Category);
        AddOrderBy(p => p.Name);
    }

    private static Expression<Func<Product, bool>> BuildCriteria(PaginationDto pagination)
    {
        // Convert filters to criteria
        return pagination.Filters?.ToPredicate<Product>() ?? (p => true);
    }
}
```

## 📊 Configuration Options

### Connection String Provider Options

```csharp
public class ConnectionStringOptions
{
    public string DefaultConnection { get; set; } = string.Empty;
    public Dictionary<string, string> TenantConnections { get; set; } = new();
    public bool UseEnvironmentVariables { get; set; } = true;
    public string EnvironmentPrefix { get; set; } = "DB_";
}
```

### Context Factory Options

```csharp
public class ContextFactoryOptions
{
    public bool EnableRetryOnFailure { get; set; } = true;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
```

## 🔧 Pagination Extensions

### Fluent API Usage

```csharp
// Create pagination with fluent API
var pagination = PaginationExtensions.Create(page: 1, pageSize: 20)
    .WithSearch("laptop")
    .WithSort("Price", SortDirection.Desc)
    .WithFilter("CategoryId", 5)
    .WithFilter("IsActive", true);

// Navigation helpers
var nextPage = pagination.NextPage();
var previousPage = pagination.PreviousPage();
var specificPage = pagination.ToPage(3);

// Validation
var validatedPagination = pagination.Validate();
```

### Filter-to-Predicate Conversion

```csharp
// Automatic conversion from filters to LINQ expressions
var filters = new Dictionary<string, object>
{
    ["CategoryId"] = 5,
    ["IsActive"] = true,
    ["MinPrice"] = 100.00m
};

var predicate = filters.ToPredicate<Product>();
// Results in: p => p.CategoryId == 5 && p.IsActive == true && p.Price >= 100.00m

// Use in repository
var result = await repository.GetPagedAsync(pagination, predicate);
```

### Advanced Filtering

```csharp
// String contains search
var searchPredicate = FilterPredicateExtensions.CreateContainsPredicate<Product>("Name", "laptop");

// Range filtering
var rangePredicate = FilterPredicateExtensions.CreateRangePredicate<Product>("Price", 100m, 500m);

// Custom property filtering
var customPredicate = FilterPredicateExtensions.CreatePredicate<Product>("CategoryId", 5);
```

## 🎯 Choosing the Right Query Approach

### When to Use Each Method

| Scenario                          | Recommended Method               | Example                                            |
|-----------------------------------|----------------------------------|----------------------------------------------------|
| **Simple CRUD**                   | Standard methods                 | `GetByIdAsync()`, `FindAsync()`                    |
| **Basic filtering**               | Standard methods with predicates | `FindAsync(p => p.IsActive)`                       |
| **Include navigation properties** | Standard methods with includes   | `GetAllAsync(p => p.Category)`                     |
| **Pagination**                    | `GetPagedAsync()`                | `GetPagedAsync(pagination, filter)`                |
| **Custom projections**            | `GetProjectionAsync()`           | `GetProjectionAsync(p => new { p.Name, p.Price })` |
| **Complex business logic**        | Specification pattern            | `FindWithSpecificationAsync(spec)`                 |
| **Multi-table joins**             | `GetQueryable()`                 | `GetQueryable().Join(...)`                         |
| **Complex aggregations**          | `ExecuteQueryToListAsync()`      | `ExecuteQueryToListAsync(q => q.GroupBy(...))`     |
| **Custom SQL**                    | ADO.NET methods                  | `QueryAsync<Result>("SELECT ...")`                 |

### Performance Guidelines

```csharp
// ✅ Good: Use projections for large datasets
var summaries = await repository.GetProjectionAsync(p => new ProductSummaryDto
{
    Id = p.Id,
    Name = p.Name,
    Price = p.Price
});

// ✅ Good: Use GetQueryable for complex joins
var complexData = await repository.GetQueryable()
    .Join(_context.Set<Category>(), p => p.CategoryId, c => c.Id, (p, c) => new { p, c })
    .Where(combined => combined.c.IsActive)
    .Select(combined => new { combined.p.Name, combined.c.Name })
    .ToListAsync();

// ❌ Bad: Don't load entire entities for simple data
var allProducts = await repository.GetAllAsync(); // Loads everything!

// ✅ Good: Use pagination
var pagedProducts = await repository.GetPagedAsync(pagination);
```

## 🔍 Best Practices

### 1. Pagination Best Practices

```csharp
// ✅ Good: Use validation
var pagination = request.Validate();

// ✅ Good: Set reasonable page size limits
var pagination = new PaginationDto { PageSize = Math.Min(request.PageSize, 100) };

// ✅ Good: Use projections for large datasets
var summaries = await repository.GetPagedProjectionAsync(pagination, p => new ProductSummaryDto
{
    Id = p.Id,
    Name = p.Name,
    Price = p.Price
});

// ❌ Bad: Don't load entire collections
var allProducts = await repository.GetAllAsync(); // This loads everything!

// ✅ Good: Use pagination
var pagedProducts = await repository.GetPagedAsync(pagination);
```

### 2. Filter Performance

```csharp
// ✅ Good: Use indexed columns for filtering
Expression<Func<Product, bool>> filter = p => p.CategoryId == categoryId; // Indexed

// ✅ Good: Combine filters efficiently
var predicate = pagination.Filters?.ToPredicate<Product>() ?? (p => true);

// ❌ Bad: Avoid string operations on large datasets
Expression<Func<Product, bool>> badFilter = p => p.Description.Contains(searchTerm); // Slow
```

### 3. Connection String Management

```csharp
// Use hierarchical configuration
var connectionString = _connectionProvider.GetConnectionString("DefaultConnection");

// Handle missing connections gracefully
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
}
```

### 2. Context Lifecycle Management

```csharp
public class UnitOfWork : IDisposable
{
    private readonly DbContext _context;
    private readonly BaseRepository<Product> _productRepository;
    private readonly BaseRepository<Category> _categoryRepository;

    public UnitOfWork(IDbContextFactory<MyDbContext> contextFactory, string contextName = null)
    {
        _context = contextFactory.GetContext(contextName ?? "Default");
        _productRepository = new BaseRepository<Product>(_context);
        _categoryRepository = new BaseRepository<Category>(_context);
    }

    public BaseRepository<Product> Products => _productRepository;
    public BaseRepository<Category> Categories => _categoryRepository;

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

### 3. Error Handling

```csharp
public async Task<Result<Product>> GetProductSafelyAsync(int id)
{
    try
    {
        var context = _contextFactory.GetContext();
        var repository = new BaseRepository<Product>(context);
        var product = await repository.GetByIdAsync(id);

        return product is not null
            ? Result<Product>.Success(product)
            : Result<Product>.Failure(DomainError.NotFound("PRODUCT_NOT_FOUND", $"Product {id} not found"));
    }
    catch (Exception ex)
    {
        return Result<Product>.Failure(DomainError.Internal("DATABASE_ERROR", ex.Message));
    }
}
```

## 📚 API Reference

### IConnectionStringProvider

```csharp
public interface IConnectionStringProvider
{
    string GetConnectionString(string name);
}
```

### IDbContextFactory<TContext>

```csharp
public interface IDbContextFactory<TContext> where TContext : DbContext
{
    TContext GetContext(string contextName);
}
```

### BaseRepository<TEntity>

```csharp
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    public BaseRepository(DbContext context);

    // Async operations
    public Task<TEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    public Task<TEntity?> GetByIdOrDefaultAsync(int id, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    public Task<TEntity> AddAsync(TEntity entity);
    public Task UpdateAsync(TEntity entity);
    public Task DeleteAsync(TEntity entity);

    // Specification pattern
    public Task<IReadOnlyList<TEntity>> FindWithSpecificationAsync(ISpecification<TEntity> spec);
    public Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> spec);
}
```

## 🔧 Dependencies

- **.NET 10+** - Advanced .NET framework
- **Entity Framework Core** - ORM and data access
- **Microsoft.Extensions.Configuration** - Configuration management
- **Acontplus.Core** - Core abstractions and patterns

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

**Built with ❤️ for the .NET community using cutting-edge .NET features**

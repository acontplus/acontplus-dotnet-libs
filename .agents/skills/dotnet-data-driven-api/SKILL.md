---
name: dotnet-data-driven-api
description: >-
  Create data-driven ASP.NET Core APIs with Entity Framework Core, Acontplus.Persistence,
  and Acontplus.Core. Use when scaffolding new entities, configurations, repository patterns,
  or endpoints in the Demo app or consumer services.
---

# Create a Data-Driven API with Acontplus

## When to Use

Activate this skill when:

* You need to add a new entity and its repository/API endpoints to `apps/src/Demo.*`.
* You need to set up EF Core entity configurations using Acontplus persistence patterns.
* You are implementing repository contracts using `Acontplus.Persistence.Common`.

## Entity & Repository Design Pattern

### 1. Define the Entity (in `apps/src/Demo.Domain`)

```csharp
namespace Demo.Domain.Entities;

public class Product : BaseEntity<int>
{
    public required string Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### 2. Repositories in Acontplus

Acontplus provides generic repository abstractions in `Acontplus.Core.Abstractions.Persistence` / `Acontplus.Persistence.Common`:
- `IRepository<T>` with `GetByIdAsync`, `GetQueryable()`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.
- `IUnitOfWork` with `GetRepository<T>()`, `SaveChangesAsync()`, `BeginTransactionAsync()`.

For specialized domain queries, extend or implement custom methods:

```csharp
public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
}
```

### 3. Configure the Entity (in `apps/src/Demo.Infrastructure`)

```csharp
namespace Demo.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Sku).HasMaxLength(50);
        builder.Property(e => e.Price).HasPrecision(18, 2);
    }
}
```

### 4. Create and Apply Migrations

```bash
dotnet ef migrations add AddProduct --project apps/src/Demo.Infrastructure --startup-project apps/src/Demo.Api
dotnet ef database update --project apps/src/Demo.Infrastructure --startup-project apps/src/Demo.Api
```

### 5. Create the Application Service (in `apps/src/Demo.Application`)

Use the `Result<T, DomainError>` pattern from `Acontplus.Core` for all business operations.

### 6. Create API Endpoints (in `apps/src/Demo.Api`)

Use ASP.NET Core Minimal APIs with endpoint modules and `Result<T, DomainError>`.

## Best Practices

* **Use Fluent API** in `IEntityTypeConfiguration<T>` — never Data Annotations.
* **Never expose entities directly** in API responses; always use DTOs.
* **Use `AsNoTracking()`** for read-only queries.
* **Always order migrations chronologically** and commit them with the model change.
* **Use `Result<T, DomainError>`** — never throw exceptions for business validation.

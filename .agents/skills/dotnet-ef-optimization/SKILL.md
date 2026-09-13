---
name: dotnet-ef-optimization
description: >-
  Optimize Entity Framework Core queries for better performance. Use when asked
  to improve EF Core query performance, diagnose slow database queries, reduce
  N+1 query problems, or optimize LINQ-to-SQL translation. Covers eager
  loading, query splitting, compiled queries, raw SQL, pagination, projection,
  and change-tracking configuration.
---

# Optimizing EF Core Queries

## When to Use

Activate this skill when:

* You need to optimize EF Core queries in Acontplus persistence packages or applications.
* You observe N+1 query patterns or in-memory filtering with `GetAllAsync()`.
* You need to optimize LINQ queries that translate to inefficient SQL.
* You are implementing repository methods in `Acontplus.Persistence.*`.

## Key Optimization Strategies

### 1. Use Projections (`Select`) Instead of Loading Full Entities

Loading only the columns you need dramatically reduces data transfer and avoids
tracking overhead.

```csharp
// ❌ Bad: loads the entire entity graph
var enrollments = await context.Enrollments
    .Include(e => e.Student)
    .ToListAsync(cancellationToken);

// ✅ Good: project to a DTO
var enrollments = await context.Enrollments
    .Select(e => new EnrollmentSummaryDto
    {
        Id = e.Id,
        StudentName = e.Student.FullName,
        StartDate = e.StartDate
    })
    .ToListAsync(cancellationToken);
```

### 2. Avoid N+1 Queries with Eager Loading

```csharp
// ❌ N+1: one query per enrollment to load student
foreach (var enrollment in context.Enrollments.ToList())
{
    var student = enrollment.Student; // lazy load triggers a query
}

// ✅ Eager load with Include
var enrollments = await context.Enrollments
    .Include(e => e.Student)
    .ToListAsync(cancellationToken);
```

### 3. Use `AsSplitQuery()` for Complex Includes

When including multiple collection navigations, a single query can produce a
cartesian explosion. Split queries issue separate SQL statements.

```csharp
var courses = await context.Courses
    .Include(c => c.Modules)
    .Include(c => c.Enrollments)
    .AsSplitQuery()
    .ToListAsync(cancellationToken);
```

### 4. Disable Change Tracking for Read-Only Queries

```csharp
var categories = await context.Categories
    .AsNoTracking()
    .Where(c => c.IsActive)
    .ToListAsync(cancellationToken);
```

### 5. Use Compiled Queries for Hot Paths

```csharp
private static readonly Func<AppDbContext, Guid, CancellationToken, Task<Item?>> GetItemById =
    EF.CompileAsyncQuery(
        (AppDbContext ctx, Guid id, CancellationToken ct) =>
            ctx.Items
                .FirstOrDefault(s => s.Id == id));

// Usage
var item = await GetItemById(context, itemId, cancellationToken);
```

### 6. Efficient Pagination

```csharp
// ❌ Bad: Skip/Take without a deterministic order
var page = await context.Courses
    .Skip(offset)
    .Take(pageSize)
    .ToListAsync(cancellationToken);

// ✅ Good: keyset (seek) pagination
var page = await context.Courses
    .Where(c => c.Id > lastSeenId)
    .OrderBy(c => c.Id)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

### 7. Batch Operations (EF Core 7+)

```csharp
// Bulk update without loading entities
await context.Enrollments
    .Where(e => e.Status == EnrollmentStatus.Expired)
    .ExecuteUpdateAsync(s =>
        s.SetProperty(e => e.Status, EnrollmentStatus.Archived),
        cancellationToken);

// Bulk delete
await context.AuditLogs
    .Where(a => a.CreatedAt < cutoffDate)
    .ExecuteDeleteAsync(cancellationToken);
```

## Diagnostic Tips

### Enable Query Logging

```csharp
optionsBuilder
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging(); // dev only!
```

### Use `ToQueryString()` to Inspect Generated SQL

```csharp
var query = context.Enrollments.Where(e => e.IsActive);
Console.WriteLine(query.ToQueryString());
```

## Common Anti-Patterns to Avoid

| Anti-Pattern | Fix |
|---|---|
| Calling `.ToList()` before filtering | Apply `Where` before materialization |
| Using `Count()` when `Any()` suffices | Replace `Count() > 0` with `Any()` |
| Loading navigation properties in loops | Use `Include` or projections |
| Ignoring query plan warnings | Review `EXPLAIN ANALYZE` output |
| Mixing tracked and untracked queries | Be consistent within a unit of work |

## Provider-Specific Tips (Acontplus Persistence)

### PostgreSQL (`Acontplus.Persistence.PostgreSQL`)
* Use `EXPLAIN ANALYZE` via `psql` or pgAdmin to review query plans.
* Uses snake_case naming conventions — column names in raw SQL must follow database schema conventions.
* Npgsql supports `jsonb` natively — use EF Core's `ToJson()` for complex value objects.

### SQL Server (`Acontplus.Persistence.SqlServer`)
* Review execution plans using SQL Server Management Studio (SSMS).
* Ensure proper indexes exist on foreign keys and soft-delete/audit columns.

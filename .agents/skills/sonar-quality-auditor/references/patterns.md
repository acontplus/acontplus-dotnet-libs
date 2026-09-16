# Acontplus SonarQube & Roslyn Refactoring Patterns

This document details the refactoring patterns and architectural decisions established for the
Acontplus .NET Libraries to prevent and resolve common SonarQube/Roslyn violations.

---

## 1. Cognitive Complexity (csharpsquid:S3776)
**Rule:** Refactor methods exceeding a Cognitive Complexity of 15.

### Pattern: Multi-Filter Deconstruction (`ApplyFilters`)
Query filtration in application services often combines dictionary/equality filters with full-text search.
Accumulating multiple `if (filters.TryGetValue(...))` statements in one method rapidly drives complexity past 15.

**Solution:** Always split filter logic into two distinct private static methods:
```csharp
private static IQueryable<TEntity> ApplyFilters(
    IQueryable<TEntity> queryable,
    IReadOnlyDictionary<string, object>? filters,
    string? searchTerm)
{
    queryable = ApplyDictionaryFilters(queryable, filters);
    return ApplySearchFilter(queryable, searchTerm);
}

private static IQueryable<TEntity> ApplyDictionaryFilters(
    IQueryable<TEntity> queryable,
    IReadOnlyDictionary<string, object>? filters)
{
    if (filters == null) return queryable;
    // Sequential property filters here...
    return queryable;
}

private static IQueryable<TEntity> ApplySearchFilter(
    IQueryable<TEntity> queryable,
    string? searchTerm)
{
    if (string.IsNullOrEmpty(searchTerm)) return queryable;
    // Full-text string search with StringComparison.OrdinalIgnoreCase here...
    return queryable;
}
```

### Pattern: Configuration Builders & DI Registration Extensions (`AddXxx`)
Methods configuring DI, reading options, or resolving environment overrides (e.g. OpenTelemetry, Serilog, persistence) can easily accumulate nested `if` statements and boolean operators, exceeding Cognitive Complexity 15.

**Solution:** Always decompose configuration and setup methods into focused private static helpers:
1. `BuildXxxOptions(configuration)` — binds options and delegates to:
   - `ResolveEnvironmentOverrides(options, configuration)` — handles environment variable fallbacks.
   - `ResolveServiceIdentity(options, configuration)` — handles service name / version metadata.
2. `RegisterXxx(builder, options, ...)` — handles backend / exporter registrations.
This guarantees the public extension method's Cognitive Complexity <= 2 and all helpers <= 7.

---

## 2. String Duplicate Literals (csharpsquid:S1192)
**Rule:** Define a constant instead of repeating literal strings 3 or more times.

**Solution:** Declare class-level private constants for:
- Domain error codes and messages (`private const string EntityNotFoundCode = "...";`).
- Recurring entity statuses (`private const string StatusPending = "Pending";`).
- Role names (`private const string RoleAdmin = "Admin";`).

---

## 3. Asynchronous Cancellation Tokens (csharpsquid:S8949 & CA2016)
**Rule:** Forward the `CancellationToken` to async methods.

**Rules of thumb in Acontplus:**
- Always pass `cancellationToken` to EF repository methods (`GetByIdAsync`, `FindAsync`, `AddAsync`, `UpdateAsync`, `SaveChangesAsync`) and transaction commit (`transaction.CommitAsync(cancellationToken)`).
- **Architectural Boundary:** `IUnitOfWork.BeginTransactionAsync(IsolationLevel)` from `Acontplus.Core` does **not** accept a `CancellationToken`. Do not try to force a token parameter. Instead, apply the justification attribute:
```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("SonarQube", "csharpsquid:S8949",
    Justification = "IUnitOfWork.BeginTransactionAsync does not accept CancellationToken")]
```

---

## 4. Collections and LINQ Optimizations
- **CA1826 / CA1829**: For indexable collections (`IReadOnlyList<T>`, `List<T>`, `T[]`), use `.Count` instead of `.Count()` and `list[0]` instead of `list.First()`.
- **CA1860**: Use `list.Count == 0` or `!list.Any()` appropriately based on the concrete collection type.

---

## 5. Logging Performance (external_roslyn:CA1873)
**Rule:** Evaluation of arguments may be expensive if logging is disabled.

**Solution:** Guard debug and info logging with:
```csharp
if (_logger.IsEnabled(LogLevel.Information))
{
    _logger.LogInformation(...);
}
```

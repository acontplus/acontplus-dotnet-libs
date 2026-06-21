---
inclusion: always
---

# Coding Patterns & Standards

Rules that apply to all C# code written in this monorepo.

## Result Pattern (Acontplus.Core)

All service and repository methods must return `Result<T>` or `Result`. Never throw exceptions for expected business failures.

```csharp
// ✅ Correct
public async Task<Result<Invoice>> GetByIdAsync(string id, CancellationToken ct = default)
{
    var entity = await _repo.FindAsync(id, ct);
    if (entity is null)
        return Result<Invoice>.Failure("NOT_FOUND", $"Invoice {id} not found");
    return Result<Invoice>.Success(entity);
}

// ❌ Wrong — never throw for expected failures
public async Task<Invoice> GetByIdAsync(string id)
{
    var entity = await _repo.FindAsync(id) ?? throw new NotFoundException(...);
    return entity;
}
```

Result assertions in tests:

```csharp
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be("NOT_FOUND");
```

## Async/Await

- Always use `async`/`await` — never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Always propagate `CancellationToken` through the call chain
- Use `ConfigureAwait(false)` in library code (not in application/API layer)

```csharp
// ✅ Library code
var result = await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

// ✅ API/Application layer (no ConfigureAwait needed)
var result = await _service.ProcessAsync(request, ct);
```

## Dependency Injection Registration

Every library exposes an `IServiceCollection` extension method:

```csharp
namespace Acontplus.<Name>.Extensions;

public static class <Name>ServiceExtensions
{
    public static IServiceCollection Add<Name>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<<Name>Options>(configuration.GetSection("<Name>"));
        services.AddScoped<I<Name>Service, <Name>Service>();
        return services;
    }
}
```

## XML Documentation

All `public` and `protected` members must have `<summary>` tags.

```csharp
/// <summary>Retrieves an invoice by its unique identifier.</summary>
/// <param name="id">The unique identifier of the invoice.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the invoice on success,
/// or a failure result with code "NOT_FOUND" if not found.
/// </returns>
public async Task<Result<Invoice>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

Use `<inheritdoc />` on interface implementations to avoid duplication.

## Null Safety

- All projects have `<Nullable>enable</Nullable>` — treat every nullable warning as a real issue
- Use `string.Empty` instead of `""` for default string properties
- Prefer `is null` / `is not null` over `== null` / `!= null`

## Test Naming Convention

Pattern: `<MethodName>_<Condition>_<ExpectedOutcome>`

```csharp
GetById_WhenEntityExists_ReturnsSuccessResult()
GetById_WhenEntityNotFound_ReturnsFailureResult()
Save_WithNullEntity_ThrowsArgumentNullException()
```

Test class structure (xUnit + FluentAssertions + NSubstitute):

```csharp
public sealed class MyServiceTests
{
    private readonly IMyDependency _dep = Substitute.For<IMyDependency>();
    private readonly MyService _sut;

    public MyServiceTests() => _sut = new MyService(_dep);

    [Fact]
    public async Task DoWork_WhenInputIsValid_ReturnsSuccess()
    {
        // Arrange
        // Act
        var result = await _sut.DoWorkAsync("input");
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

## Clean Architecture Layer Rules

- **Domain** (`Acontplus.Core`): no infrastructure dependencies, no EF Core, no HTTP
- **Application**: depends only on Domain interfaces — never on concrete infrastructure
- **Infrastructure**: implements Domain interfaces — `Acontplus.Persistence.*`, `Acontplus.Notifications`, etc.
- **API** (`Demo.Api`): depends on Application — no direct Domain repository calls

No circular dependencies. No upward references (Infrastructure → Application is fine; Application → Infrastructure is not).

## Global Usings

Each project has a `GlobalUsings.cs`. Add only namespace imports that are used in 3+ files.

```csharp
// GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Acontplus.Core.Results;
```

---
name: generate-documentation
description: Generate or update README.md and XML doc-comments for an Acontplus library. Use when documenting a library, refreshing package docs, or adding missing XML summaries.
---

## Process

### Step 1 — Clarify (if not provided)

1. **Target library** — e.g. `Acontplus.Notifications`
2. **Scope** — README only / XML doc-comments only / both
3. **Audience** — internal, public NuGet, or both

---

### Step 2 — Generate README.md

Path: `src/Acontplus.<Name>/README.md`

Follow `readme-conventions.md` steering strictly — no version-stamped content, no change history.

````markdown
# Acontplus.<Name>

[![NuGet](https://img.shields.io/nuget/v/Acontplus.<Name>.svg)](https://www.nuget.org/packages/Acontplus.<Name>)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

<One paragraph — purpose and primary value.>

## Features

- ✅ **Feature A** — what it does
- ✅ **Feature B** — what it does

## Installation

```bash
dotnet add package Acontplus.<Name>
```
````

## Quick Start

```csharp
builder.Services.Add<Name>(builder.Configuration);
```

## Configuration

```json
{ "<Name>": { "Option": "value" } }
```

```csharp
services.Add<Name>(opts => { opts.Option = "value"; });
```

## Usage Examples

### Example: <Scenario>

```csharp
// code
```

## Requirements

- .NET 10.0+

## License

MIT

````

---

### Step 3 — Add XML Doc-Comments

Every `public` and `protected` member must have `<summary>`.

**Classes/Interfaces**:
```csharp
/// <summary>One sentence describing what this type does.</summary>
/// <remarks>Optional: threading notes, usage context.</remarks>
public class MyService : IMyService
````

**Methods**:

```csharp
/// <summary>One sentence describing what this method does.</summary>
/// <param name="id">The unique identifier.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the entity on success,
/// or a failure result with code "NOT_FOUND" if not found.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
public async Task<Result<T>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

**Properties**: `/// <summary>Gets or sets X.</summary>`

**Enums**: document the type and each member individually.

Use `<inheritdoc />` on implementations to avoid duplication.

---

### Step 4 — Quality Checklist

- [ ] README has all 3 badges (NuGet, .NET, License)
- [ ] No version-stamped headings or change history in README
- [ ] Quick Start has a minimal working code example
- [ ] Configuration shows both JSON and C# registration
- [ ] All `public`/`protected` members have `<summary>`
- [ ] `Result<T>` returns document both success and failure paths
- [ ] `CancellationToken` params documented
- [ ] `<exception>` tags where exceptions are thrown
- [ ] `<inheritdoc />` on overrides/implementations
- [ ] `<NoWarn>1591</NoWarn>` in `.csproj`

---

### Step 5 — Verify

```bash
dotnet build src/Acontplus.<Name>
```

**0 errors and 0 CS1591 warnings** required before finishing.

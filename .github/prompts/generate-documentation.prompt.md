---
agent: agent
description: Generate or update README and XML documentation for an Acontplus library
tools:
  - read_file
  - replace_string_in_file
  - create_file
  - file_search
  - semantic_search
---

# Generate Documentation for Acontplus Library

Produce or refresh the documentation for a specified library: the package `README.md` and XML `<summary>` comments on all public members.

## Required Information

Ask the user for the following before proceeding:

1. **Target library** — e.g. `Acontplus.Notifications`, `Acontplus.Billing`
2. **Documentation scope** — README only, XML doc-comments only, or both
3. **Audience** — internal consumers, public NuGet, or both
4. **New features to highlight** — list any new APIs or behaviors to call out in "What's New"

---

## Documentation Structure (no duplication)

| Location                         | Purpose                                                                                                  | Audience            |
| -------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------- |
| `src/Acontplus.<Name>/README.md` | How to install and use the package. Published to NuGet.org. "What's New" shows **current version only**. | External consumers  |
| `CHANGELOG.md` (repo root)       | Full versioned history for all packages. Never duplicated in READMEs.                                    | Team + contributors |
| `docs/wiki/Home.md`              | Navigation index — links to package READMEs. No content here.                                            | Team / repo         |
| `docs/wiki/*.md`                 | Cross-cutting guides: architecture, resilience, migration paths.                                         | Team / repo         |

---

## README.md Template

Generate `src/Acontplus.<Name>/README.md` with this structure:

````markdown
# Acontplus.<Name>

[![NuGet](https://img.shields.io/nuget/v/Acontplus.<Name>.svg)](https://www.nuget.org/packages/Acontplus.<Name>)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

<One paragraph describing the library's purpose and primary value.>

## What's New (Latest Version)

- **Feature 1** — brief description
- **Feature 2** — brief description

## Installation

```bash
dotnet add package Acontplus.<Name>
```
````

## Quick Start

```csharp
// Minimal working example
builder.Services.Add<Name>(builder.Configuration);
```

## Configuration

```json
{
  "<Name>": {
    "Option1": "value",
    "Option2": true
  }
}
```

```csharp
// All available options with defaults
services.Add<Name>(opts =>
{
    opts.Option1 = "value";
    opts.Option2 = true;
});
```

## Features

- Feature A
- Feature B
- Feature C

## Usage Examples

### Example 1: <Common Scenario>

```csharp
// code
```

### Example 2: <Another Scenario>

```csharp
// code
```

## API Reference

| Type             | Description            |
| ---------------- | ---------------------- |
| `I<Name>Service` | Main service interface |
| `<Name>Options`  | Configuration options  |

## Related Packages

- [Acontplus.Core](../Acontplus.Core/README.md) — base abstractions

```

```

---

## XML Documentation Rules

When updating XML doc-comments, follow these rules:

### Classes and interfaces

```csharp
/// <summary>
/// One sentence describing what this type does.
/// </summary>
/// <remarks>
/// Optional: additional context, usage notes, or threading considerations.
/// </remarks>
public class MyService : IMyService
```

### Methods

```csharp
/// <summary>
/// One sentence describing what this method does.
/// </summary>
/// <param name="id">The unique identifier of the entity to retrieve.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>
/// A <see cref="Result{T}"/> containing the entity on success,
/// or an error result if the entity was not found.
/// </returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
public async Task<Result<MyEntity>> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```

### Properties

```csharp
/// <summary>Gets or sets the base URL for the external API.</summary>
public string BaseUrl { get; set; } = string.Empty;
```

### Enums and enum members

```csharp
/// <summary>Defines the supported notification channels.</summary>
public enum NotificationChannel
{
    /// <summary>Email via SMTP or AWS SES.</summary>
    Email,
    /// <summary>WhatsApp via Meta Cloud API.</summary>
    WhatsApp,
}
```

---

## Quality Checklist

- [ ] README has all 3 badges (NuGet, .NET, License)
- [ ] README includes a minimal working Quick Start code example
- [ ] README Configuration section shows both JSON and C# registration styles
- [ ] All `public` and `protected` members have `<summary>` tags
- [ ] Method returns that use `Result<T>` document both success and failure in `<returns>`
- [ ] `CancellationToken` parameters noted in `<param>`
- [ ] `<exception>` tags present where exceptions are deliberately thrown
- [ ] No duplicate documentation — prefer `<inheritdoc />` on overrides/implementations
- [ ] `<NoWarn>1591</NoWarn>` is set in `.csproj` (suppress missing-XML warnings for intentionally undocumented internal members)

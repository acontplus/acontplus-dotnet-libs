---
inclusion: auto
name: workflow-generate-documentation
description: Generate or update README and XML documentation for an Acontplus library. Use when asked to document a library, write a README, add XML doc-comments, or refresh package docs.
---

# Workflow: Generate Documentation

Produce or refresh documentation for a specified library: the package `README.md` and XML `<summary>` doc-comments on all public members.

## Step 1 — Gather Information

Ask the user for the following before proceeding:

1. **Target library** — e.g. `Acontplus.Notifications`, `Acontplus.Billing`
2. **Documentation scope** — README only, XML doc-comments only, or both
3. **Audience** — internal consumers, public NuGet, or both

---

## Documentation Structure (no duplication)

| Location                         | Purpose                                                     | Audience            |
| -------------------------------- | ----------------------------------------------------------- | ------------------- |
| `src/Acontplus.<Name>/README.md` | How to install and use the package. Published to NuGet.org. | External consumers  |
| `CHANGELOG.md` (repo root)       | Full versioned history for all packages. Never in READMEs.  | Team + contributors |
| `docs/wiki/Home.md`              | Navigation index — links only, no content.                  | Team / repo         |
| `docs/wiki/*.md`                 | Cross-cutting guides: architecture, resilience, migrations. | Team / repo         |

---

## README.md Template

Follow the structure from `.kiro/steering/readme-conventions.md`. Key sections:

````markdown
# Acontplus.<Name>

[![NuGet](https://img.shields.io/nuget/v/Acontplus.<Name>.svg)](https://www.nuget.org/packages/Acontplus.<Name>)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

<One paragraph describing the library's purpose and primary value.>

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
{
  "<Name>": {
    "Option1": "value"
  }
}
```

## Features

- Feature A
- Feature B

## Usage Examples

### Example: <Common Scenario>

```csharp
// code
```

## API Reference

| Type             | Description            |
| ---------------- | ---------------------- |
| `I<Name>Service` | Main service interface |
| `<Name>Options`  | Configuration options  |

````

---

## XML Documentation Rules

### Classes and interfaces

```csharp
/// <summary>
/// One sentence describing what this type does.
/// </summary>
/// <remarks>
/// Optional: additional context, usage notes, or threading considerations.
/// </remarks>
public class MyService : IMyService
````

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

### Enums

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

Use `<inheritdoc />` on interface implementations to avoid duplication.

---

## Quality Checklist

- [ ] README has all 3 badges (NuGet, .NET, License)
- [ ] README includes a minimal working Quick Start example
- [ ] README Configuration section shows both JSON and C# registration
- [ ] All `public` and `protected` members have `<summary>` tags
- [ ] Methods returning `Result<T>` document both success and failure in `<returns>`
- [ ] `CancellationToken` parameters documented in `<param>`
- [ ] `<exception>` tags where exceptions are deliberately thrown
- [ ] `<inheritdoc />` used on overrides/implementations
- [ ] `<NoWarn>1591</NoWarn>` set in `.csproj`
- [ ] No version references in README (belongs in CHANGELOG.md)

---

## Final Verification

```bash
dotnet build src/Acontplus.<Name>
```

Confirm **0 errors and 0 CS1591 XML documentation warnings**. If any warnings remain, add missing `<summary>` tags before presenting the result.

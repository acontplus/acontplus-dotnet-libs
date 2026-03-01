# Acontplus.ApiDocumentation

[![NuGet](https://img.shields.io/nuget/v/Acontplus.ApiDocumentation.svg)](https://www.nuget.org/packages/Acontplus.ApiDocumentation)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Standardized API versioning and Swagger/OpenAPI documentation for ASP.NET Core. Works with **controller-based APIs**, **Minimal APIs**, and **mixed projects** — including correct multi-version Swagger UI dropdown support.

---

## 🚀 Features

- **Full Minimal API versioning** — `app.NewApiVersionSet()` + `WithApiVersionSet()` supported out of the box
- **Swagger UI version dropdown** — all registered versions appear correctly via `app.DescribeApiVersions()`
- **Per-endpoint version control** — assign individual endpoints to V1-only, V2-only, or all versions
- **Controller-based API versioning** — `[ApiVersion]` attribute support via `AddMvc()` integration
- **JWT Bearer Auth UI** — pre-configured Bearer token auth in Swagger UI
- **XML Comments** — auto-included from all assemblies in the output directory
- **Custom metadata** — contact, license, and description from `appsettings.json`
- **.NET 10+ ready**

---

## 📦 Installation

```bash
dotnet add package Acontplus.ApiDocumentation
```

Also enable XML documentation in your `.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

---

## ⚙️ Configuration (`appsettings.json`)

```json
"SwaggerInfo": {
  "ContactName": "Your Team",
  "ContactEmail": "support@example.com",
  "ContactUrl": "https://example.com/support",
  "LicenseName": "MIT License",
  "LicenseUrl": "https://opensource.org/licenses/MIT"
}
```

---

## 🎯 Quick Start — Controller-based API

```csharp
using Acontplus.ApiDocumentation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddApiVersioningAndDocumentation(); // registers versioning + SwaggerGen

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Call AFTER MapControllers so all endpoint metadata is registered
if (app.Environment.IsDevelopment())
    app.UseApiVersioningAndDocumentation();

app.Run();
```

Version your controllers with `[ApiVersion]`:

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public IActionResult GetV1() => Ok("V1 response");

    [HttpGet]
    [MapToApiVersion("2.0")]
    public IActionResult GetV2() => Ok("V2 response");
}
```

---

## 🎯 Quick Start — Minimal API

> **Important:** `UseApiVersioningAndDocumentation()` must be called **after** all `app.MapXxx()` calls.
> This is because the `WebApplication` overload uses `app.DescribeApiVersions()` which scans live endpoint data sources — calling it before endpoints are mapped yields an incomplete dropdown.

```csharp
using Acontplus.ApiDocumentation;
using Asp.Versioning;
using Asp.Versioning.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiVersioningAndDocumentation(); // registers versioning + SwaggerGen

var app = builder.Build();

// 1. Build a shared version set
ApiVersionSet versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// 2. Assign endpoints to specific versions
var v1 = app.MapGroup("").WithApiVersionSet(versionSet).MapToApiVersion(1, 0);
var v2 = app.MapGroup("").WithApiVersionSet(versionSet).MapToApiVersion(2, 0);
var all = app.MapGroup("")
    .WithApiVersionSet(versionSet)
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0));

v1.MapGet("/barcode", () => "Barcode V1").WithTags("Barcode");
v2.MapGet("/storage", () => "Storage V2").WithTags("Storage");
all.MapGet("/products", () => "Products V1+V2").WithTags("Products");

// 3. Call LAST — after all Map* calls
if (app.Environment.IsDevelopment())
    app.UseApiVersioningAndDocumentation(); // WebApplication overload: uses DescribeApiVersions()

app.Run();
```

---

## 🗂️ Per-Endpoint Version Control

Use three groups sharing the same `ApiVersionSet` to control exactly which Swagger definition each endpoint appears in:

| Group   | How to declare                                                 | Swagger shows in      |
| ------- | -------------------------------------------------------------- | --------------------- |
| V1 only | `.WithApiVersionSet(vs).MapToApiVersion(1, 0)`                 | V1 definition only    |
| V2 only | `.WithApiVersionSet(vs).MapToApiVersion(2, 0)`                 | V2 definition only    |
| Both    | `.WithApiVersionSet(vs).HasApiVersion(1,0).HasApiVersion(2,0)` | V1 and V2 definitions |

---

## 🔄 `UseApiVersioningAndDocumentation` overloads

The library provides two overloads:

### `WebApplication` overload (Minimal APIs & mixed projects — preferred)

```csharp
app.UseApiVersioningAndDocumentation(); // app is WebApplication
```

Uses `app.DescribeApiVersions()` internally — scans live endpoint data sources at call time. Always reflects every version set registered via `app.NewApiVersionSet()`. **Must be called after all `app.MapXxx()`.**

### `IApplicationBuilder` overload (controller-only projects)

```csharp
IApplicationBuilder appBuilder = app;
appBuilder.UseApiVersioningAndDocumentation();
```

Uses `IApiVersionDescriptionProvider` (cached singleton). Safe for controller-only APIs since controller versions are registered at DI build time, before the pipeline runs. Includes a safe V1 fallback if no versions are discovered.

---

## 🛠️ What `AddApiVersioningAndDocumentation` registers

| Registration                                | Purpose                                                                          |
| ------------------------------------------- | -------------------------------------------------------------------------------- |
| `AddApiVersioning(...)`                     | Core versioning: default V1, URL segment + header + media type readers           |
| `.AddMvc()`                                 | Controller `[ApiVersion]` attribute support                                      |
| `.AddApiExplorer(GroupNameFormat="'v'V")`   | `IApiVersionDescriptionProvider` for both controllers and Minimal APIs           |
| `ConfigureOptions<ConfigureSwaggerOptions>` | Creates one Swagger doc per discovered version using `appsettings.json` metadata |
| `AddSwaggerGen(...)`                        | Swagger generator with JWT Bearer auth and auto XML comments                     |

---

## 📄 License & Info

- **License:** [MIT](../LICENSE)
- **Authors:** Ivan Paz
- **Company:** [Acontplus](https://www.acontplus.com)
- **Repository:** [github.com/acontplus/acontplus-dotnet-libs](https://github.com/acontplus/acontplus-dotnet-libs)
- **Contact:** [proyectos@acontplus.com](mailto:proyectos@acontplus.com)

---

**Built with ❤️ for the .NET community**

---

© 2025 Acontplus All rights reserved.

---
inclusion: auto
name: workflow-new-nuget-package
description: Scaffold a new Acontplus NuGet package. Use when asked to create a new library, add a new package, or scaffold a new Acontplus module.
---

# Workflow: Scaffold New Acontplus NuGet Package

Generate a complete, production-ready NuGet library package under `src/` following all monorepo conventions.

## Step 1 — Gather Information

Ask the user for the following before generating any files:

1. **Package short name** — e.g. `Caching` (becomes `Acontplus.Caching`)
2. **One-line description** — what the package does
3. **Primary responsibility** — main feature area
4. **Dependencies** — which other `Acontplus.*` packages or NuGet packages it needs
5. **Target audience** — internal library, public NuGet, or both
6. **ASP.NET Core only?** — determines FrameworkReference vs standalone (see `nuget-package-conventions.md`)

---

## Step 2 — Create `.csproj`

Path: `src/Acontplus.<Name>/Acontplus.<Name>.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>Acontplus.<Name></PackageId>
    <Version>1.0.0</Version>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <Authors>Ivan Paz</Authors>
    <Company>Acontplus</Company>
    <Product>Acontplus .NET Libraries</Product>
    <Copyright>Copyright © 2026 Acontplus</Copyright>
    <Description><Description></Description>
    <PackageTags><Tags></PackageTags>
    <RepositoryUrl>https://github.com/acontplus/acontplus-dotnet-libs</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageProjectUrl>https://github.com/acontplus/acontplus-dotnet-libs</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>$(OutputPath)$(AssemblyName).xml</DocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true' or '$(TF_BUILD)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
  <ItemGroup>
    <None Include="Resources\Images\icon.png" Pack="true" PackagePath="\" />
    <None Include="README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
  <!-- PackageReference items here — NO Version attribute, managed by Directory.Packages.props -->
</Project>
```

---

## Step 3 — Create Folder Structure

```
src/Acontplus.<Name>/
  Abstractions/        ← interfaces / contracts
  Models/              ← DTOs, options, request/response types
  Services/            ← implementations
  Extensions/          ← IServiceCollection extension (AddXxx)
  Resources/Images/    ← icon.png placeholder
```

---

## Step 4 — Create `GlobalUsings.cs`

```csharp
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
```

For host-agnostic libraries, only include namespaces explicitly used.

---

## Step 5 — Create `Extensions/<Name>ServiceExtensions.cs`

```csharp
namespace Acontplus.<Name>.Extensions;

/// <summary>Registers Acontplus.<Name> services into the DI container.</summary>
public static class <Name>ServiceExtensions
{
    /// <summary>Adds Acontplus.<Name> services.</summary>
    public static IServiceCollection Add<Name>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<<Name>Options>(configuration.GetSection("<Name>"));
        // TODO: register services
        return services;
    }
}
```

---

## Step 6 — Create `README.md`

Follow the structure from `.kiro/steering/readme-conventions.md`:

- NuGet badge, .NET badge, License badge
- Short summary
- `## Features` — current capabilities, no version references
- `## Installation` — `dotnet add package Acontplus.<Name>`
- `## Quick Start` — minimal working code example
- `## Configuration` — JSON + C# registration styles

---

## Step 7 — Update Directory.Packages.props

Add an internal `<PackageVersion>` entry:

```xml
<PackageVersion Include="Acontplus.<Name>" Version="1.0.0" />
```

If new third-party packages are needed, resolve latest stable versions from NuGet API first:

```
https://api.nuget.org/v3-flatcontainer/<packageid>/index.json
```

---

## Step 8 — Register in Solution

Add to `acontplus-dotnet-libs.slnx` under `/src/`:

```xml
<Folder Name="/src/">
  <Project Path="src/Acontplus.<Name>/Acontplus.<Name>.csproj" />
</Folder>
```

---

## Step 9 — Update Commit Conventions

Add new scope to `.kiro/steering/commit-conventions.md` scope list:

```
| `<scope>` | Acontplus.<Name> |
```

---

## Step 10 — Verify

```bash
dotnet build src/Acontplus.<Name>
```

Confirm **0 errors** before presenting the result to the user.

---

## New Package Checklist

- [ ] `.csproj` has all required properties
- [ ] No `Version=""` on any `<PackageReference>`
- [ ] FrameworkReference vs standalone packages decision applied
- [ ] `Extensions/<Name>ServiceExtensions.cs` created
- [ ] `README.md` follows conventions from `readme-conventions.md`
- [ ] `Directory.Packages.props` updated with new `<PackageVersion>`
- [ ] Project added to `acontplus-dotnet-libs.slnx`
- [ ] Scope added to `commit-conventions.md`
- [ ] Build passes with 0 errors

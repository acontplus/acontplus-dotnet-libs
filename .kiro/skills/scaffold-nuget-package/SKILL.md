---
name: scaffold-nuget-package
description: Scaffold a new Acontplus NuGet library package following all monorepo conventions. Use when adding a new library to the repository under src/.
---

## Process

### Step 1 — Clarify (if not already provided)

1. **Package short name** — e.g. `Caching` → becomes `Acontplus.Caching`
2. **One-line description**
3. **Dependencies** — other `Acontplus.*` packages or NuGet packages needed
4. **ASP.NET Core only?** — determines FrameworkReference vs standalone packages

---

### Step 2 — Create `src/Acontplus.<Name>/Acontplus.<Name>.csproj`

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
</Project>
```

**CRITICAL**: Never add `Version=""` to `<PackageReference>` — all versions live in `Directory.Packages.props`.

FrameworkReference decision:

- ASP.NET Core only → `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- Host-agnostic → standalone NuGet packages

---

### Step 3 — Create Folder Structure

```
src/Acontplus.<Name>/
  Abstractions/
  Models/
  Services/
  Extensions/
  Resources/Images/
```

---

### Step 4 — Create `GlobalUsings.cs`

```csharp
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
```

---

### Step 5 — Create `Extensions/<Name>ServiceExtensions.cs`

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
        return services;
    }
}
```

---

### Step 6 — Create `README.md`

Follow `readme-conventions.md`. Must include: 3 badges, description paragraph, `## Features` (no version refs), `## Installation`, `## Quick Start`, `## Configuration`.

---

### Step 7 — Update `Directory.Packages.props`

Add: `<PackageVersion Include="Acontplus.<Name>" Version="1.0.0" />`

For new third-party packages, resolve latest stable version:

```
https://api.nuget.org/v3-flatcontainer/<packageid>/index.json
```

---

### Step 8 — Register in Solution and Conventions

- Add to `acontplus-dotnet-libs.slnx` under `/src/`
- Add scope entry to `.kiro/steering/commit-conventions.md`

---

### Step 9 — Verify

```bash
dotnet build src/Acontplus.<Name>
```

**0 errors** required before presenting result.

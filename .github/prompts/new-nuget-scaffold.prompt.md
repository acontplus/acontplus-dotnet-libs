---
agent: agent
description: Scaffold a new Acontplus NuGet library package following monorepo conventions
tools:
  - create_file
  - replace_string_in_file
  - read_file
  - file_search
---

# Scaffold New Acontplus NuGet Library

Generate a complete, production-ready NuGet library package under `src/` that matches all conventions of this monorepo.

## Required Information

Ask the user for the following before generating any files:

1. **Package short name** — e.g. `Caching` (will become `Acontplus.Caching`)
2. **One-line description** — what the package does
3. **Primary responsibility** — main feature area (e.g. caching, messaging, validation)
4. **Dependencies** — which other `Acontplus.*` packages or NuGet packages it needs
5. **Target audience** — internal library, public NuGet, or both

---

## Files to Generate

### 1. `src/Acontplus.<Name>/Acontplus.<Name>.csproj`

Use this exact structure (replace `<Name>`, `<Description>`, `<Tags>`, `<ReleaseNotes>`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <PackageId>Acontplus.<Name></PackageId>
    <Version>1.0.0</Version>
    <Authors>Ivan Paz</Authors>
    <Company>Acontplus</Company>
    <Product>Acontplus .NET Libraries</Product>
    <Copyright>Copyright © 2025 Acontplus</Copyright>
    <Description><Description></Description>
    <PackageTags><Tags></PackageTags>
    <RepositoryUrl>https://github.com/acontplus/acontplus-dotnet-libs</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageProjectUrl>https://github.com/acontplus/acontplus-dotnet-libs</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReleaseNotes><ReleaseNotes></PackageReleaseNotes>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>$(OutputPath)$(AssemblyName).xml</DocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
    <LangVersion>latest</LangVersion>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
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
  <!-- Add PackageReference items here using versions from Directory.Packages.props -->
</Project>
```

> **Important**: Do NOT specify `Version` inside `<PackageReference>` elements — versions are managed centrally in `Directory.Packages.props`. If the required package is not listed there, note it so the user can add it.

### 2. `src/Acontplus.<Name>/GlobalUsings.cs`

```csharp
global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;
// If library uses FrameworkReference (ASP.NET Core required):
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
// If library is host-agnostic (standalone packages), only include what is explicitly referenced:
// global using Microsoft.Extensions.DependencyInjection.Abstractions;
// global using Microsoft.Extensions.Logging.Abstractions;
```

### 3. Folder structure

Create the following empty directories with a `.gitkeep` placeholder only if no real files populate them yet:

```
src/Acontplus.<Name>/
  Abstractions/       ← interfaces / contracts
  Models/             ← DTOs, options, request/response types
  Services/           ← implementations
  Extensions/         ← IServiceCollection extensions (AddXxx)
  Resources/Images/   ← for icon.png placeholder path
```

### 4. `src/Acontplus.<Name>/Extensions/<Name>ServiceExtensions.cs`

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

### 5. `src/Acontplus.<Name>/README.md`

Generate a README following the same structure as `src/Acontplus.Core/README.md`:

- NuGet badge, .NET badge, License badge
- Short summary
- "What's New" section (first release: initial implementation)
- Installation section (`dotnet add package Acontplus.<Name>`)
- Quick Start with code example
- Configuration section
- Features list

### 6. Solution reference

Remind the user to:

1. Add the new project to `acontplus-dotnet-libs.slnx` — the file is XML-based (`.slnx` format). Add a `<Project>` entry inside the `/src/` folder node:

   ```xml
   <Folder Name="/src/">
     <!-- existing projects ... -->
     <Project Path="src/Acontplus.<Name>/Acontplus.<Name>.csproj" />
   </Folder>
   ```

2. Add any new third-party NuGet packages to `Directory.Packages.props`
3. Add an internal `<PackageVersion Include="Acontplus.<Name>" Version="1.0.0" />` entry to `Directory.Packages.props`
4. Add a corresponding entry to the commit scope table in `.github/instructions/commits.instructions.md`

---

## FrameworkReference vs Standalone Packages

**Critical decision — affects all NuGet consumers:**

When `<FrameworkReference Include="Microsoft.AspNetCore.App" />` is added to a library, the generated `.nupkg` includes a `<frameworkReference>` in the nuspec. This means **every consumer must also run on ASP.NET Core** (Web SDK or explicit FrameworkReference). This propagates to all downstream NuGet users.

Use this decision tree:

```
Is the library ONLY usable in ASP.NET Core apps (Web API, MVC)?
├── YES → Use FrameworkReference, remove redundant Microsoft.Extensions.* NuGet packages
│         Affected packages that become redundant (already in framework):
│           Microsoft.Extensions.DependencyInjection[.Abstractions]
│           Microsoft.Extensions.Configuration[.Abstractions][.Json]
│           Microsoft.Extensions.Hosting.Abstractions
│           Microsoft.Extensions.Options
│           Microsoft.Extensions.Caching.Memory
│           Microsoft.Extensions.Logging.Abstractions
│           Microsoft.AspNetCore.Authentication.JwtBearer  ← EXCEPTION: still needs PackageReference
│
└── NO (usable in console, worker, Azure Function, etc.) → Keep standalone NuGet packages
      Examples: Acontplus.Logging, Acontplus.Analytics, Acontplus.Barcode,
                Acontplus.Persistence.*, Acontplus.Core
```

**Libraries in this repo using FrameworkReference (ASP.NET Core required):**
`Utilities`, `Infrastructure`, `S3Application`, `Services`, `Reports`, `Notifications`, `Billing`

**Libraries using standalone packages (host-agnostic):**
`Core`, `Logging`, `Analytics`, `Barcode`, `Persistence.Common`, `Persistence.SqlServer`, `Persistence.PostgreSQL`

---

## Conventions Checklist

- [ ] `net10.0` target framework
- [ ] `Nullable` and `ImplicitUsings` both enabled
- [ ] `GeneratePackageOnBuild` is `false`
- [ ] `GenerateDocumentationFile` is `true`
- [ ] No hardcoded package versions in `.csproj` (central management)
- [ ] `IncludeSymbols` + `snupkg` for source debugging
- [ ] `ContinuousIntegrationBuild` conditioned on CI env vars
- [ ] `Extensions/` contains `AddXxx` registration method
- [ ] `README.md` included in `PackagePath`
- [ ] `icon.png` included in `PackagePath`
- [ ] FrameworkReference vs standalone packages decision documented above

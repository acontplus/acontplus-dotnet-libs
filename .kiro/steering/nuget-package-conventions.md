---
inclusion: always
---

# NuGet Package Conventions

Rules that apply whenever creating or modifying any `Acontplus.*` library project.

## .csproj Required Properties

Every package project must have all of these set:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>   <!-- packing is always explicit -->
  <PackageId>Acontplus.<Name></PackageId>
  <Version>X.Y.Z</Version>
  <AssemblyVersion>X.0.0.0</AssemblyVersion>               <!-- major only -->
  <Authors>Ivan Paz</Authors>
  <Company>Acontplus</Company>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true' or '$(TF_BUILD)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

## Central Package Management (CPM) — Critical Rule

**Never add `Version=""` to a `<PackageReference>` element.** All versions live in `Directory.Packages.props`.

```xml
<!-- ✅ Correct -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />

<!-- ❌ Wrong -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
```

When adding a new third-party package: add `<PackageVersion Include="..." Version="..." />` to `Directory.Packages.props` first, then reference it without a version in the `.csproj`.

When updating an internal package reference between monorepo packages, also update the corresponding `<PackageVersion>` entry in `Directory.Packages.props`.

## FrameworkReference vs Standalone Packages

**This decision affects all downstream NuGet consumers.**

```
Is the library ONLY usable in ASP.NET Core apps?
├── YES → <FrameworkReference Include="Microsoft.AspNetCore.App" />
│         Remove redundant standalone packages already in the framework:
│         Microsoft.Extensions.DependencyInjection[.Abstractions]
│         Microsoft.Extensions.Configuration[.Abstractions][.Json]
│         Microsoft.Extensions.Hosting.Abstractions
│         Microsoft.Extensions.Options
│         Microsoft.Extensions.Caching.Memory
│         Microsoft.Extensions.Logging.Abstractions
│         (Exception: Microsoft.AspNetCore.Authentication.JwtBearer still needs PackageReference)
│
└── NO  → Use standalone NuGet packages only
```

ASP.NET Core-only (FrameworkReference): `Utilities`, `Infrastructure`, `S3Application`, `Services`, `Reports`, `Notifications`, `Billing`

Host-agnostic (standalone): `Core`, `Logging`, `Analytics`, `Barcode`, `Persistence.Common`, `Persistence.SqlServer`, `Persistence.PostgreSQL`

## Version Bumping Rules (Semantic Versioning)

| Change type                      | Bump                                          |
| -------------------------------- | --------------------------------------------- |
| Breaking API change              | MAJOR (x.0.0) — also update `AssemblyVersion` |
| New feature, backward-compatible | MINOR (x.y.0)                                 |
| Bug fix, no new API              | PATCH (x.y.z)                                 |

Always bump both the `.csproj` `<Version>` **and** the `Directory.Packages.props` `<PackageVersion>` entry for the same package simultaneously.

Use the PowerShell scripts for consistency:

```powershell
.\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor
.\batch-upgrade-version.ps1
```

## Solution File (.slnx)

When adding a new project, add it to `acontplus-dotnet-libs.slnx` under the correct folder node:

```xml
<Folder Name="/src/">
  <Project Path="src/Acontplus.<Name>/Acontplus.<Name>.csproj" />
</Folder>
```

Test projects go under `/tests/`.

## New Package Checklist

- [ ] `.csproj` has all required properties above
- [ ] `net10.0` target framework
- [ ] `Nullable` and `ImplicitUsings` both enabled
- [ ] `GeneratePackageOnBuild` is `false`
- [ ] No `Version=""` on any `<PackageReference>`
- [ ] `FrameworkReference` vs standalone packages decision made
- [ ] `Extensions/<Name>ServiceExtensions.cs` with `AddXxx()` registration method
- [ ] `README.md` included as `<None Pack="true" PackagePath="\" />`
- [ ] `icon.png` included under `Resources/Images/`
- [ ] Entry added to `Directory.Packages.props`
- [ ] Project added to `acontplus-dotnet-libs.slnx`
- [ ] New scope added to `.kiro/steering/commit-conventions.md`
- [ ] `dotnet build src/Acontplus.<Name>` passes with 0 errors

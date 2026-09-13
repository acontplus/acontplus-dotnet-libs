---
name: dotnet-nuget-management
description: >-
  Manage NuGet package dependencies in .NET projects. Use when asked to add,
  update, or remove NuGet packages, resolve version conflicts, configure
  package sources, or maintain Central Package Management with
  Directory.Packages.props. Covers package operations, source configuration,
  and vulnerability scanning.
---

# NuGet Dependency Management

## When to Use

Activate this skill when:

* You need to add, update, or remove NuGet packages in Acontplus .NET Libraries.
* Package version conflicts need to be resolved.
* Central Package Management needs to be maintained.
* Libraries need to be packed with `dotnet pack` or versions updated.
* NuGet sources need to be configured.

## Package Operations

### Add a Package (Acontplus Workflow)

Acontplus .NET Libraries use Central Package Management (`Directory.Packages.props`).
To add a new package dependency:

1. **Add the version to `Directory.Packages.props`**:

```xml
<ItemGroup>
  <PackageVersion Include="NewPackage" Version="1.2.3" />
</ItemGroup>
```

2. **Reference in the project `.csproj` without version**:

```xml
<ItemGroup>
  <PackageReference Include="NewPackage" />
</ItemGroup>
```

3. **Restore**:

```bash
dotnet restore
```

> ⚠️ **Never** add `Version` attributes directly in project `.csproj` files.

### Update Packages

```bash
# List outdated packages
dotnet list package --outdated

# Update version in Directory.Packages.props, then restore
dotnet restore
```

### Remove a Package

```bash
# Remove from .csproj
dotnet remove package PackageName

# Also remove the entry from Directory.Packages.props if no other project uses it
```

## List and Audit

```bash
# List all packages
dotnet list package

# List vulnerable packages
dotnet list package --vulnerable

# List outdated packages
dotnet list package --outdated

# Include transitive dependencies
dotnet list package --vulnerable --include-transitive
```

## Source Configuration

Check `nuget.config` for configured package sources:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## Library Packaging & Versioning (Acontplus Distribution)

Acontplus distributes versioned NuGet packages (`Acontplus.Core`, `Acontplus.Infrastructure`, `Acontplus.Persistence.*`, etc.).

### Pack All Libraries
```bash
dotnet pack acontplus-dotnet-libs.slnx --configuration Release --no-build --output nupkgs
```

### Library Versioning Rules
- Each distributable library defines its `<Version>` in its `.csproj` using SemVer.
- When bumping an internal package version, update its `<PackageVersion>` in `Directory.Packages.props` for consumer projects within the monorepo.
- Use `upgrade-version.ps1` or `batch-upgrade-version.ps1` when performing coordinated multi-package version upgrades.

## Troubleshooting

### Clear Cache

```bash
dotnet nuget locals all --clear
```

### Restore with Verbose Logging

```bash
dotnet restore -v detailed
```

### Common Issues

| Issue | Fix |
|---|---|
| NU1101: Unable to find package | Check package source configuration |
| Version conflict | Update `Directory.Packages.props` to a compatible version |
| NU1605: Detected package downgrade | Align versions in `Directory.Packages.props` |
| Build fails after package update | Check for breaking API changes in release notes |

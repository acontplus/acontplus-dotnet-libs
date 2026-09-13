---
name: dotnet-upgrade-guide
description: >-
  Guide for migrating and upgrading .NET projects across framework versions.
  Use when upgrading Acontplus .NET Libraries between .NET versions, checking for
  breaking changes, updating target frameworks, or ensuring package
  compatibility. Covers TargetFramework changes, API compatibility analysis,
  and upgrade-assistant tool usage.
---

# .NET Upgrade Guide

## When to Use

Activate this skill when:

* Acontplus .NET Libraries need to upgrade to a new .NET version.
* You need to check for breaking changes between versions.
* Package compatibility needs to be verified after an upgrade.
* You need to update `TargetFramework` across library and demo projects.

## Upgrade Workflow

### 1. Check Current Version

```bash
cat global.json
dotnet --version
```

### 2. Update `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### 3. Update Target Frameworks

Update all `.csproj` files:

```xml
<!-- Before -->
<TargetFramework>net9.0</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

Find all files that need updating:

```bash
grep -r "<TargetFramework>" src/ tests/ --include="*.csproj"
```

### 4. Update NuGet Packages

```bash
# List outdated packages
dotnet list package --outdated

# Update versions in Directory.Packages.props
```

Key packages to update for each .NET version:
* `Microsoft.AspNetCore.*`
* `Microsoft.EntityFrameworkCore.*`
* `Microsoft.Extensions.*`
* `Npgsql.EntityFrameworkCore.PostgreSQL`

### 5. Check for Breaking Changes

Review the official breaking changes documentation:
* [.NET 10 breaking changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
* [EF Core breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/)
* [ASP.NET Core breaking changes](https://learn.microsoft.com/en-us/aspnet/core/migration/)

### 6. Build and Test

```bash
# Restore and build
dotnet restore acontplus-dotnet-libs.slnx
dotnet build acontplus-dotnet-libs.slnx --configuration Release

# Run all tests
dotnet test --solution acontplus-dotnet-libs.slnx --no-build
```

### 7. Pack and Check Package Compatibility

```bash
dotnet pack acontplus-dotnet-libs.slnx --configuration Release --no-build --output nupkgs
```

## Using the Upgrade Assistant

```bash
# Install the upgrade assistant
dotnet tool install -g upgrade-assistant

# Analyze the project
upgrade-assistant analyze acontplus-dotnet-libs.slnx

# Upgrade interactively
upgrade-assistant upgrade acontplus-dotnet-libs.slnx
```

## Common Upgrade Issues

| Issue | Fix |
|---|---|
| Obsolete API warnings | Check breaking changes docs for replacement APIs |
| Package incompatibility | Update to latest compatible version in Directory.Packages.props |
| EF Core migration issues | Create a new migration after upgrading EF Core |
| Runtime behavior changes | Review release notes for behavioral changes |
| Docker build failures | Update base images to match new .NET version |

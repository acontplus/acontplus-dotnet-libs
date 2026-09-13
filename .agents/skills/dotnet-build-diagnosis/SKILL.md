---
name: dotnet-build-diagnosis
description: >-
  Diagnose and fix .NET build failures. Use when `dotnet build` or `dotnet
  restore` fails with errors. Covers common MSBuild errors (CS*, MSB*, NU*),
  NuGet restore failures, SDK resolution problems, target framework
  mismatches, and project reference issues.
---

# Build Failure Diagnosis

## When to Use

Activate this skill when:

* `dotnet build` fails with compiler errors (CS*), MSBuild errors (MSB*), or
  NuGet errors (NU*).
* `dotnet restore` cannot resolve packages or dependencies.
* A project fails to load due to SDK or target framework issues.

## Diagnostic Steps

### 1. Reproduce with Verbose Output

```bash
dotnet build acontplus-dotnet-libs.slnx -v detailed 2>&1 | tee build.log
```

### 2. Identify the Error Category

| Prefix | Category | Example |
|---|---|---|
| CS | C# compiler error | CS0246: type or namespace not found |
| MSB | MSBuild error | MSB3644: reference assemblies not found |
| NU | NuGet error | NU1101: unable to find package |
| NETSDK | SDK error | NETSDK1045: current SDK does not support target |

### 3. Common Fixes

#### CS0246 / CS0234: Type or Namespace Not Found

```bash
# Check if the package is referenced
dotnet list package

# Add missing package (remember: Acontplus uses Central Package Management)
# Add to Directory.Packages.props first, then reference in .csproj without version
```

#### NU1101 / NU1102: Package Not Found

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with detailed logging
dotnet restore -v detailed
```

#### NETSDK1045: SDK Version Mismatch

Use the `dotnet-setup-sdk` skill to install the required SDK version.

#### MSB4019: Imported Project Not Found

```bash
# Usually a missing SDK or workload
dotnet workload list
dotnet workload install <workload>
```

### 4. Binary Log for Deep Analysis

```bash
dotnet build -bl
# Produces msbuild.binlog – open with MSBuild Structured Log Viewer
```

## Project Reference Issues

```bash
# List all project references
dotnet list reference

# Verify dependency graph
dotnet build --no-restore -v minimal
```

## Central Package Management Issues (Acontplus-Specific)

Acontplus .NET Libraries use `Directory.Packages.props` for centralized versioning:

* Ensure all `<PackageReference>` items omit the `Version` attribute.
* Check that the package is listed in `Directory.Packages.props`.
* Run `dotnet restore` after any changes to the central file.
* Never add version attributes directly in project `.csproj` files.

## Code Quality Analysis

### Enable in Project File

```xml
<PropertyGroup>
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

### Treating Warnings as Errors

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsNotAsErrors>CS1591</WarningsNotAsErrors>
</PropertyGroup>
```

### Suppressing Warnings

```csharp
[SuppressMessage("Design", "CA1062", Justification = "Parameter validated by framework")]
public void MyMethod(string input) { }
```

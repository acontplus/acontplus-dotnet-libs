---
inclusion: always
---

# Acontplus .NET Libraries — Project Overview

This is a **.NET 10 library monorepo** containing 13 NuGet packages (all prefixed `Acontplus.*`) and a Demo API application. All packages target `net10.0` with C# nullable and implicit usings enabled.

## Repository Structure

```
acontplus-dotnet-libs/
├── src/                          ← 13 NuGet library packages
│   ├── Acontplus.Core/           ← Domain primitives, Result<T>, specs, enums
│   ├── Acontplus.Utilities/      ← Helpers, encryption, string extensions
│   ├── Acontplus.Infrastructure/ ← Cross-cutting infrastructure concerns
│   ├── Acontplus.Persistence.Common/     ← Repository abstractions
│   ├── Acontplus.Persistence.SqlServer/  ← EF Core + SQL Server
│   ├── Acontplus.Persistence.PostgreSQL/ ← EF Core + PostgreSQL
│   ├── Acontplus.Notifications/  ← Email, WhatsApp, SMS, templates
│   ├── Acontplus.Billing/        ← Electronic invoicing, SRI integration
│   ├── Acontplus.Reports/        ← RDLC reports, PDF generation
│   ├── Acontplus.Services/       ← Caching, auth middleware
│   ├── Acontplus.Analytics/      ← Analytics support
│   ├── Acontplus.Logging/        ← Serilog configuration
│   ├── Acontplus.Barcode/        ← QR/barcode generation
│   ├── Acontplus.S3Application/  ← AWS S3 storage
│   └── Acontplus.ApiDocumentation/ ← Swagger/OpenAPI setup
├── apps/src/                     ← Demo application (Demo.Api, Demo.Application, etc.)
├── tests/                        ← xUnit test projects
├── docs/wiki/                    ← Wiki source of truth (synced to GitHub Wiki via publish-wiki.yml)
├── Directory.Packages.props      ← Central NuGet version management (CPM)
├── acontplus-dotnet-libs.slnx    ← Solution file (.slnx format)
└── .github/workflows/            ← CI/CD automation
```

## Package Dependency Layers

```
Foundation:   Core ← Utilities
Application:  Notifications, Billing, Reports, Services, Analytics, S3, Barcode, Logging, ApiDocs
              (all depend on Core)
Infrastructure: Persistence.Common ← Persistence.SqlServer / Persistence.PostgreSQL
                Infrastructure → Core
```

## Key Architectural Decisions

- **Central Package Management (CPM)**: All NuGet versions are in `Directory.Packages.props`. Never add `Version=""` directly to `<PackageReference>` in project files.
- **FrameworkReference split**: Libraries only usable in ASP.NET Core use `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (Utilities, Infrastructure, S3, Services, Reports, Notifications, Billing). Host-agnostic libraries use standalone NuGet packages (Core, Logging, Analytics, Barcode, Persistence.\*).
- **Result Pattern**: All service methods return `Result<T>` from `Acontplus.Core`. Never throw for expected failures.
- **Clean Architecture**: Domain → Application → Infrastructure → API layers. No upward dependencies.
- **GeneratePackageOnBuild = false**: Packing is always explicit via `dotnet pack`.
- **SourceLink + snupkg**: All packages include symbol packages and source linking.

## NuGet Publishing Workflow

1. Update version in `.csproj` + `Directory.Packages.props`
2. Open PR → `build-test.yml` validates on every branch/PR
3. Merge to `main` → `smart-publish.yml` detects version changes and publishes automatically
4. Daily `version-check.yml` monitors unpublished versions

## Common Commands

```bash
dotnet build acontplus-dotnet-libs.slnx    # Build everything
dotnet test                                  # Run all tests
dotnet pack src/Acontplus.Core --output nupkgs  # Pack a specific package

# Version bump scripts (PowerShell)
.\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor
.\batch-upgrade-version.ps1
```

## Commit Scope Reference

| Scope                    | Package                                    |
| ------------------------ | ------------------------------------------ |
| `core`                   | Acontplus.Core                             |
| `utilities`              | Acontplus.Utilities                        |
| `billing`                | Acontplus.Billing                          |
| `notifications`          | Acontplus.Notifications                    |
| `reports`                | Acontplus.Reports                          |
| `persistence`            | Acontplus.Persistence.Common               |
| `persistence-sqlserver`  | Acontplus.Persistence.SqlServer            |
| `persistence-postgresql` | Acontplus.Persistence.PostgreSQL           |
| `services`               | Acontplus.Services                         |
| `api-docs`               | Acontplus.ApiDocumentation                 |
| `logging`                | Acontplus.Logging                          |
| `barcode`                | Acontplus.Barcode                          |
| `s3`                     | Acontplus.S3Application                    |
| `analytics`              | Acontplus.Analytics                        |
| `infrastructure`         | Acontplus.Infrastructure                   |
| `demo-api`               | Demo.Api                                   |
| `build`                  | .csproj / Directory.Packages.props changes |
| `ci`                     | GitHub Actions workflows                   |
| `docs`                   | README / wiki / documentation              |
| `scripts`                | PowerShell scripts                         |

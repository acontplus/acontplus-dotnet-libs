---
applyTo: '**'
---

# Commit Message Guidelines

When generating commit messages, follow these rules strictly:

## Format

Generate ONLY a single-line commit message in Conventional Commits format:

```
<type>(scope): <description>
```

## Rules

- **Single line only** - no body, no bullet points, no additional paragraphs
- Maximum 72 characters total length
- **No emoji** - plain text only
- **No line breaks** - everything on one line
- Description should be between 1-50 characters after the type and scope

## Types

Use one of these types only:

- `feat` - new features or significant additions
- `fix` - bug fixes
- `docs` - documentation changes
- `style` - code style/formatting (no logic changes)
- `refactor` - code restructuring (no functionality changes)
- `test` - adding or modifying tests
- `chore` - maintenance, build, tooling updates
- `perf` - performance improvements
- `ci` - CI/CD configuration changes
- `build` - build system or dependency changes
- `revert` - reverting previous commits

## Scope

Use the affected library or component name in parentheses. Choose from:

### Library Packages (src/)
- `core` - Acontplus.Core (domain, result pattern, enums, specifications)
- `billing` - Acontplus.Billing (electronic invoicing, SRI integration)
- `notifications` - Acontplus.Notifications (email, WhatsApp, SMS, templates)
- `reports` - Acontplus.Reports (RDLC reports, PDF generation)
- `persistence` - Acontplus.Persistence.Common (repository abstractions)
- `persistence-sqlserver` - Acontplus.Persistence.SqlServer
- `persistence-postgresql` - Acontplus.Persistence.PostgreSQL
- `services` - Acontplus.Services (caching, auth, middleware)
- `utilities` - Acontplus.Utilities (helpers, encryption, extensions)
- `api-docs` - Acontplus.ApiDocumentation (Swagger/OpenAPI)
- `logging` - Acontplus.Logging (Serilog configuration)
- `barcode` - Acontplus.Barcode (QR/barcode generation)
- `s3` - Acontplus.S3Application (AWS S3 storage)

### Sample Applications (apps/)
- `demo-api` - Demo.Api
- `demo-app` - Demo.Application
- `demo-domain` - Demo.Domain
- `demo-infra` - Demo.Infrastructure

### Infrastructure & Configuration
- `build` - Build configuration, Directory.Packages.props, .csproj files
- `ci` - GitHub Actions, CI/CD workflows
- `docs` - Documentation, README files, wiki
- `config` - Solution-wide configuration, .editorconfig, Nuget.config
- `scripts` - PowerShell scripts (upgrade-version.ps1, etc.)
- `deps` - Dependency updates across multiple packages

### Special Cases
- Omit scope only if the change affects multiple libraries across the entire workspace
- For multi-package version updates, use `build(deps)` or just `build`
- Use kebab-case for multi-word scopes (e.g., `api-docs`, `demo-api`)

## Valid Examples

```
feat(core): add success message to Result type
feat(billing): add SRI electronic signature support
fix(persistence): correct transaction handling in SaveChanges
fix(notifications): handle null template variables
docs(reports): update RDLC usage examples
docs: update main README with .NET 9 features
refactor(utilities): simplify encryption service interface
refactor(services): extract cache configuration to options
chore(build): upgrade to .NET 9.0
test(core): add unit tests for Result pattern
perf(persistence-sqlserver): optimize bulk insert operations
build(deps): update NuGet dependencies
build: update Directory.Packages.props versions
ci: add automated package publishing workflow
style(api-docs): apply consistent code formatting
```

## Invalid Examples (DO NOT GENERATE)

```
❌ Refactor workspace verification script and tidy TypeScript configuration
❌ feat(ng-auth): add JWT refresh 🔐
❌ fix: resolve issue

   - Updated logic
   - Fixed bug
❌ feat: this is a very long description that exceeds the character limit and will fail validation
❌ fix(Acontplus.Core): use full package name instead of scope
❌ feat(accounting): wrong scope - not a library in this repo
❌ chore(test_api): use kebab-case not snake_case
```

## Context & Best Practices

This repository is a **.NET 9 library monorepo** containing:
- **13 library packages** distributed as NuGet packages
- **4 sample applications** demonstrating library usage
- **Central package management** via Directory.Packages.props
- Focus on **clean architecture**, **DDD patterns**, and **modern C# features**

When committing:
- Reference the **library scope**, not the full package name
- Use `build` for version bumps and package configuration
- Use `docs` for README and XML documentation updates
- Group related changes logically (don't mix feat + refactor in one commit)
- Keep descriptions specific and actionable

## Validation

The commit message MUST pass this regex validation:

```regex
^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\(.+\))?(!)?: .{1,50}
```

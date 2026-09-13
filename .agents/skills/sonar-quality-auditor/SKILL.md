---
name: sonar-quality-auditor
description: >-
  Audit, analyze, and resolve SonarQube quality issues and Quality Gate violations in
  Acontplus .NET Libraries. Use this skill when reviewing SonarQube scan reports, resolving
  cognitive complexity (S3776), string duplicate literals (S1192), async token forwarding (S8949/CA2016),
  or validating build and test compliance.
---

# Sonar Quality Auditor for Acontplus .NET Libraries

This skill encapsulates the guidelines, automated diagnosis scripts, and refactoring patterns
developed for Acontplus .NET Libraries to systematically prevent and fix SonarQube and Roslyn analyzer violations.

## Workflow

### Step 1: Run the Automated Issues Diagnostics Script
Execute the bundled diagnostic script to parse SonarQube JSON outputs (`issues.json` or `acontplus-sonarqube-results/issues.json`):

```bash
python3 .agents/skills/sonar-quality-auditor/scripts/audit_sonar_issues.py
```

The script will automatically:
- Filter out out-of-scope files (`appsettings*.json`, generated migration files, sample test payloads).
- Distinguish between actionable issues and architectural false positives (such as LINQ expression-tree projections).
- Group issues by library/module, severity, and rule ID.

### Step 2: Apply Standard Refactoring Patterns
Follow the verified design patterns documented in [references/patterns.md](./references/patterns.md):
- **Cognitive Complexity (S3776)**: Split complex methods into focused sub-methods.
- **Async CancellationToken (S8949 / CA2016)**: Forward tokens to `CommitAsync`, `GetByIdAsync`, `FindAsync`. Apply `[SuppressMessage]` where parameters cannot take tokens.
- **String Duplicate Literals (S1192)**: Declare `private const string` constants for recurring strings or error codes.
- **Collection Performance (CA1826 / CA1829 / CA1860 / S6608)**: Use `.Count == 0` instead of `!Any()`, indexer `[0]` instead of `.First()`, and `.Count` instead of `.Count()`.
- **Logging Cost (CA1873)**: Guard info/debug logs with `if (_logger.IsEnabled(LogLevel.Information))`.

### Step 3: Verify Integrity
Always verify the solution after applying changes:
```bash
dotnet build acontplus-dotnet-libs.slnx --configuration Release
dotnet test --solution acontplus-dotnet-libs.slnx --no-build
```

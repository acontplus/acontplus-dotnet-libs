---
name: dotnet-test-execution
description: >-
  Run .NET tests using the dotnet CLI. Use when asked to run tests, filter
  tests, configure test output, use Microsoft.Testing.Platform, or
  troubleshoot test execution failures. Covers xUnit v3, command-line
  filtering, parallel execution, and CI integration.
---

# .NET Test Execution

## When to Use

Activate this skill when:

* You need to run unit tests in Acontplus .NET Libraries.
* Tests are failing and you need to diagnose execution problems.
* You need to filter tests by name, class, or trait.
* You need to configure test execution or test coverage.

## Running Tests in Acontplus

### Run All Tests Across the Solution

```bash
dotnet test --solution acontplus-dotnet-libs.slnx --no-build
```

Or with Release configuration:
```bash
dotnet test --solution acontplus-dotnet-libs.slnx --configuration Release --no-build --verbosity normal
```

### Focused Project Testing

```bash
# Services tests
dotnet test --project tests/Acontplus.Services.Tests.Unit/Acontplus.Services.Tests.Unit.csproj

# Reports tests
dotnet test --project tests/Acontplus.Reports.Tests.Unit/Acontplus.Reports.Tests.Unit.csproj
```

### Microsoft.Testing.Platform (MTP)

Acontplus uses `Microsoft.Testing.Platform` (configured in `global.json`).
Projects support additional MTP options:

```bash
# List available tests
dotnet test --project tests/Acontplus.Services.Tests.Unit/Acontplus.Services.Tests.Unit.csproj --list-tests
```

## Filtering Tests

```bash
# By fully qualified name (contains)
dotnet test --filter "FullyQualifiedName~OrderService"

# By test name
dotnet test --filter "Name=Should_Return_Order_When_Valid_Id"

# By trait/category
dotnet test --filter "Category=Unit"

# Combine filters
dotnet test --filter "FullyQualifiedName~OrderService&Category=Unit"

# Exclude tests
dotnet test --filter "Category!=Integration"
```

## Output and Verbosity

```bash
# Detailed output
dotnet test --verbosity detailed

# Log to file (TRX format)
dotnet test --logger "trx;LogFileName=results.trx"

# Console logger with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Troubleshooting

### Common Failures

| Symptom | Likely Cause | Fix |
|---|---|---|
| "No test matches the given testcase filter" | Wrong filter syntax | Check `--filter` syntax and test names |
| Tests hang indefinitely | Deadlock or missing async/await | Add timeout: `[Fact(Timeout = 30000)]` |
| "Could not load file or assembly" | Missing dependency | Run `dotnet restore` and check project references |
| Flaky test failures | Shared state or timing | Isolate tests, use unique data per test |
| Integration tests fail | Docker not running | Start Docker: `systemctl start docker` |

### Running with Diagnostics

```bash
# Enable diagnostic logging
dotnet test --diag:test_diag.log

# Blame mode: identifies the test that causes a crash
dotnet test --blame
```

## Acontplus Test Conventions

* Test naming: `<Method>_<Condition>_<ExpectedOutcome>` (or `Method_Scenario_ExpectedBehavior`)
* Unit tests use xUnit, Moq, and Microsoft.Testing.Platform
* Tests live under `tests/Acontplus.<Name>.Tests.Unit/` mirroring the library folder structure
* Follow Arrange/Act/Assert pattern
* Verify tests locally with `dotnet test --solution acontplus-dotnet-libs.slnx --no-build`

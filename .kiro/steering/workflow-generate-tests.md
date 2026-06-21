---
inclusion: auto
name: workflow-generate-tests
description: Generate xUnit tests for an Acontplus library. Use when asked to write tests, add unit tests, create integration tests, or improve test coverage.
---

# Workflow: Generate Tests

Generate comprehensive unit and/or integration tests for a specified library or feature.

## Step 1 — Gather Information

Ask the user for the following before generating any files:

1. **Target library or file** — e.g. `Acontplus.Core`, a specific class, or a feature area
2. **Test type** — unit tests, integration tests, or both
3. **Scenarios to cover** — happy path, edge cases, error conditions (or "all" to infer from code)
4. **Test framework preference** — default: xUnit + FluentAssertions + NSubstitute

---

## Step 2 — Add Test Packages to Directory.Packages.props (if missing)

Check whether these `<PackageVersion>` entries exist. If any are missing, resolve the **latest stable version compatible with `net10.0`** from the NuGet API:

```
https://api.nuget.org/v3-flatcontainer/<packageid>/index.json
```

Pick the highest stable (non-prerelease) version, then add missing entries:

```xml
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="<resolved>" />
<PackageVersion Include="xunit" Version="<resolved>" />
<PackageVersion Include="xunit.runner.visualstudio" Version="<resolved>" />
<PackageVersion Include="FluentAssertions" Version="<resolved>" />
<PackageVersion Include="NSubstitute" Version="<resolved>" />
<PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="<resolved>" />
<PackageVersion Include="coverlet.collector" Version="<resolved>" />
```

Never hardcode versions — always resolve from NuGet.org at generation time.

---

## Step 3 — Create Test Project

Location: `tests/Acontplus.<Name>.Tests/Acontplus.<Name>.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Acontplus.<Name>\Acontplus.<Name>.csproj" />
  </ItemGroup>
</Project>
```

**Important**: Do NOT specify `Version` on `<PackageReference>` — versions are centrally managed in `Directory.Packages.props`.

---

## Step 4 — Unit Test Conventions

### File naming

One file per class under test: `<ClassName>Tests.cs`, mirroring source structure.

### Test class structure

```csharp
namespace Acontplus.<Name>.Tests.<Folder>;

public sealed class <ClassName>Tests
{
    private readonly I<Dependency> _dep = Substitute.For<I<Dependency>>();
    private readonly <ClassName> _sut;

    public <ClassName>Tests() => _sut = new <ClassName>(_dep);

    [Fact]
    public async Task <MethodName>_<Condition>_<ExpectedOutcome>()
    {
        // Arrange
        // Act
        var result = await _sut.<Method>(...);
        // Assert
        result.Should().<Assertion>();
    }
}
```

### Naming pattern: `<Method>_<Condition>_<ExpectedOutcome>`

```
GetById_WhenEntityExists_ReturnsSuccessResult
GetById_WhenEntityNotFound_ReturnsFailureResult
Save_WithNullEntity_ThrowsArgumentNullException
```

### Result Pattern assertions (Acontplus.Core)

```csharp
// Success
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

// Failure
result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be("EXPECTED_CODE");
```

---

## Step 5 — Integration Test Conventions

```csharp
public sealed class <Feature>IntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;

    public <Feature>IntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real DB with in-memory or test container
                });
            });
    }

    public async Task InitializeAsync() { _client = _factory.CreateClient(); await Task.CompletedTask; }
    public async Task DisposeAsync() { _factory.Dispose(); await Task.CompletedTask; }

    [Fact]
    public async Task <Endpoint>_<Condition>_Returns<StatusCode>()
    {
        var response = await _client.GetAsync("/api/...");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## Coverage Goals

- All public methods covered (happy path + at least one failure path)
- Null / boundary inputs validated
- Async methods tested with `await` — never `.Result` or `.Wait()`
- No `Thread.Sleep` — use proper async patterns

---

## Step 6 — Register Project in Solution

Add to `acontplus-dotnet-libs.slnx` under `/tests/`:

```xml
<Folder Name="/tests/">
  <Project Path="tests/Acontplus.<Name>.Tests/Acontplus.<Name>.Tests.csproj" />
</Folder>
```

---

## Step 7 — Verify

```bash
dotnet build tests/Acontplus.<Name>.Tests
dotnet test tests/Acontplus.<Name>.Tests --no-build
```

Report the final test count and any failures.

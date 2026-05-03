---
agent: agent
description: Generate unit and integration tests for an Acontplus library or feature
tools:
  - create_file
  - read_file
  - file_search
  - semantic_search
  - grep_search
  - fetch_webpage
---

# Generate Tests for Acontplus Library

Generate comprehensive unit and/or integration tests for a specified library or feature in this monorepo.

## Required Information

Ask the user for the following before generating any files:

1. **Target library or file** — e.g. `Acontplus.Core`, a specific class, or a feature area
2. **Test type** — unit tests, integration tests, or both
3. **Scenarios to cover** — happy path, edge cases, error conditions (or "all" to infer from code)
4. **Test framework preference** — default: xUnit + FluentAssertions + NSubstitute

---

## Test Project Location

- Unit tests: `tests/Acontplus.<Name>.Tests/`
- Integration tests: `tests/Acontplus.<Name>.IntegrationTests/`

If the project does not exist yet:

### Step 0 — Add test packages to `Directory.Packages.props`

First, check whether the following `<PackageVersion>` entries exist in `Directory.Packages.props`. If any are missing, resolve the **latest stable version compatible with `net10.0`** for each package by querying the NuGet.org API before writing any version number:

```
https://api.nuget.org/v3-flatcontainer/<packageid>/index.json
```

Pick the highest stable (non-prerelease) version from the `versions` array. Then add the missing entries:

```xml
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="<resolved>" />
<PackageVersion Include="xunit" Version="<resolved>" />
<PackageVersion Include="xunit.runner.visualstudio" Version="<resolved>" />
<PackageVersion Include="FluentAssertions" Version="<resolved>" />
<PackageVersion Include="NSubstitute" Version="<resolved>" />
<PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="<resolved>" />
<PackageVersion Include="coverlet.collector" Version="<resolved>" />
```

> Never hardcode version numbers — always resolve from NuGet.org at generation time.

### Step 1 — Create `tests/Acontplus.<Name>.Tests/Acontplus.<Name>.Tests.csproj`

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

> **Important**: Do NOT specify `Version` on `<PackageReference>` — versions are centrally managed in `Directory.Packages.props`.

---

## Unit Test Conventions

### File naming

- One file per class under test: `<ClassName>Tests.cs`
- Place in a subfolder mirroring the source structure:
  - `Services/` → `tests/.../Services/<ClassName>Tests.cs`
  - `Extensions/` → `tests/.../Extensions/<ClassName>Tests.cs`

### Test class structure

```csharp
namespace Acontplus.<Name>.Tests.<Folder>;

public sealed class <ClassName>Tests
{
    // Arrange shared state via constructor or field initializers
    private readonly I<Dependency> _dep = Substitute.For<I<Dependency>>();
    private readonly <ClassName> _sut;

    public <ClassName>Tests()
    {
        _sut = new <ClassName>(_dep);
    }

    [Fact]
    public async Task <MethodName>_<Condition>_<ExpectedOutcome>()
    {
        // Arrange
        ...
        // Act
        var result = await _sut.<Method>(...);
        // Assert
        result.Should().<Assertion>();
    }

    [Theory]
    [InlineData(...)]
    public void <MethodName>_WithVariousInputs_<ExpectedOutcome>(...)
    {
        ...
    }
}
```

### Naming pattern: `<Method>_<Condition>_<ExpectedOutcome>`

- `GetById_WhenEntityExists_ReturnsSuccessResult`
- `GetById_WhenEntityNotFound_ReturnsFailureResult`
- `Save_WithNullEntity_ThrowsArgumentNullException`

---

## Integration Test Conventions

Use `WebApplicationFactory<TEntryPoint>` or a custom `IAsyncLifetime` fixture for database-backed tests.

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

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task <Endpoint>_<Condition>_Returns<StatusCode>()
    {
        // Arrange + Act
        var response = await _client.GetAsync("/api/...");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

---

## Result Pattern Assertions (Acontplus.Core)

When the method under test returns `Result<T>` or `Result`:

```csharp
// Success
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

// Failure
result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be("EXPECTED_CODE");
```

---

## Coverage Goals

Generate tests to achieve at minimum:

- All public methods covered (happy path + at least one failure path)
- Null / boundary inputs validated
- Async methods tested with `await` — never `.Result` or `.Wait()`
- No `Thread.Sleep` — use `Task.Delay` or proper async test patterns

---

## After Generating Tests

Remind the user to:

1. Add the new test project to `acontplus-dotnet-libs.slnx`
2. Add any missing test packages to `Directory.Packages.props`
3. Run `dotnet test tests/Acontplus.<Name>.Tests` to verify

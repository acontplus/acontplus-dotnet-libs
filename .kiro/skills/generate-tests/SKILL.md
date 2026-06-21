---
name: generate-tests
description: Generate xUnit unit and integration tests for an Acontplus library or class following monorepo conventions. Use when adding tests or improving test coverage.
---

## Process

### Step 1 — Clarify (if not provided)

1. **Target** — library name, specific class, or feature area
2. **Test type** — unit, integration, or both
3. **Scenarios** — happy path, edge cases, error conditions, or "all"

---

### Step 2 — Check / Add Packages to Directory.Packages.props

Verify these entries exist. If missing, resolve **latest stable for net10.0** from NuGet API:
`https://api.nuget.org/v3-flatcontainer/<id>/index.json`

```xml
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="<resolved>" />
<PackageVersion Include="xunit" Version="<resolved>" />
<PackageVersion Include="xunit.runner.visualstudio" Version="<resolved>" />
<PackageVersion Include="FluentAssertions" Version="<resolved>" />
<PackageVersion Include="NSubstitute" Version="<resolved>" />
<PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="<resolved>" />
<PackageVersion Include="coverlet.collector" Version="<resolved>" />
```

---

### Step 3 — Create Test Project (if it doesn't exist)

Path: `tests/Acontplus.<Name>.Tests/Acontplus.<Name>.Tests.csproj`

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

No `Version=""` on any `<PackageReference>` — centrally managed.

---

### Step 4 — Write Unit Tests

Naming: `<Method>_<Condition>_<ExpectedOutcome>`

```csharp
namespace Acontplus.<Name>.Tests.<Folder>;

public sealed class <ClassName>Tests
{
    private readonly I<Dep> _dep = Substitute.For<I<Dep>>();
    private readonly <ClassName> _sut;

    public <ClassName>Tests() => _sut = new <ClassName>(_dep);

    [Fact]
    public async Task Method_WhenCondition_ReturnsExpected()
    {
        // Arrange
        // Act
        var result = await _sut.MethodAsync(...);
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData(...)]
    public void Method_WithVariousInputs_ExpectedBehavior(...) { }
}
```

Result Pattern assertions:

```csharp
// Success
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

// Failure
result.IsFailure.Should().BeTrue();
result.Error.Code.Should().Be("EXPECTED_CODE");
```

Coverage minimum: all public methods, happy path + failure path, boundary inputs, no `.Result`/`.Wait()`, no `Thread.Sleep`.

---

### Step 5 — Write Integration Tests (if requested)

```csharp
public sealed class <Feature>IntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client = null!;

    public <Feature>IntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                // swap real DB for test double
            }));
    }

    public async Task InitializeAsync() { _client = _factory.CreateClient(); await Task.CompletedTask; }
    public async Task DisposeAsync() { _factory.Dispose(); await Task.CompletedTask; }
}
```

---

### Step 6 — Register in Solution

Add to `acontplus-dotnet-libs.slnx` under `/tests/`:

```xml
<Project Path="tests/Acontplus.<Name>.Tests/Acontplus.<Name>.Tests.csproj" />
```

---

### Step 7 — Verify

```bash
dotnet build tests/Acontplus.<Name>.Tests
dotnet test tests/Acontplus.<Name>.Tests --no-build
```

Report final test count and any failures.

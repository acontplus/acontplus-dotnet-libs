---
name: dotnet-test-generation
description: >-
  Generate .NET unit tests for existing code. Use when asked to create tests
  for a class, method, or module. Covers test project setup, naming
  conventions (Method_Scenario_ExpectedBehavior), assertion patterns, mocking
  with Moq, and test data generation for xUnit v3.
---

# .NET Test Generation

## When to Use

Activate this skill when:

* A user asks to write tests for existing production code.
* You need to add tests to Acontplus library test projects.
* You need guidance on test naming, structure, or assertion patterns.

## Test Projects in Acontplus

* **Unit tests**: Located in `tests/Acontplus.<Name>.Tests.Unit/` (e.g. `tests/Acontplus.Services.Tests.Unit/`, `tests/Acontplus.Reports.Tests.Unit/`).
* **Test file structure**: Mirrors the production folder structure, named `<ClassName>Tests.cs`.

## Test Structure and Naming

### Naming Convention

Follow `AGENTS.md`: `<Method>_<Condition>_<ExpectedOutcome>` (or `Method_Scenario_ExpectedBehavior`).

```csharp
public class TenantResolutionServiceTests
{
    [Fact]
    public async Task ResolveTenant_WithValidHeader_ReturnsSuccess()
    {
        // Arrange
        // Act
        // Assert
    }

    [Fact]
    public async Task ResolveTenant_WithMissingHeader_ReturnsFailure()
    {
        // ...
    }
}
```

## Writing Tests with Moq

### Basic Test Pattern

```csharp
public class EnrollmentServiceTests
{
    private readonly Mock<IEnrollmentRepository> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<EnrollmentService>> _loggerMock = new();
    private readonly EnrollmentService _sut;

    public EnrollmentServiceTests()
    {
        _sut = new EnrollmentService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsEnrollment()
    {
        // Arrange
        var expectedEnrollment = new Enrollment { Id = Guid.NewGuid() };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(expectedEnrollment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEnrollment);

        // Act
        var result = await _sut.GetByIdAsync(expectedEnrollment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFoundError()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enrollment?)null);

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
    }
}
```

### Parameterized Tests

```csharp
[Theory]
[InlineData("", false)]
[InlineData("a", false)]
[InlineData("valid@email.com", true)]
public void IsValidEmail_WithVariousInputs_ReturnsExpected(
    string email, bool expected)
{
    var result = EmailValidator.IsValid(email);
    Assert.Equal(expected, result);
}
```

### Testing Result<T, DomainError> Pattern

```csharp
[Fact]
public async Task Create_WithDuplicateEmail_ReturnsConflictError()
{
    // Arrange
    _repositoryMock
        .Setup(r => r.ExistsByEmailAsync("test@test.com", It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    // Act
    var result = await _sut.CreateAsync(new CreateStudentDto { Email = "test@test.com" });

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(DomainErrorType.Conflict, result.Error.Type);
}
```

## Best Practices

* **One assertion concept per test** – test one behavior, not multiple.
* **Don't test implementation details** – test behavior and outcomes.
* **Use descriptive names** – the test name should describe the scenario.
* **Avoid test interdependence** – each test must be independently runnable.
* **Keep tests fast** – mock external dependencies, avoid I/O.
* **Use `Result<T, DomainError>` assertions** – never throw exceptions for business validation.

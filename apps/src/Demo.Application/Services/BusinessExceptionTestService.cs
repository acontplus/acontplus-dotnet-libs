namespace Demo.Application.Services;

/// <summary>
/// Service implementation that returns domain Results instead of throwing.
/// </summary>
public class BusinessExceptionTestService : IBusinessExceptionTestService
{
    private readonly ILogger<BusinessExceptionTestService> _logger;

    // Simulated in-memory customer store
    private readonly Dictionary<int, CustomerModel> _customers = new()
    {
        [1] = new CustomerModel { Id = 1, Name = "John Doe", Email = "john@example.com" },
        [2] = new CustomerModel { Id = 2, Name = "Jane Smith", Email = "jane@example.com" }
    };

    public BusinessExceptionTestService(ILogger<BusinessExceptionTestService> logger)
    {
        _logger = logger;
    }

    public Task<Result<object, DomainErrors>> ValidateEmailAsync(string email)
    {
        _logger.LogInformation("Validating email: {Email}", email);

        if (string.IsNullOrWhiteSpace(email))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Email is required" }
            };

            var domainErrors = DomainErrors.Multiple(errors.SelectMany(kvp => kvp.Value.Select(m => DomainError.Validation("EMAIL_REQUIRED", m, kvp.Key))).ToList());
            return Task.FromResult(Result<object, DomainErrors>.Failure(domainErrors));
        }

        if (!email.Contains('@'))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Email format is invalid", "Email must contain @ symbol" }
            };

            var domainErrors = DomainErrors.Multiple(errors.SelectMany(kvp => kvp.Value.Select(m => DomainError.Validation("EMAIL_INVALID_FORMAT", m, kvp.Key))).ToList());
            return Task.FromResult(Result<object, DomainErrors>.Failure(domainErrors));
        }

        return Task.FromResult(Result<object, DomainErrors>.Success(new { message = "Validation passed" }));
    }

    public Task<Result<CustomerModel, DomainError>> GetCustomerAsync(int id)
    {
        _logger.LogInformation("Getting customer with ID: {Id}", id);

        if (!_customers.ContainsKey(id))
        {
            _logger.LogWarning("Customer not found: {Id}", id);
            return Task.FromResult(Result<CustomerModel, DomainError>.Failure(
                DomainError.NotFound("CUSTOMER_NOT_FOUND", $"Customer with ID {id} was not found in the system")));
        }

        return Task.FromResult(Result<CustomerModel, DomainError>.Success(_customers[id]));
    }

    public Task<Result<CustomerModel, DomainError>> CreateCustomerAsync(string email)
    {
        _logger.LogInformation("Creating customer with email: {Email}", email);

        if (_customers.Values.Any(c => c.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            _logger.LogWarning("Customer with email already exists: {Email}", email);
            return Task.FromResult(Result<CustomerModel, DomainError>.Failure(
                DomainError.Conflict("DUPLICATE_EMAIL", $"A customer with email '{email}' already exists")));
        }

        var newCustomer = new CustomerModel
        {
            Id = _customers.Keys.Max() + 1,
            Email = email,
            Name = "New Customer"
        };

        _customers.Add(newCustomer.Id, newCustomer);
        return Task.FromResult(Result<CustomerModel, DomainError>.Success(newCustomer));
    }

    public Task<Result<object, DomainError>> ExecuteDatabaseOperationAsync()
    {
        _logger.LogInformation("Executing database operation that will fail");

        // Simulate SQL foreign key violation converted to DomainError result
        var error = DomainError.Conflict(
            "FK_VIOLATION",
            "Cannot delete customer because it has associated orders. Please delete the orders first");

        return Task.FromResult(Result<object, DomainError>.Failure(error));
    }

    public Task<Result<object, DomainError>> ProcessComplexOperationAsync()
    {
        _logger.LogInformation("Processing complex operation that may fail");

        try
        {
            SimulateInternalOperation();
            return Task.FromResult(Result<object, DomainError>.Success(new { message = "Operation completed" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complex operation failed");
            var error = DomainError.Internal(
                "COMPLEX_OPERATION_FAILED",
                "An unexpected error occurred during complex operation processing");
            return Task.FromResult(Result<object, DomainError>.Failure(error));
        }
    }

    public async Task<Result<CustomerModel, DomainError>> GetCustomerWithDeepStackAsync(int id)
    {
        _logger.LogInformation("Getting customer with deep call stack: {Id}", id);

        try
        {
            var customer = await GetFromRepositoryAsync(id); // may throw internally
            return Result<CustomerModel, DomainError>.Success(customer);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error in deep stack: {Code}", ex.ErrorCode);
            return Result<CustomerModel, DomainError>.Failure(new DomainError(ex.ErrorType, ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in deep stack");
            return Result<CustomerModel, DomainError>.Failure(DomainError.Internal("DEEP_STACK_ERROR", "Failed during deep call stack"));
        }
    }

    public Task<Result<object, DomainError>> AsyncOperationThatFailsAsync()
    {
        _logger.LogInformation("Executing async operation that will fail");

        // Simulate async failure as DomainError result
        var error = DomainError.ServiceUnavailable(
            "ASYNC_OPERATION_FAILED",
            "The async operation failed due to service unavailability");

        return Task.FromResult(Result<object, DomainError>.Failure(error));
    }

    public Task<Result<object, DomainError>> TaskRunOperationAsync()
    {
        _logger.LogInformation("Executing Task.Run operation that will fail");

        var error = DomainError.Timeout(
            "TASK_RUN_TIMEOUT",
            "The background task timed out after 10ms");

        return Task.FromResult(Result<object, DomainError>.Failure(error));
    }

    public Task<Result<CustomerModel, DomainError>> GetValidCustomerAsync(int id)
    {
        _logger.LogInformation("Getting valid customer: {Id}", id);

        var customer = _customers.ContainsKey(id) ? _customers[id] : _customers[1];
        return Task.FromResult(Result<CustomerModel, DomainError>.Success(customer));
    }

    #region Private Helper Methods

    /// <summary>
    /// Simulates repository layer that throws exception.
    /// </summary>
    private async Task<CustomerModel> GetFromRepositoryAsync(int id)
    {
        _logger.LogDebug("Repository: Fetching customer {Id}", id);

        // Simulate repository calling database layer
        await SimulateDatabaseCallAsync(id);

        return _customers.ContainsKey(id)
            ? _customers[id]
            : throw new GenericDomainException(
                ErrorType.NotFound,
                "CUSTOMER_NOT_FOUND_IN_DB",
                $"Customer {id} not found in database");
    }

    /// <summary>
    /// Simulates database layer that throws exception.
    /// </summary>
    private Task SimulateDatabaseCallAsync(int id)
    {
        _logger.LogDebug("Database: Executing query for customer {Id}", id);

        // Simulate database operation that might fail
        //if (id > 1000)
        //{
        //    var sqlErrorInfo = new SqlErrorInfo(
        //        ErrorType.Timeout,
        //        "SQL_TIMEOUT",
        //        "Database query timed out after 30 seconds",
        //        new TimeoutException("SQL Server timeout")
        //    );

        //    throw new SqlDomainException(sqlErrorInfo);
        //}

        return Task.CompletedTask;
    }

    /// <summary>
    /// Simulates internal operation that throws exception.
    /// </summary>
    private void SimulateInternalOperation()
    {
        _logger.LogDebug("Simulating internal operation");

        // Simulate some condition that causes failure
        var randomFailure = new Random().Next(0, 2);
        if (randomFailure == 0)
        {
            throw new InvalidOperationException("Internal operation failed due to invalid state");
        }
    }

    #endregion
}

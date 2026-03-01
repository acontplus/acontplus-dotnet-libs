namespace Demo.Api.Endpoints.Demo;

public static class ExceptionTestEndpoints
{
    public static void MapExceptionTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/exception-test")
            .WithTags("Exception Test");

        #region Client Errors (4xx) - DomainException Examples

        group.MapPost("/validation-error", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating validation error");

            throw new GenericDomainException(
                ErrorType.Validation,
                "INVALID_EMAIL",
                "The email address format is invalid");
        });

        group.MapPost("/validation-error-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating validation error using Result pattern");

            var error = DomainError.Validation(
                "INVALID_EMAIL",
                "The email address format is invalid",
                "email");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapPost("/bad-request", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating bad request error");

            throw new GenericDomainException(
                ErrorType.BadRequest,
                "INVALID_JSON",
                "The request body contains invalid JSON syntax");
        });

        group.MapPost("/bad-request-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating bad request using Result pattern");

            var error = DomainError.BadRequest(
                "INVALID_JSON",
                "The request body contains invalid JSON syntax");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/not-found/{id:int}", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, int id) =>
        {
            logger.LogInformation("Simulating not found error for ID: {Id}", id);

            throw new GenericDomainException(
                ErrorType.NotFound,
                "CUSTOMER_NOT_FOUND",
                $"Customer with ID {id} was not found");
        });

        group.MapGet("/not-found-result/{id:int}", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, int id) =>
        {
            logger.LogInformation("Simulating not found using Result pattern for ID: {Id}", id);

            var error = DomainError.NotFound(
                "CUSTOMER_NOT_FOUND",
                $"Customer with ID {id} was not found",
                "customerId");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapPost("/conflict", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating conflict error");

            throw new GenericDomainException(
                ErrorType.Conflict,
                "DUPLICATE_EMAIL",
                "A user with this email address already exists");
        });

        group.MapPost("/conflict-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating conflict using Result pattern");

            var error = DomainError.Conflict(
                "DUPLICATE_EMAIL",
                "A user with this email address already exists",
                "email");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/unauthorized", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating unauthorized error");

            throw new GenericDomainException(
                ErrorType.Unauthorized,
                "INVALID_TOKEN",
                "The authentication token is invalid or expired");
        });

        group.MapGet("/unauthorized-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating unauthorized using Result pattern");

            var error = DomainError.Unauthorized(
                "INVALID_TOKEN",
                "The authentication token is invalid or expired");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/forbidden", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating forbidden error");

            throw new GenericDomainException(
                ErrorType.Forbidden,
                "INSUFFICIENT_PERMISSIONS",
                "You do not have permission to access this resource");
        });

        group.MapGet("/forbidden-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating forbidden using Result pattern");

            var error = DomainError.Forbidden(
                "INSUFFICIENT_PERMISSIONS",
                "You do not have permission to access this resource");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/rate-limited", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating rate limit error");

            throw new GenericDomainException(
                ErrorType.RateLimited,
                "RATE_LIMIT_EXCEEDED",
                "Rate limit exceeded. Please try again in 60 seconds");
        });

        group.MapGet("/rate-limited-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating rate limit using Result pattern");

            var error = DomainError.RateLimited(
                "RATE_LIMIT_EXCEEDED",
                "Rate limit exceeded. Please try again in 60 seconds",
                details: new Dictionary<string, object>
                {
                    ["retryAfterSeconds"] = 60,
                    ["limit"] = 100,
                    ["resetTime"] = DateTime.UtcNow.AddSeconds(60)
                });

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapPost("/payload-too-large", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating payload too large error");

            throw new GenericDomainException(
                ErrorType.PayloadTooLarge,
                "PAYLOAD_TOO_LARGE",
                "The request body exceeds the maximum allowed size of 10MB");
        });

        #endregion

        #region Server Errors (5xx) - DomainException Examples

        group.MapGet("/internal-error", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating internal server error");

            throw new GenericDomainException(
                ErrorType.Internal,
                "INTERNAL_ERROR",
                "An unexpected error occurred while processing your request");
        });

        group.MapGet("/internal-error-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating internal error using Result pattern");

            var error = DomainError.Internal(
                "INTERNAL_ERROR",
                "An unexpected error occurred while processing your request");

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/service-unavailable", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating service unavailable error");

            throw new GenericDomainException(
                ErrorType.ServiceUnavailable,
                "SERVICE_UNAVAILABLE",
                "The service is temporarily unavailable. Please try again later");
        });

        group.MapGet("/service-unavailable-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating service unavailable using Result pattern");

            var error = DomainError.ServiceUnavailable(
                "SERVICE_UNAVAILABLE",
                "The service is temporarily unavailable. Please try again later",
                details: new Dictionary<string, object>
                {
                    ["estimatedDowntime"] = "5 minutes",
                    ["maintenanceMode"] = true
                });

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/timeout", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating timeout error");

            throw new GenericDomainException(
                ErrorType.Timeout,
                "EXTERNAL_SERVICE_TIMEOUT",
                "The external payment service did not respond within the expected time");
        });

        group.MapGet("/timeout-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating timeout using Result pattern");

            var error = DomainError.Timeout(
                "EXTERNAL_SERVICE_TIMEOUT",
                "The external payment service did not respond within the expected time",
                "paymentService",
                new Dictionary<string, object>
                {
                    ["timeoutSeconds"] = 30,
                    ["service"] = "PaymentGateway"
                });

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        group.MapGet("/not-implemented", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating not implemented error");

            throw new GenericDomainException(
                ErrorType.NotImplemented,
                "FEATURE_NOT_IMPLEMENTED",
                "This feature is not yet implemented");
        });

        group.MapGet("/external-error", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating external service error");

            throw new GenericDomainException(
                ErrorType.External,
                "EXTERNAL_API_ERROR",
                "The external payment gateway returned an error");
        });

        group.MapGet("/external-error-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating external error using Result pattern");

            var error = DomainError.External(
                "EXTERNAL_API_ERROR",
                "The external payment gateway returned an error",
                "paymentGateway",
                new Dictionary<string, object>
                {
                    ["externalErrorCode"] = "GATEWAY_503",
                    ["externalMessage"] = "Service temporarily unavailable"
                });

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        #endregion

        #region SQL Exception Examples

        group.MapDelete("/sql/foreign-key-violation", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating SQL foreign key violation");

            var sqlErrorInfo = new SqlErrorInfo(
                ErrorType.Conflict,
                "FK_VIOLATION",
                "Cannot delete customer because it has associated orders",
                new InvalidOperationException("SQL Server Error 547: Foreign Key Violation")
            );

            throw new SqlDomainException(sqlErrorInfo);
        });

        group.MapPost("/sql/unique-violation", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating SQL unique constraint violation");

            var sqlErrorInfo = new SqlErrorInfo(
                ErrorType.Conflict,
                "UNIQUE_VIOLATION",
                "A record with this email address already exists",
                new InvalidOperationException("SQL Server Error 2627: Unique Constraint Violation")
            );

            throw new SqlDomainException(sqlErrorInfo);
        });

        group.MapGet("/sql/timeout", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating SQL timeout error");

            var sqlErrorInfo = new SqlErrorInfo(
                ErrorType.Timeout,
                "SQL_TIMEOUT",
                "The database operation timed out after 30 seconds",
                new TimeoutException("SQL Server Timeout")
            );

            throw new SqlDomainException(sqlErrorInfo);
        });

        group.MapPost("/sql/deadlock", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating SQL deadlock error");

            var sqlErrorInfo = new SqlErrorInfo(
                ErrorType.Conflict,
                "SQL_DEADLOCK",
                "A database deadlock occurred. The transaction has been rolled back",
                new InvalidOperationException("SQL Server Error 1205: Deadlock")
            );

            throw new SqlDomainException(sqlErrorInfo);
        });

        group.MapGet("/sql/connection-error", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating SQL connection error");

            var sqlErrorInfo = new SqlErrorInfo(
                ErrorType.ServiceUnavailable,
                "SQL_CONNECTION_FAILED",
                "Unable to connect to the database server",
                new InvalidOperationException("SQL Server Error: Connection Failed")
            );

            throw new SqlDomainException(sqlErrorInfo);
        });

        #endregion

        #region Standard .NET Exceptions

        group.MapPost("/standard/argument-null", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating ArgumentNullException");

            throw new ArgumentNullException("customerId", "Customer ID cannot be null");
        });

        group.MapPost("/standard/argument-invalid", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating ArgumentException");

            throw new ArgumentException("Age must be between 0 and 150", "age");
        });

        group.MapPost("/standard/invalid-operation", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating InvalidOperationException");

            throw new InvalidOperationException("Cannot process payment for an order that is already cancelled");
        });

        group.MapGet("/standard/timeout", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating TimeoutException");

            throw new TimeoutException("The operation timed out after 30 seconds");
        });

        group.MapGet("/standard/null-reference", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating NullReferenceException");

            string? nullString = null;
            return Results.Ok(nullString!.Length);
        });

        group.MapGet("/standard/divide-by-zero", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating DivideByZeroException");

            int divisor = 0;
            int result = 100 / divisor;
            return Results.Ok(result);
        });

        #endregion

        #region Complex Scenarios

        group.MapGet("/complex/nested", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating nested exception");

            try
            {
                try
                {
                    throw new InvalidOperationException("Inner database error");
                }
                catch (Exception innerEx)
                {
                    throw new GenericDomainException(
                        ErrorType.Internal,
                        "DATABASE_ERROR",
                        "Failed to execute database operation",
                        innerEx);
                }
            }
            catch (Exception ex)
            {
                throw new GenericDomainException(
                    ErrorType.Internal,
                    "BUSINESS_LOGIC_ERROR",
                    "Business logic failed during customer creation",
                    ex);
            }
        });

        group.MapGet("/complex/aggregate", async ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating aggregate exception");

            var tasks = new List<Task>
            {
                Task.Run(() => throw new InvalidOperationException("Service 1 failed")),
                Task.Run(() => throw new TimeoutException("Service 2 timed out")),
                Task.Run(() => throw new ArgumentException("Service 3 received invalid input"))
            };

            await Task.WhenAll(tasks);
            return Results.Ok();
        });

        group.MapPost("/complex/multiple-validation", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating multiple validation errors");

            var errors = new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Email is required", "Email format is invalid" },
                ["password"] = new[] { "Password must be at least 8 characters", "Password must contain a number" },
                ["age"] = new[] { "Age must be between 18 and 100" }
            };

            throw new ValidationException(errors);
        });

        group.MapPost("/complex/multiple-validation-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating multiple validation errors using Result pattern");

            var errors = new List<DomainError>
            {
                DomainError.Validation("EMAIL_REQUIRED", "Email is required", "email"),
                DomainError.Validation("EMAIL_INVALID_FORMAT", "Email format is invalid", "email"),
                DomainError.Validation("PASSWORD_TOO_SHORT", "Password must be at least 8 characters", "password"),
                DomainError.Validation("PASSWORD_MISSING_NUMBER", "Password must contain a number", "password"),
                DomainError.Validation("AGE_OUT_OF_RANGE", "Age must be between 18 and 100", "age")
            };

            var domainErrors = DomainErrors.Multiple(errors);
            var result = Result<object, DomainErrors>.Failure(domainErrors);

            return result.ToMinimalApiResult();
        });

        group.MapPost("/complex/business-rule", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating business rule violation");

            throw new GenericDomainException(
                ErrorType.Validation,
                "BUSINESS_RULE_VIOLATION",
                "Cannot create order: Customer credit limit exceeded and payment method requires preauthorization");
        });

        group.MapPost("/complex/business-rule-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Simulating business rule violation using Result pattern");

            var error = DomainError.Validation(
                "BUSINESS_RULE_VIOLATION",
                "Cannot create order: Customer credit limit exceeded and payment method requires preauthorization",
                "order",
                new Dictionary<string, object>
                {
                    ["customerId"] = 12345,
                    ["creditLimit"] = 5000.00m,
                    ["orderTotal"] = 7500.00m,
                    ["paymentMethod"] = "CreditCard",
                    ["requiresPreauth"] = true
                });

            var result = Result<object, DomainError>.Failure(error);
            return result.ToMinimalApiResult();
        });

        #endregion

        #region Success Cases for Comparison

        group.MapGet("/success", () =>
        {
            return Results.Ok(new
            {
                success = true,
                message = "Request processed successfully",
                data = new { id = 123, name = "Test Customer", status = "Active" },
                timestamp = DateTime.UtcNow
            });
        });

        group.MapGet("/success-result", () =>
        {
            var customer = new { id = 123, name = "Test Customer", status = "Active" };
            var result = Result<object, DomainError>.Success(customer);

            return result.ToMinimalApiResult();
        });

        group.MapGet("/success-result-message", () =>
        {
            var customer = new { id = 123, name = "Test Customer", status = "Active" };
            var result = Result<object, DomainError>.Success(customer);

            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        group.MapGet("/success-result-async", async () =>
        {
            await Task.Delay(10);

            var customer = new { id = 456, name = "Async Customer", status = "Active" };
            var result = Result<object, DomainError>.Success(customer);

            return await Task.FromResult(result).ToMinimalApiResultAsync();
        });

        group.MapGet("/crud/get/{id:int}", (int id) =>
        {
            var customer = id <= 100
                ? new { id, name = $"Customer {id}", status = "Active" }
                : null;

            var result = customer != null
                ? Result<object?, DomainError>.Success(customer)
                : Result<object?, DomainError>.Success(null);

            return result.ToGetMinimalApiResult();
        });

        group.MapPost("/crud/create", () =>
        {
            var newCustomer = new { id = 999, name = "New Customer", status = "Active" };
            var result = Result<object, DomainError>.Success(newCustomer);

            var locationUri = $"/api/customers/{newCustomer.id}";
            return result.ToCreatedMinimalApiResult(locationUri);
        });

        group.MapPut("/crud/update/{id:int}", (int id) =>
        {
            var updatedCustomer = new { id, name = $"Updated Customer {id}", status = "Active" };
            var result = Result<object, DomainError>.Success(updatedCustomer);

            return result.ToPutMinimalApiResult();
        });

        group.MapDelete("/crud/delete/{id:int}", (int id) =>
        {
            bool deleted = id <= 100;

            var result = deleted
                ? Result<bool, DomainError>.Success(true)
                : Result<bool, DomainError>.Failure(
                    DomainError.NotFound("CUSTOMER_NOT_FOUND", $"Customer {id} not found"));

            return result.ToDeleteMinimalApiResult();
        });

        #endregion

        #region Random Exception Generator

        group.MapGet("/random", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            var random = Random.Shared.Next(1, 16);

            logger.LogInformation("Throwing random exception type: {Type}", random);

            return random switch
            {
                1 => throw new GenericDomainException(ErrorType.Validation, "INVALID_EMAIL", "The email address format is invalid"),
                2 => throw new GenericDomainException(ErrorType.NotFound, "CUSTOMER_NOT_FOUND", "Customer not found"),
                3 => throw new GenericDomainException(ErrorType.Conflict, "DUPLICATE_EMAIL", "Duplicate email"),
                4 => throw new GenericDomainException(ErrorType.Unauthorized, "INVALID_TOKEN", "Invalid token"),
                5 => throw new GenericDomainException(ErrorType.Forbidden, "INSUFFICIENT_PERMISSIONS", "Forbidden"),
                6 => throw new GenericDomainException(ErrorType.Internal, "INTERNAL_ERROR", "Internal error"),
                7 => throw new GenericDomainException(ErrorType.ServiceUnavailable, "SERVICE_UNAVAILABLE", "Service unavailable"),
                8 => throw new GenericDomainException(ErrorType.Timeout, "TIMEOUT", "Timeout"),
                9 => throw new SqlDomainException(new SqlErrorInfo(ErrorType.Conflict, "FK_VIOLATION", "Foreign key violation", new InvalidOperationException())),
                10 => throw new SqlDomainException(new SqlErrorInfo(ErrorType.Conflict, "UNIQUE_VIOLATION", "Unique violation", new InvalidOperationException())),
                11 => throw new ArgumentNullException("customerId", "Customer ID cannot be null"),
                12 => throw new InvalidOperationException("Invalid operation"),
                13 => throw new TimeoutException("Timeout"),
                14 => throw new GenericDomainException(ErrorType.RateLimited, "RATE_LIMIT_EXCEEDED", "Rate limited"),
                15 => throw new GenericDomainException(ErrorType.PayloadTooLarge, "PAYLOAD_TOO_LARGE", "Payload too large"),
                _ => Results.Ok(new { success = true, message = "Random success" })
            };
        });

        group.MapGet("/random-result", ([FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            var random = Random.Shared.Next(1, 11);

            logger.LogInformation("Random Result pattern response type: {Type}", random);

            return random switch
            {
                1 => Result<object, DomainError>.Failure(DomainError.Validation("INVALID_EMAIL", "Invalid email")).ToMinimalApiResult(),
                2 => Result<object, DomainError>.Failure(DomainError.NotFound("CUSTOMER_NOT_FOUND", "Not found")).ToMinimalApiResult(),
                3 => Result<object, DomainError>.Failure(DomainError.Conflict("DUPLICATE_EMAIL", "Duplicate")).ToMinimalApiResult(),
                4 => Result<object, DomainError>.Failure(DomainError.Unauthorized("INVALID_TOKEN", "Unauthorized")).ToMinimalApiResult(),
                5 => Result<object, DomainError>.Failure(DomainError.Forbidden("FORBIDDEN", "Forbidden")).ToMinimalApiResult(),
                6 => Result<object, DomainError>.Failure(DomainError.Internal("INTERNAL", "Internal")).ToMinimalApiResult(),
                7 => Result<object, DomainError>.Failure(DomainError.ServiceUnavailable("UNAVAILABLE", "Unavailable")).ToMinimalApiResult(),
                8 => Result<object, DomainError>.Failure(DomainError.Timeout("TIMEOUT", "Timeout")).ToMinimalApiResult(),
                9 => Result<object, DomainError>.Failure(DomainError.External("EXTERNAL", "External")).ToMinimalApiResult(),
                _ => Result<object, DomainError>.Success(new { id = 123, name = "Success" }).ToMinimalApiResult()
            };
        });

        #endregion

        #region Documentation Endpoint

        group.MapGet("/info", () =>
        {
            return Results.Ok(new
            {
                description = "Exception Testing Controller",
                purpose = "Simulate various exception scenarios to test ApiExceptionMiddleware and demonstrate Result pattern",
                approaches = new
                {
                    exceptionThrowing = "Endpoints that throw exceptions - middleware catches and handles",
                    resultPattern = "Endpoints suffixed with '-result' that use Result<T, DomainError> pattern"
                },
                categories = new
                {
                    clientErrors = new[] {
                        new { endpoint = "POST /validation-error", status = 422, code = "INVALID_EMAIL", approach = "Exception" },
                        new { endpoint = "POST /validation-error-result", status = 422, code = "INVALID_EMAIL", approach = "Result" },
                        new { endpoint = "POST /bad-request", status = 400, code = "INVALID_JSON", approach = "Exception" },
                        new { endpoint = "POST /bad-request-result", status = 400, code = "INVALID_JSON", approach = "Result" },
                        new { endpoint = "GET /not-found/{id}", status = 404, code = "CUSTOMER_NOT_FOUND", approach = "Exception" },
                        new { endpoint = "GET /not-found-result/{id}", status = 404, code = "CUSTOMER_NOT_FOUND", approach = "Result" },
                        new { endpoint = "POST /conflict", status = 409, code = "DUPLICATE_EMAIL", approach = "Exception" },
                        new { endpoint = "POST /conflict-result", status = 409, code = "DUPLICATE_EMAIL", approach = "Result" },
                        new { endpoint = "GET /unauthorized", status = 401, code = "INVALID_TOKEN", approach = "Exception" },
                        new { endpoint = "GET /unauthorized-result", status = 401, code = "INVALID_TOKEN", approach = "Result" },
                        new { endpoint = "GET /forbidden", status = 403, code = "INSUFFICIENT_PERMISSIONS", approach = "Exception" },
                        new { endpoint = "GET /forbidden-result", status = 403, code = "INSUFFICIENT_PERMISSIONS", approach = "Result" },
                        new { endpoint = "GET /rate-limited", status = 429, code = "RATE_LIMIT_EXCEEDED", approach = "Exception" },
                        new { endpoint = "GET /rate-limited-result", status = 429, code = "RATE_LIMIT_EXCEEDED", approach = "Result" },
                        new { endpoint = "POST /payload-too-large", status = 413, code = "PAYLOAD_TOO_LARGE", approach = "Exception" }
                    },
                    serverErrors = new[] {
                        new { endpoint = "GET /internal-error", status = 500, code = "INTERNAL_ERROR", approach = "Exception" },
                        new { endpoint = "GET /internal-error-result", status = 500, code = "INTERNAL_ERROR", approach = "Result" },
                        new { endpoint = "GET /service-unavailable", status = 503, code = "SERVICE_UNAVAILABLE", approach = "Exception" },
                        new { endpoint = "GET /service-unavailable-result", status = 503, code = "SERVICE_UNAVAILABLE", approach = "Result" },
                        new { endpoint = "GET /timeout", status = 504, code = "EXTERNAL_SERVICE_TIMEOUT", approach = "Exception" },
                        new { endpoint = "GET /timeout-result", status = 504, code = "EXTERNAL_SERVICE_TIMEOUT", approach = "Result" },
                        new { endpoint = "GET /not-implemented", status = 501, code = "FEATURE_NOT_IMPLEMENTED", approach = "Exception" },
                        new { endpoint = "GET /external-error", status = 502, code = "EXTERNAL_API_ERROR", approach = "Exception" },
                        new { endpoint = "GET /external-error-result", status = 502, code = "EXTERNAL_API_ERROR", approach = "Result" }
                    },
                    sqlErrors = new[] {
                        new { endpoint = "DELETE /sql/foreign-key-violation", status = 409, code = "FK_VIOLATION" },
                        new { endpoint = "POST /sql/unique-violation", status = 409, code = "UNIQUE_VIOLATION" },
                        new { endpoint = "GET /sql/timeout", status = 504, code = "SQL_TIMEOUT" },
                        new { endpoint = "POST /sql/deadlock", status = 409, code = "SQL_DEADLOCK" },
                        new { endpoint = "GET /sql/connection-error", status = 503, code = "SQL_CONNECTION_FAILED" }
                    },
                    standardExceptions = new[] {
                        new { endpoint = "POST /standard/argument-null", status = 500, description = "ArgumentNullException" },
                        new { endpoint = "POST /standard/argument-invalid", status = 500, description = "ArgumentException" },
                        new { endpoint = "POST /standard/invalid-operation", status = 500, description = "InvalidOperationException" },
                        new { endpoint = "GET /standard/timeout", status = 500, description = "TimeoutException" },
                        new { endpoint = "GET /standard/null-reference", status = 500, description = "NullReferenceException" },
                        new { endpoint = "GET /standard/divide-by-zero", status = 500, description = "DivideByZeroException" }
                    },
                    complex = new[] {
                        new { endpoint = "GET /complex/nested", description = "Nested exceptions" },
                        new { endpoint = "GET /complex/aggregate", description = "AggregateException with multiple errors" },
                        new { endpoint = "POST /complex/multiple-validation", description = "Multiple validation errors (Exception)" },
                        new { endpoint = "POST /complex/multiple-validation-result", description = "Multiple validation errors (Result)" },
                        new { endpoint = "POST /complex/business-rule", description = "Business rule violation (Exception)" },
                        new { endpoint = "POST /complex/business-rule-result", description = "Business rule violation (Result)" }
                    },
                    crudPatterns = new[] {
                        new { endpoint = "GET /crud/get/{id}", description = "GET with 200 OK or 204 NoContent" },
                        new { endpoint = "POST /crud/create", description = "POST with 201 Created" },
                        new { endpoint = "PUT /crud/update/{id}", description = "PUT with 200 OK or 204 NoContent" },
                        new { endpoint = "DELETE /crud/delete/{id}", description = "DELETE with 204 NoContent or 404 NotFound" }
                    },
                    success = new[] {
                        new { endpoint = "GET /success", description = "Standard success response" },
                        new { endpoint = "GET /success-result", description = "Success with Result pattern" },
                        new { endpoint = "GET /success-result-message", description = "Success with custom message" },
                        new { endpoint = "GET /success-result-async", description = "Async success with Result pattern" }
                    },
                    utility = new[] {
                        new { endpoint = "GET /random", description = "Random exception type" },
                        new { endpoint = "GET /random-result", description = "Random Result pattern response" },
                        new { endpoint = "GET /info", description = "This endpoint - shows all available tests" }
                    }
                },
                usage = new
                {
                    baseUrl = "/api/ExceptionTest",
                    exampleComparisons = new[] {
                        new {
                            scenario = "Validation Error",
                            exceptionBased = "POST /api/ExceptionTest/validation-error",
                            resultBased = "POST /api/ExceptionTest/validation-error-result",
                            note = "Both produce identical API responses"
                        },
                        new {
                            scenario = "Not Found",
                            exceptionBased = "GET /api/ExceptionTest/not-found/123",
                            resultBased = "GET /api/ExceptionTest/not-found-result/123",
                            note = "Result pattern avoids exception overhead"
                        },
                        new {
                            scenario = "Multiple Errors",
                            exceptionBased = "POST /api/ExceptionTest/complex/multiple-validation",
                            resultBased = "POST /api/ExceptionTest/complex/multiple-validation-result",
                            note = "Result pattern with DomainErrors for multiple issues"
                        }
                    }
                },
                extensionMethods = new
                {
                    basic = new[] {
                        "ToActionResult() - Convert Result to IActionResult",
                        "ToActionResult(message) - With custom success message",
                        "ToActionResultAsync() - Async version"
                    },
                    crud = new[] {
                        "ToGetActionResult() - 200 OK or 204 NoContent",
                        "ToCreatedActionResult(uri) - 201 Created with Location",
                        "ToPutActionResult() - 200 OK or 204 NoContent",
                        "ToDeleteActionResult() - 204 NoContent or 404 NotFound"
                    },
                    errors = new[] {
                        "Result<T, DomainError> - Single error",
                        "Result<T, DomainErrors> - Multiple errors",
                        "DomainError.Validation/NotFound/Conflict/etc. - Factory methods"
                    }
                }
            });
        });

        #endregion
    }
}

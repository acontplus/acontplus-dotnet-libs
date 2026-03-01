namespace Demo.Api.Endpoints.Demo;

public static class BusinessExceptionTestEndpoints
{
    public static void MapBusinessExceptionTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/business-exception-test")
            .WithTags("Business Exception Test");

        #region Direct Service Calls - Result pattern

        group.MapPost("/validation-from-service", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Calling service that may throw validation exception");

            var result = await service.ValidateEmailAsync("invalid-email");
            return result.ToMinimalApiResult("Validation completed");
        });

        group.MapGet("/not-found-from-service/{id:int}", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, int id) =>
        {
            logger.LogInformation("Calling service to get customer ID: {Id}", id);

            var result = await service.GetCustomerAsync(id);
            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        group.MapPost("/conflict-from-service", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, CustomerRequest request) =>
        {
            logger.LogInformation("Calling service to create customer with email: {Email}", request.Email);

            var result = await service.CreateCustomerAsync(request.Email!);
            return result.ToMinimalApiResult("Customer created successfully");
        });

        group.MapPost("/sql-error-from-service", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Calling service to execute database operation");

            var result = await service.ExecuteDatabaseOperationAsync();
            return result.ToMinimalApiResult("Database operation completed");
        });

        group.MapGet("/internal-error-from-service", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Calling service to process complex operation");

            var result = await service.ProcessComplexOperationAsync();
            return result.ToMinimalApiResult("Operation completed");
        });

        #endregion

        #region Wrapped Service Calls - Result pattern responses

        group.MapGet("/wrapped-exception/{id:int}", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, int id) =>
        {
            logger.LogInformation("Calling service with try-catch wrapper");

            var result = await service.GetCustomerAsync(id);
            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        group.MapPost("/wrapped-with-context", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, CustomerRequest request) =>
        {
            logger.LogInformation("Calling service and wrapping exception with context");

            var result = await service.CreateCustomerAsync(request.Email!);
            return result.ToMinimalApiResult("Customer created successfully");
        });

        #endregion

        #region Multiple Layers - Deep Call Stack

        group.MapGet("/deep-call-stack/{id:int}", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger, int id) =>
        {
            logger.LogInformation("Testing deep call stack exception propagation");

            var result = await service.GetCustomerWithDeepStackAsync(id);
            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        #endregion

        #region Async Exception Handling

        group.MapPost("/async-exception", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Testing async exception handling");

            var result = await service.AsyncOperationThatFailsAsync();
            return result.ToMinimalApiResult("Async operation completed");
        });

        group.MapGet("/task-run-exception", async ([FromServices] IBusinessExceptionTestService service, [FromServices] Microsoft.Extensions.Logging.ILogger<object> logger) =>
        {
            logger.LogInformation("Testing exception from Task.Run");

            var result = await service.TaskRunOperationAsync();
            return result.ToMinimalApiResult("Background task completed");
        });

        #endregion

        #region Success Cases - Using ResultApiExtensions

        group.MapGet("/success/{id:int}", async (IBusinessExceptionTestService service, int id) =>
        {
            var result = await service.GetValidCustomerAsync(id);
            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        group.MapGet("/success-with-correlation/{id:int}", async (IBusinessExceptionTestService service, int id, HttpContext context) =>
        {
            var result = await service.GetValidCustomerAsync(id);
            var correlationId = context.TraceIdentifier;
            return result.ToMinimalApiResult("Customer retrieved successfully", correlationId);
        });

        group.MapGet("/success-async/{id:int}", async (IBusinessExceptionTestService service, int id) =>
        {
            var task = service.GetValidCustomerAsync(id);
            return await task.ToMinimalApiResultAsync("Customer retrieved successfully");
        });

        group.MapGet("/crud/get/{id:int}", async (IBusinessExceptionTestService service, int id) =>
        {
            var result = id <= 100
                ? await service.GetValidCustomerAsync(id)
                : Result<CustomerModel, DomainError>.Failure(DomainError.NotFound("CUSTOMER_NOT_FOUND", $"Customer {id} not found"));

            return result.ToGetMinimalApiResult();
        });

        group.MapPost("/crud/create", async (IBusinessExceptionTestService service, CustomerRequest request) =>
        {
            var result = await service.CreateCustomerAsync(request.Email!);
            var locationUri = $"/api/BusinessExceptionTest/crud/get/{result.Match(v => v.Id, _ => 0)}";
            return result.ToCreatedMinimalApiResult(locationUri);
        });

        group.MapPut("/crud/update/{id:int}", async (IBusinessExceptionTestService service, int id, CustomerRequest request) =>
        {
            var result = await service.GetValidCustomerAsync(id);
            // If successful, update values in returned object
            return result.Match<IResult>(
                success: value =>
                {
                    value.Email = request.Email!;
                    value.Name = request.Name!;
                    return Result<CustomerModel, DomainError>.Success(value).ToPutMinimalApiResult();
                },
                failure: error => Result<CustomerModel, DomainError>.Failure(error).ToMinimalApiResult());
        });

        group.MapDelete("/crud/delete/{id:int}", async (int id) =>
        {
            var result = id <= 100
                ? Result<bool, DomainError>.Success(true)
                : Result<bool, DomainError>.Failure(DomainError.NotFound("CUSTOMER_NOT_FOUND", $"Customer {id} not found"));

            return result.ToDeleteMinimalApiResult();
        });

        group.MapPost("/success-with-warnings", async (IBusinessExceptionTestService service, CustomerRequest request) =>
        {
            var result = await service.GetValidCustomerAsync(1);
            return result.Match<IResult>(
                success: value =>
                {
                    var warnings = new List<DomainError>
                    {
                        DomainError.Validation("EMAIL_FORMAT_WARNING", "Email format is valid but non-standard", "email"),
                        DomainError.Validation("NAME_LENGTH_WARNING", "Name is very short", "name")
                    };

                    var domainWarnings = DomainWarnings.Multiple(warnings);
                    var successWithWarnings = new SuccessWithWarnings<CustomerModel>(value, domainWarnings);
                    return Result<SuccessWithWarnings<CustomerModel>, DomainError>.Success(successWithWarnings).ToMinimalApiResult("Customer created with warnings");
                },
                failure: error => Result<CustomerModel, DomainError>.Failure(error).ToMinimalApiResult());
        });

        group.MapGet("/custom-response/{id:int}", async (IBusinessExceptionTestService service, int id, HttpContext context) =>
        {
            var result = await service.GetValidCustomerAsync(id);
            return result.Match<IResult>(
                success: value =>
                {
                    var response = ApiResponse<CustomerModel>.Success(
                        value,
                        new ApiResponseOptions
                        {
                            Message = "Customer retrieved with custom response",
                            CorrelationId = context.TraceIdentifier,
                            StatusCode = System.Net.HttpStatusCode.OK,
                            Metadata = new Dictionary<string, object>
                            {
                                ["requestTime"] = DateTime.UtcNow,
                                ["version"] = "1.0",
                                ["customField"] = "Custom value"
                            }
                        });

                    return Results.Ok(response);
                },
                failure: error => Result<CustomerModel, DomainError>.Failure(error).ToMinimalApiResult());
        });

        #endregion

        #region Comparison: Exception vs Result Pattern

        group.MapGet("/compare/exception/{id:int}", async (IBusinessExceptionTestService service, int id) =>
        {
            var result = await service.GetCustomerAsync(id);
            return result.ToMinimalApiResult("Customer retrieved successfully");
        });

        group.MapGet("/compare/result/{id:int}", async (IBusinessExceptionTestService service, int id) =>
        {
            var result = await service.GetCustomerAsync(id);
            return result.ToMinimalApiResult("Customer found");
        });

        #endregion

        #region Info Endpoint

        group.MapGet("/info", () =>
        {
            return Results.Ok(new
            {
                description = "Business Layer Exception Testing with ResultApiExtensions",
                purpose = "Demonstrate exception handling and Result pattern with standardized API responses",
                testCategories = new
                {
                    exceptionExamples = new[] {
                        new { endpoint = "POST /validation-from-service", description = "Service throws ValidationException" },
                        new { endpoint = "GET /not-found-from-service/{id}", description = "Service throws NotFound DomainException" },
                        new { endpoint = "POST /conflict-from-service", description = "Service throws Conflict DomainException" },
                        new { endpoint = "POST /sql-error-from-service", description = "Service throws SqlDomainException" },
                        new { endpoint = "GET /internal-error-from-service", description = "Service throws Internal DomainException" }
                    },
                    wrappedExceptions = new[] {
                        new { endpoint = "GET /wrapped-exception/{id}", description = "Controller catches and re-throws" },
                        new { endpoint = "POST /wrapped-with-context", description = "Controller wraps exception with context" }
                    },
                    successWithResultPattern = new[] {
                        new { endpoint = "GET /success/{id}", description = "Success with ToActionResult" },
                        new { endpoint = "GET /success-with-correlation/{id}", description = "Success with correlation ID" },
                        new { endpoint = "GET /success-async/{id}", description = "Async success with ToActionResultAsync" },
                        new { endpoint = "POST /success-with-warnings", description = "Success with warnings" },
                        new { endpoint = "GET /custom-response/{id}", description = "Custom ApiResponse" }
                    },
                    crudPatterns = new[] {
                        new { endpoint = "GET /crud/get/{id}", description = "GET: 200 OK or 204 NoContent using ToGetActionResult" },
                        new { endpoint = "POST /crud/create", description = "POST: 201 Created using ToCreatedActionResult" },
                        new { endpoint = "PUT /crud/update/{id}", description = "PUT: 200 OK using ToPutActionResult" },
                        new { endpoint = "DELETE /crud/delete/{id}", description = "DELETE: 204 NoContent or 404 NotFound using ToDeleteActionResult" }
                    },
                    comparison = new[] {
                        new { endpoint = "GET /compare/exception/{id}", description = "Exception approach (throws)" },
                        new { endpoint = "GET /compare/result/{id}", description = "Result pattern approach (catches and converts)" }
                    },
                    deepCallStack = new[] {
                        new { endpoint = "GET /deep-call-stack/{id}", description = "Exception from deep service layer" }
                    },
                    asyncHandling = new[] {
                        new { endpoint = "POST /async-exception", description = "Async method exception" },
                        new { endpoint = "GET /task-run-exception", description = "Task.Run exception" }
                    }
                },
                resultApiExtensionMethods = new
                {
                    basic = new[] {
                        "ToActionResult() - Basic conversion with default message",
                        "ToActionResult(message) - With custom success message",
                        "ToActionResult(message, correlationId) - With message and correlation ID",
                        "ToActionResultAsync() - Async version"
                    },
                    crud = new[] {
                        "ToGetActionResult() - 200 OK with data or 204 NoContent if null",
                        "ToCreatedActionResult(locationUri) - 201 Created with Location header",
                        "ToPutActionResult() - 200 OK with data or 204 NoContent",
                        "ToDeleteActionResult() - 204 NoContent on success or 404 NotFound"
                    },
                    advanced = new[] {
                        "SuccessWithWarnings - Include warnings in successful response",
                        "ApiResponse.Success() - Full control over response structure",
                        "Custom metadata - Add custom fields to response"
                    }
                },
                responseFormats = new
                {
                    success = new
                    {
                        format = "ApiResponse<T>",
                        example = new
                        {
                            success = true,
                            code = "200",
                            message = "Customer retrieved successfully",
                            data = new { id = 1, name = "John Doe", email = "john@example.com", status = "Active" },
                            correlationId = "abc-123-def",
                            timestamp = "2024-01-15T10:30:00Z"
                        }
                    },
                    error = new
                    {
                        format = "ApiResponse",
                        example = new
                        {
                            success = false,
                            code = "404",
                            message = "Customer not found",
                            errors = new[] {
                                new {
                                    code = "CUSTOMER_NOT_FOUND",
                                    message = "Customer not found",
                                    category = "business",
                                    severity = "warning"
                                }
                            },
                            correlationId = "abc-123-def",
                            timestamp = "2024-01-15T10:30:00Z"
                        }
                    },
                    successWithWarnings = new
                    {
                        format = "ApiResponse<T> with warnings",
                        example = new
                        {
                            success = true,
                            code = "200",
                            message = "Customer created with warnings",
                            data = new { id = 1, name = "J", email = "j@example.com" },
                            warnings = new[] {
                                new {
                                    code = "NAME_LENGTH_WARNING",
                                    message = "Name is very short",
                                    target = "name",
                                    category = "validation",
                                    severity = "warning"
                                }
                            },
                            correlationId = "abc-123-def"
                        }
                    }
                },
                notes = new[] {
                    "All exceptions thrown from services are caught by ApiExceptionMiddleware",
                    "ResultApiExtensions provide consistent response format for success cases",
                    "CRUD methods (ToGetActionResult, ToCreatedActionResult, etc.) handle REST semantics automatically",
                    "DomainException types preserve their ErrorType and ErrorCode",
                    "Both exception throwing and Result pattern produce identical response formats",
                    "Use exception throwing for simpler code, Result pattern for expected failures"
                }
            });
        });

        #endregion
    }
}

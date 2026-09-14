namespace Acontplus.Services.Middleware;

/// <summary>
/// Middleware for centralized API exception handling, logging, and standardized error responses.
/// </summary>
public class ApiExceptionMiddleware
{
    private const string CategoryValidation = "validation";
    private const string SeverityWarning = "warning";
    private const string SeverityError = "error";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly ExceptionHandlingOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next request delegate in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">Exception handling options.</param>
    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, ExceptionHandlingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = options.IncludeDebugDetailsInResponse
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    /// <summary>
    /// Handles the HTTP request and catches exceptions, returning standardized error responses.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        var tenantId = GetTenantId(context);

        context.Items[ApiMetadataKeys.CorrelationId] = correlationId;
        context.Items[ApiMetadataKeys.TenantId] = tenantId;

        try
        {
            await _next(context);

            if (!context.Response.HasStarted && context.Response.StatusCode >= 400)
            {
                await HandleStatusCodeAsync(context, correlationId, tenantId);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, correlationId, tenantId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, string correlationId, string tenantId)
    {
        await LogException(ex, correlationId, tenantId, context);

        context.Response.ContentType = "application/json";

        var response = ex switch
        {
            ValidationException validationEx => HandleValidationException(validationEx, correlationId, tenantId),
            DomainException domainEx => HandleDomainException(domainEx, correlationId, tenantId),
            ApiException apiEx => HandleApiException(apiEx, correlationId, tenantId),
            _ => HandleUnhandledException(ex, correlationId, tenantId)
        };

        context.Response.StatusCode = int.Parse(response.Code);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions), context.RequestAborted);
    }

    private static ApiResponse HandleValidationException(ValidationException ex, string correlationId, string tenantId)
    {
        var errors = ex.Errors
            .SelectMany(e => e.Value.Select(message =>
                new ApiError(
                    Code: ex.ErrorCode,
                    Message: message,
                    Target: e.Key,
                    Category: CategoryValidation,
                    Severity: SeverityWarning)))
            .ToList();

        return ApiResponse.Failure(
            errors, new ApiResponseOptions
            {
                Message = ex.Message,
                CorrelationId = correlationId,
                StatusCode = ex.StatusCode,
                Metadata = CreateStandardMetadata(tenantId)
            });
    }

    private static ApiResponse HandleDomainException(DomainException ex, string correlationId, string tenantId)
    {
        var httpStatusCode = ex.ErrorType.ToHttpStatusCode();

        var error = new ApiError(
            Code: ex.ErrorCode,
            Message: ex.Message,
            Category: ex.ErrorType.ToCategoryString(),
            Severity: ex.ErrorType.ToSeverityString(),
            TraceId: Activity.Current?.Id);

        return ApiResponse.Failure(
            error, new ApiResponseOptions
            {
                Message = ex.Message,
                CorrelationId = correlationId,
                StatusCode = httpStatusCode,
                Metadata = CreateStandardMetadata(tenantId)
            });
    }

    private static ApiResponse HandleApiException(ApiException ex, string correlationId, string tenantId)
    {
        var error = new ApiError(
            Code: ex.ErrorCode,
            Message: ex.Message,
            Category: GetErrorCategory(ex.StatusCode),
            Severity: GetErrorSeverity(ex.StatusCode),
            TraceId: Activity.Current?.Id);

        return ApiResponse.Failure(
            error, new ApiResponseOptions
            {
                Message = ex.Message,
                CorrelationId = correlationId,
                StatusCode = ex.StatusCode,
                Metadata = CreateStandardMetadata(tenantId)
            });
    }

    private ApiResponse HandleUnhandledException(Exception ex, string correlationId, string tenantId)
    {
        // Check if this is actually a DomainException that wasn't caught earlier
        // This can happen with inheritance hierarchies or wrapper exceptions
        var innerDomainEx = FindInnerException<DomainException>(ex);
        if (innerDomainEx != null)
        {
            _logger.LogWarning("DomainException found as inner exception in unhandled path: {ErrorCode}",
                innerDomainEx.ErrorCode);
            return HandleDomainException(innerDomainEx, correlationId, tenantId);
        }

        var innerValidationEx = FindInnerException<ValidationException>(ex);
        if (innerValidationEx != null)
        {
            _logger.LogWarning("ValidationException found as inner exception in unhandled path");
            return HandleValidationException(innerValidationEx, correlationId, tenantId);
        }

        var innerApiEx = FindInnerException<ApiException>(ex);
        if (innerApiEx != null)
        {
            _logger.LogWarning("ApiException found as inner exception in unhandled path: {ErrorCode}",
                innerApiEx.ErrorCode);
            return HandleApiException(innerApiEx, correlationId, tenantId);
        }

        // Truly unhandled exception
        var debugInfo = _options.IncludeDebugDetailsInResponse
            ? GetSafeDebugInfo(ex)
            : null;

        var error = new ApiError(
            Code: "UNHANDLED_ERROR",
            Message: _options.IncludeDebugDetailsInResponse
                ? ex.Message
                : "An unexpected error occurred",
            Category: "system",
            Severity: "error",
            Details: debugInfo != null
                ? new Dictionary<string, object>
                {
                    [DebugMetadataKeys.Debug] = debugInfo
                }
                : null,
            TraceId: Activity.Current?.TraceId.ToString());

        return ApiResponse.Failure(
            error, new ApiResponseOptions
            {
                Message = "An unexpected error occurred",
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.InternalServerError,
                Metadata = CreateStandardMetadata(tenantId)
            });
    }

    /// <summary>
    /// Searches the exception chain for a specific exception type.
    /// </summary>
    private static T? FindInnerException<T>(Exception ex) where T : Exception
    {
        var current = ex;
        while (current != null)
        {
            if (current is T typedException)
            {
                return typedException;
            }
            current = current.InnerException;
        }
        return null;
    }

    private async Task HandleStatusCodeAsync(HttpContext context, string correlationId, string tenantId)
    {
        var statusCode = (HttpStatusCode)context.Response.StatusCode;

        var error = new ApiError(
            Code: GetErrorCodeForStatus(statusCode),
            Message: GetStatusMessage(statusCode),
            Category: GetErrorCategory(statusCode),
            Severity: GetErrorSeverity(statusCode),
            TraceId: Activity.Current?.Id);

        var response = ApiResponse.Failure(
            error, new ApiResponseOptions
            {
                Message = GetStatusMessage(statusCode),
                CorrelationId = correlationId,
                StatusCode = statusCode,
                Metadata = CreateStandardMetadata(tenantId)
            });

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions), context.RequestAborted);
    }

    private async Task LogException(Exception ex, string correlationId, string tenantId, HttpContext context)
    {
        var logMessage = new StringBuilder()
            .AppendLine($"CorrelationId: {correlationId}")
            .AppendLine($"TenantId: {tenantId}");

        if (ex is DomainException domainEx)
        {
            logMessage.AppendLine($"ErrorType: {domainEx.ErrorType}")
                .AppendLine($"ErrorCode: {domainEx.ErrorCode}");
        }
        else if (ex is ApiException apiEx)
        {
            logMessage.AppendLine($"StatusCode: {apiEx.StatusCode}")
                .AppendLine($"ErrorCode: {apiEx.ErrorCode}");
        }

        if (_options.IncludeRequestDetails)
        {
            logMessage.AppendLine($"Path: {context.Request.Path}")
                .AppendLine($"Method: {context.Request.Method}");
        }

        if (_options.LogRequestBody && context.Request.Body.CanRead)
        {
            logMessage.AppendLine($"Request Body: {await ReadRequestBodyAsync(context.Request)}");
        }

        var logLevel = GetLogLevel(ex);
        if (_logger.IsEnabled(logLevel))
        {
            _logger.Log(logLevel, ex, "{LogDetails}", logMessage.ToString());
        }
    }

    private static LogLevel GetLogLevel(Exception ex)
    {
        return ex switch
        {
            ValidationException => LogLevel.Warning,
            DomainException domainEx => domainEx.ErrorType switch
            {
                ErrorType.Validation => LogLevel.Warning,
                ErrorType.BadRequest => LogLevel.Warning,
                ErrorType.NotFound => LogLevel.Information,
                ErrorType.Conflict => LogLevel.Warning,
                ErrorType.Unauthorized => LogLevel.Warning,
                ErrorType.Forbidden => LogLevel.Warning,
                _ => LogLevel.Error
            },
            ApiException apiEx => (int)apiEx.StatusCode >= 500
                ? LogLevel.Error
                : LogLevel.Warning,
            _ => LogLevel.Error
        };
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }
        catch
        {
            return "<Unable to read body>";
        }
    }

    private static string GetErrorCodeForStatus(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "BAD_REQUEST",
        HttpStatusCode.Unauthorized => "UNAUTHORIZED",
        HttpStatusCode.Forbidden => "FORBIDDEN",
        HttpStatusCode.NotFound => "NOT_FOUND",
        HttpStatusCode.Conflict => "CONFLICT",
        HttpStatusCode.MethodNotAllowed => "METHOD_NOT_ALLOWED",
        HttpStatusCode.UnprocessableEntity => "VALIDATION_ERROR",
        HttpStatusCode.TooManyRequests => "RATE_LIMITED",
        HttpStatusCode.InternalServerError => "INTERNAL_ERROR",
        HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
        HttpStatusCode.GatewayTimeout => "TIMEOUT",
        _ => statusCode.ToString().ToUpperInvariant().Replace(" ", "_")
    };

    private static string GetStatusMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Invalid request",
        HttpStatusCode.Unauthorized => "Authentication required",
        HttpStatusCode.Forbidden => "Access denied",
        HttpStatusCode.NotFound => "Resource not found",
        HttpStatusCode.Conflict => "Conflict occurred",
        HttpStatusCode.MethodNotAllowed => "Method not allowed",
        HttpStatusCode.UnprocessableEntity => "Validation failed",
        HttpStatusCode.TooManyRequests => "Too many requests",
        HttpStatusCode.InternalServerError => "Internal server error",
        HttpStatusCode.ServiceUnavailable => "Service unavailable",
        HttpStatusCode.GatewayTimeout => "Request timeout",
        _ => statusCode.ToString()
    };

    private static string GetErrorCategory(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => CategoryValidation,
        HttpStatusCode.UnprocessableEntity => CategoryValidation,
        HttpStatusCode.Unauthorized => "authentication",
        HttpStatusCode.Forbidden => "authorization",
        HttpStatusCode.NotFound => "not_found",
        HttpStatusCode.Conflict => "conflict",
        HttpStatusCode.MethodNotAllowed => CategoryValidation,
        HttpStatusCode.TooManyRequests => "performance",
        HttpStatusCode.RequestTimeout => "performance",
        HttpStatusCode.GatewayTimeout => "performance",
        _ => (int)statusCode >= 500 ? "server" : "client"
    };

    private static string GetErrorSeverity(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => SeverityWarning,
        HttpStatusCode.UnprocessableEntity => SeverityWarning,
        HttpStatusCode.NotFound => SeverityWarning,
        HttpStatusCode.Conflict => SeverityWarning,
        HttpStatusCode.MethodNotAllowed => SeverityWarning,
        _ => (int)statusCode >= 500 ? SeverityError : SeverityWarning
    };

    private static Dictionary<string, object>? GetSafeDebugInfo(Exception ex)
    {
        if (!ShouldIncludeDebugInfo())
        {
            return null;
        }

        object? innerExceptionInfo = ex.InnerException != null
            ? new
            {
                type = ex.InnerException.GetType().Name,
                message = ex.InnerException.Message
            }
            : null;

        return new Dictionary<string, object>
        {
            [DebugMetadataKeys.ExceptionType] = ex.GetType().Name,
            [DebugMetadataKeys.Message] = ex.Message,
            [DebugMetadataKeys.StackTrace] = ex.StackTrace?.Split(Environment.NewLine) ?? Array.Empty<string>(),
            [DebugMetadataKeys.InnerException] = innerExceptionInfo!,
            [DebugMetadataKeys.ActivityId] = Activity.Current?.Id ?? "none"
        };
    }

    private static bool ShouldIncludeDebugInfo()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
               Debugger.IsAttached;
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return context.Request.Headers.TryGetValue("Correlation-Id", out var headerValue) &&
               Guid.TryParse(headerValue, out var parsed)
            ? parsed.ToString()
            : context.TraceIdentifier;
    }

    private static string GetTenantId(HttpContext context)
    {
        return context.Request.Headers.TryGetValue("Tenant-Id", out var headerValue)
            ? headerValue.ToString()
            : "UNKNOWN";
    }

    private static Dictionary<string, object> CreateStandardMetadata(string tenantId)
    {
        return new Dictionary<string, object>
        {
            [ApiMetadataKeys.TenantId] = tenantId,
            [ApiMetadataKeys.TimestampUtc] = DateTime.UtcNow
        };
    }
}

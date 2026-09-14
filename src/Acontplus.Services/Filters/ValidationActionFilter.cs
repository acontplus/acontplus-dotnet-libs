using Microsoft.AspNetCore.Mvc;

namespace Acontplus.Services.Filters;

/// <summary>
/// Action filter for automatic model validation with standardized error responses.
/// </summary>
public class ValidationActionFilter : IActionFilter
{
    private readonly ILogger<ValidationActionFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationActionFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ValidationActionFilter(ILogger<ValidationActionFilter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new ApiError(
                    Code: "VALIDATION_ERROR",
                    Message: e.ErrorMessage,
                    Target: x.Key,
                    Category: "validation")))
                .ToList();

            var correlationId = context.HttpContext.TraceIdentifier;
            var response = ApiResponse.Failure(
                errors, new ApiResponseOptions
                {
                    Message = "Validation failed",
                    CorrelationId = correlationId,
                    StatusCode = HttpStatusCode.BadRequest,
                    Metadata = new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["path"] = context.HttpContext.Request.Path.Value ?? string.Empty
                    }
                });

            _logger.LogWarning("Model validation failed for {Path} with {ErrorCount} errors",
                context.HttpContext.Request.Path, errors.Count);

            context.Result = new BadRequestObjectResult(response);
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}
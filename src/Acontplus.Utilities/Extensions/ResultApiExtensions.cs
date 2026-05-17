using System.Collections.Immutable;
using Acontplus.Core.Domain.Enums;

namespace Acontplus.Utilities.Extensions;

public static class ResultApiExtensions
{
    #region Configuration

    /// <summary>
    /// Allows configuring the ApiResponse before it's returned in success cases.
    /// This is useful for adding global metadata, logging, or other cross-cutting concerns.
    /// </summary>
    private static Action<ApiResponse>? ConfigureResponse { get; set; }

    /// <summary>
    /// Configures a global action to modify ApiResponse instances before they are returned.
    /// </summary>
    /// <param name="configureAction">The action to apply to each ApiResponse.</param>
    public static void ConfigureApiResponses(Action<ApiResponse> configureAction)
    {
        ConfigureResponse = configureAction;
    }

    #endregion

    #region Action Results (Controller-style)

    public static IActionResult ToActionResult<TValue>(
        this Result<TValue, DomainError> result,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            value => CreateSuccessResponse(value, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    // New overload allowing explicit success message
    public static IActionResult ToActionResult<TValue>(
        this Result<TValue, DomainError> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            value => CreateSuccessResponse(value, successMessage, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    public static IActionResult ToActionResult<TValue>(
        this Result<TValue, DomainErrors> result,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            value => CreateSuccessResponse(value, correlationId),
            errors => errors.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    // New overload allowing explicit success message for DomainErrors
    public static IActionResult ToActionResult<TValue>(
        this Result<TValue, DomainErrors> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            value => CreateSuccessResponse(value, successMessage, correlationId),
            errors => errors.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    public static IActionResult ToActionResult<TValue>(
        this Result<SuccessWithWarnings<TValue>, DomainError> result,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            successWithWarnings => successWithWarnings.ToActionResult(correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    // New overload to pass explicit success message for SuccessWithWarnings
    public static IActionResult ToActionResult<TValue>(
        this Result<SuccessWithWarnings<TValue>, DomainError> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            successWithWarnings => successWithWarnings.ToActionResult(successMessage, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToActionResult()
        );
    }

    #endregion

    #region Async Action Results

    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<TValue, DomainError>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(correlationId);
    }

    // Async overload with successMessage
    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<TValue, DomainError>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(successMessage, correlationId);
    }

    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<TValue, DomainErrors>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(correlationId);
    }

    // Async overload with successMessage for DomainErrors
    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<TValue, DomainErrors>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(successMessage, correlationId);
    }

    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<SuccessWithWarnings<TValue>, DomainError>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(correlationId);
    }

    // Async overload with successMessage for SuccessWithWarnings
    public static async Task<IActionResult> ToActionResultAsync<TValue>(
        this Task<Result<SuccessWithWarnings<TValue>, DomainError>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToActionResult(successMessage, correlationId);
    }

    #endregion

    #region Minimal API Results (IResult)

    public static IResult ToMinimalApiResult<TValue>(
        this Result<TValue, DomainError> result,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateSuccessResult(value, result.SuccessMessage ?? string.Empty, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    // New overload allowing explicit success message
    public static IResult ToMinimalApiResult<TValue>(
        this Result<TValue, DomainError> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateSuccessResult(value, successMessage, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    public static IResult ToMinimalApiResult<TValue>(
        this Result<TValue, DomainErrors> result,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateSuccessResult(value, correlationId),
            errors => errors.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    public static IResult ToMinimalApiResult<TValue>(
        this Result<TValue, DomainErrors> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateSuccessResult(value, successMessage, correlationId),
            errors => errors.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    public static IResult ToMinimalApiResult<TValue>(
        this Result<SuccessWithWarnings<TValue>, DomainError> result,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            successWithWarnings => successWithWarnings.ToMinimalApiResult(correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    // New overload to pass explicit success message for SuccessWithWarnings
    public static IResult ToMinimalApiResult<TValue>(
        this Result<SuccessWithWarnings<TValue>, DomainError> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            successWithWarnings => successWithWarnings.ToMinimalApiResult(successMessage, correlationId),
            error => error.ToApiResponse<TValue>(correlationId).ToMinimalApiResult()
        );
    }

    #endregion

    #region Async Minimal API Results

    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<TValue, DomainError>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(correlationId);
    }

    // Async overload with explicit success message
    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<TValue, DomainError>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(successMessage, correlationId);
    }

    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<TValue, DomainErrors>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(correlationId);
    }

    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<SuccessWithWarnings<TValue>, DomainError>> resultTask,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(correlationId);
    }

    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<TValue, DomainErrors>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(successMessage, correlationId);
    }

    // Async overload for SuccessWithWarnings with explicit message
    public static async Task<IResult> ToMinimalApiResultAsync<TValue>(
        this Task<Result<SuccessWithWarnings<TValue>, DomainError>> resultTask,
        string successMessage,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToMinimalApiResult(successMessage, correlationId);
    }

    #endregion

    #region SuccessWithWarnings Extensions

    public static IActionResult ToActionResult<TValue>(
        this SuccessWithWarnings<TValue> successWithWarnings,
        string? correlationId = null)
    {
        return successWithWarnings.Warnings.HasWarnings
            ? CreateWarningResponse(
                successWithWarnings.Value,
                successWithWarnings.Warnings,
                correlationId)
            : CreateSuccessResponse(successWithWarnings.Value, correlationId);
    }

    // New overload to accept success message
    public static IActionResult ToActionResult<TValue>(
        this SuccessWithWarnings<TValue> successWithWarnings,
        string successMessage,
        string? correlationId = null)
    {
        return successWithWarnings.Warnings.HasWarnings
            ? CreateWarningResponse(
                successWithWarnings.Value,
                successWithWarnings.Warnings,
                correlationId) // warnings don't carry message currently
            : CreateSuccessResponse(successWithWarnings.Value, successMessage, correlationId);
    }

    public static IResult ToMinimalApiResult<TValue>(
        this SuccessWithWarnings<TValue> successWithWarnings,
        string? correlationId = null)
    {
        return successWithWarnings.Warnings.HasWarnings
            ? CreateWarningResult(
                successWithWarnings.Value,
                successWithWarnings.Warnings,
                correlationId)
            : CreateSuccessResult(successWithWarnings.Value, correlationId);
    }

    // New overload to accept success message for minimal API
    public static IResult ToMinimalApiResult<TValue>(
        this SuccessWithWarnings<TValue> successWithWarnings,
        string successMessage,
        string? correlationId = null)
    {
        return successWithWarnings.Warnings.HasWarnings
            ? CreateWarningResult(
                successWithWarnings.Value,
                successWithWarnings.Warnings,
                correlationId) // warnings don't carry message currently
            : CreateSuccessResult(successWithWarnings.Value, successMessage, correlationId);
    }

    #endregion

    #region CRUD-Specific Extensions (Synchronous)

    // GET: 200 OK or 204 NoContent
    public static IActionResult ToGetActionResult<T>(this Result<T, DomainError> result)
    {
        return result.Match<IActionResult>(
            value => CreateGetActionResult(value),
            error => error.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // POST: 201 Created (with Location header)
    public static IActionResult ToCreatedActionResult<T>(this Result<T, DomainError> result, string locationUri)
    {
        return result.Match<IActionResult>(
            value => CreateCreatedActionResult(value, locationUri),
            error => error.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // PUT: 200 OK or 204 NoContent
    public static IActionResult ToPutActionResult<T>(this Result<T, DomainError> result)
    {
        return result.Match<IActionResult>(
            value => CreatePutActionResult(value),
            error => error.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // DELETE: 204 NoContent or 404 NotFound
    public static IActionResult ToDeleteActionResult(this Result<bool, DomainError> result)
    {
        return result.Match<IActionResult>(
            deleted => CreateDeleteActionResult(deleted),
            error => error.ToApiResponse<bool>(null, null).ToActionResult()
        );
    }

    // Minimal API: GET: 200 OK or 204 NoContent
    public static IResult ToGetMinimalApiResult<T>(this Result<T, DomainError> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateGetMinimalApiResult(value),
            error => error.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: POST: 201 Created (with Location header)
    public static IResult ToCreatedMinimalApiResult<T>(this Result<T, DomainError> result, string locationUri, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateCreatedMinimalApiResult(value, locationUri),
            error => error.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: PUT: 200 OK or 204 NoContent
    public static IResult ToPutMinimalApiResult<T>(this Result<T, DomainError> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreatePutMinimalApiResult(value),
            error => error.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: DELETE: 204 NoContent or 404 NotFound
    public static IResult ToDeleteMinimalApiResult(this Result<bool, DomainError> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            deleted => CreateDeleteMinimalApiResult(deleted),
            error => error.ToApiResponse<bool>(correlationId).ToMinimalApiResult()
        );
    }

    #endregion

    #region CRUD-Specific Extensions for DomainErrors (Synchronous)

    // GET: 200 OK or 204 NoContent
    public static IActionResult ToGetActionResult<T>(this Result<T, DomainErrors> result)
    {
        return result.Match<IActionResult>(
            value => CreateGetActionResult(value),
            errors => errors.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // POST: 201 Created (with Location header)
    public static IActionResult ToCreatedActionResult<T>(this Result<T, DomainErrors> result, string locationUri)
    {
        return result.Match<IActionResult>(
            value => CreateCreatedActionResult(value, locationUri),
            errors => errors.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // PUT: 200 OK or 204 NoContent
    public static IActionResult ToPutActionResult<T>(this Result<T, DomainErrors> result)
    {
        return result.Match<IActionResult>(
            value => CreatePutActionResult(value),
            errors => errors.ToApiResponse<T>(null, null).ToActionResult()
        );
    }

    // DELETE: 204 NoContent or 404 NotFound
    public static IActionResult ToDeleteActionResult(this Result<bool, DomainErrors> result)
    {
        return result.Match<IActionResult>(
            deleted => CreateDeleteActionResult(deleted),
            errors => errors.ToApiResponse<bool>(null, null).ToActionResult()
        );
    }

    // Minimal API: GET: 200 OK or 204 NoContent
    public static IResult ToGetMinimalApiResult<T>(this Result<T, DomainErrors> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateGetMinimalApiResult(value),
            errors => errors.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: POST: 201 Created (with Location header)
    public static IResult ToCreatedMinimalApiResult<T>(this Result<T, DomainErrors> result, string locationUri, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreateCreatedMinimalApiResult(value, locationUri),
            errors => errors.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: PUT: 200 OK or 204 NoContent
    public static IResult ToPutMinimalApiResult<T>(this Result<T, DomainErrors> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            value => CreatePutMinimalApiResult(value),
            errors => errors.ToApiResponse<T>(correlationId).ToMinimalApiResult()
        );
    }

    // Minimal API: DELETE: 204 NoContent or 404 NotFound
    public static IResult ToDeleteMinimalApiResult(this Result<bool, DomainErrors> result, string? correlationId = null)
    {
        return result.Match<IResult>(
            deleted => CreateDeleteMinimalApiResult(deleted),
            errors => errors.ToApiResponse<bool>(correlationId).ToMinimalApiResult()
        );
    }

    #endregion

    #region CRUD-Specific Extensions (Async)

    // GET: 200 OK or 204 NoContent (async)
    public static async Task<IActionResult> ToGetActionResultAsync<T>(this Task<Result<T, DomainError>> resultTask)
    {
        var result = await resultTask;
        return result.ToGetActionResult();
    }

    // POST: 201 Created (async)
    public static async Task<IActionResult> ToCreatedActionResultAsync<T>(this Task<Result<T, DomainError>> resultTask, string locationUri)
    {
        var result = await resultTask;
        return result.ToCreatedActionResult(locationUri);
    }

    // PUT: 200 OK or 204 NoContent (async)
    public static async Task<IActionResult> ToPutActionResultAsync<T>(this Task<Result<T, DomainError>> resultTask)
    {
        var result = await resultTask;
        return result.ToPutActionResult();
    }

    // DELETE: 204 NoContent or 404 NotFound (async)
    public static async Task<IActionResult> ToDeleteActionResultAsync(this Task<Result<bool, DomainError>> resultTask)
    {
        var result = await resultTask;
        return result.ToDeleteActionResult();
    }

    // Minimal API: GET: 200 OK or 204 NoContent (async)
    public static async Task<IResult> ToGetMinimalApiResultAsync<T>(this Task<Result<T, DomainError>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToGetMinimalApiResult(correlationId);
    }

    // Minimal API: POST: 201 Created (async)
    public static async Task<IResult> ToCreatedMinimalApiResultAsync<T>(this Task<Result<T, DomainError>> resultTask, string locationUri, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToCreatedMinimalApiResult(locationUri, correlationId);
    }

    // Minimal API: PUT: 200 OK or 204 NoContent (async)
    public static async Task<IResult> ToPutMinimalApiResultAsync<T>(this Task<Result<T, DomainError>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPutMinimalApiResult(correlationId);
    }

    // Minimal API: DELETE: 204 NoContent or 404 NotFound (async)
    public static async Task<IResult> ToDeleteMinimalApiResultAsync(this Task<Result<bool, DomainError>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToDeleteMinimalApiResult(correlationId);
    }

    #endregion

    #region CRUD-Specific Extensions for DomainErrors (Async)

    // GET: 200 OK or 204 NoContent (async)
    public static async Task<IActionResult> ToGetActionResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask)
    {
        var result = await resultTask;
        return result.ToGetActionResult();
    }

    // POST: 201 Created (async)
    public static async Task<IActionResult> ToCreatedActionResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask, string locationUri)
    {
        var result = await resultTask;
        return result.ToCreatedActionResult(locationUri);
    }

    // PUT: 200 OK or 204 NoContent (async)
    public static async Task<IActionResult> ToPutActionResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask)
    {
        var result = await resultTask;
        return result.ToPutActionResult();
    }

    // DELETE: 204 NoContent or 404 NotFound (async)
    public static async Task<IActionResult> ToDeleteActionResultAsync(this Task<Result<bool, DomainErrors>> resultTask)
    {
        var result = await resultTask;
        return result.ToDeleteActionResult();
    }

    // Minimal API: GET: 200 OK or 204 NoContent (async)
    public static async Task<IResult> ToGetMinimalApiResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToGetMinimalApiResult(correlationId);
    }

    // Minimal API: POST: 201 Created (async)
    public static async Task<IResult> ToCreatedMinimalApiResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask, string locationUri, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToCreatedMinimalApiResult(locationUri, correlationId);
    }

    // Minimal API: PUT: 200 OK or 204 NoContent (async)
    public static async Task<IResult> ToPutMinimalApiResultAsync<T>(this Task<Result<T, DomainErrors>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPutMinimalApiResult(correlationId);
    }

    // Minimal API: DELETE: 204 NoContent or 404 NotFound (async)
    public static async Task<IResult> ToDeleteMinimalApiResultAsync(this Task<Result<bool, DomainErrors>> resultTask, string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToDeleteMinimalApiResult(correlationId);
    }

    #endregion

    #region Domain Error Extensions

    private static readonly ImmutableDictionary<ErrorType, int> ErrorSeverity =
        new Dictionary<ErrorType, int>
        {
            // Server Errors (5xx) - highest severity
            [ErrorType.Internal] = 100,
            [ErrorType.External] = 95,
            [ErrorType.ServiceUnavailable] = 90,
            [ErrorType.Timeout] = 85,
            [ErrorType.NotImplemented] = 80,
            [ErrorType.HttpVersionNotSupported] = 75,
            [ErrorType.InsufficientStorage] = 70,
            [ErrorType.LoopDetected] = 65,
            [ErrorType.NotExtended] = 60,
            [ErrorType.NetworkAuthRequired] = 55,

            // Client Errors (4xx) - lower severity
            [ErrorType.RequestTimeout] = 50,
            [ErrorType.UnavailableForLegal] = 45,
            [ErrorType.Forbidden] = 40,
            [ErrorType.Unauthorized] = 35,
            [ErrorType.RateLimited] = 30,
            [ErrorType.Conflict] = 25,
            [ErrorType.NotFound] = 20,
            [ErrorType.Validation] = 15,
            [ErrorType.BadRequest] = 10,
            [ErrorType.MethodNotAllowed] = 9,
            [ErrorType.NotAcceptable] = 8,
            [ErrorType.PayloadTooLarge] = 7,
            [ErrorType.UriTooLong] = 6,
            [ErrorType.UnsupportedMediaType] = 5,
            [ErrorType.RangeNotSatisfiable] = 4,
            [ErrorType.ExpectationFailed] = 3,
            [ErrorType.PreconditionFailed] = 2,
            [ErrorType.PreconditionRequired] = 1,
            [ErrorType.RequestHeadersTooLarge] = 0
        }.ToImmutableDictionary();

    private static ApiResponse<T> CreateApiResponse<T>(
        IReadOnlyList<DomainError>? errors = null,
        IReadOnlyList<DomainError>? warnings = null,
        T? data = default,
        string? message = null,
        string? correlationId = null)
    {
        var options = new ApiResponseOptions
        {
            Message = message ?? errors?.FirstOrDefault().Message,
            Errors = errors?.ToApiErrors(),
            Warnings = warnings?.ToApiErrors(),
            CorrelationId = correlationId,
            StatusCode = errors?.GetMostSevereError().GetHttpStatusCode() ?? HttpStatusCode.OK
        };

        return errors == null || !errors.Any()
            ? ApiResponse<T>.Success(data!, options)
            : ApiResponse<T>.Failure(errors.ToApiErrors(), options);
    }

    // ================ Single Error Methods ================
    public static ApiResponse<T> ToApiResponse<T>(this DomainError error, string? correlationId = null)
        => CreateApiResponse<T>(errors: new[] { error }, correlationId: correlationId);

    public static ApiResponse<T> ToApiResponse<T>(
        this DomainError error,
        string? correlationId = null,
        DomainWarnings? warnings = null)
        => CreateApiResponse<T>(
            errors: new[] { error },
            warnings: warnings?.Warnings,
            correlationId: correlationId);

    // ================ Collection Methods ================
    public static ApiResponse<T> ToApiResponse<T>(
        this DomainErrors errors,
        string? correlationId = null,
        DomainWarnings? warnings = null)
        => CreateApiResponse<T>(
            errors: errors.Errors,
            warnings: warnings?.Warnings,
            message: errors.GetAggregateErrorMessage(),
            correlationId: correlationId);

    public static ApiResponse<T> ToApiResponse<T>(
        this IEnumerable<DomainError> errors,
        string? correlationId = null,
        DomainWarnings? warnings = null)
        => CreateApiResponse<T>(
            errors: errors.ToList(),
            warnings: warnings?.Warnings,
            correlationId: correlationId);

    // ================ Result<T> Conversions ================
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result, string? correlationId = null)
        => result.Match(
            success: data => CreateApiResponse(data: data, correlationId: correlationId),
            failure: error => error.ToApiResponse<T>(correlationId));

    public static ApiResponse<T> ToApiResponse<T>(
        this Result<T> result,
        string successMessage,
        string? correlationId = null)
        => result.Match(
            success: data => CreateApiResponse(
                data: data,
                message: successMessage,
                correlationId: correlationId),
            failure: error => error.ToApiResponse<T>(correlationId));

    // ================ Result<TValue, DomainError> Conversions ================
    public static ApiResponse<TValue> ToApiResponse<TValue>(
        this Result<TValue, DomainError> result,
        string? correlationId = null)
    {
        return result.Match(
            success: data => CreateApiResponse(data: data, correlationId: correlationId),
            failure: error => error.ToApiResponse<TValue>(correlationId));
    }

    public static ApiResponse<TValue> ToApiResponse<TValue>(
        this Result<TValue, DomainError> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match(
            success: data => CreateApiResponse(data: data, message: successMessage, correlationId: correlationId),
            failure: error => error.ToApiResponse<TValue>(correlationId));
    }

    // ================ Result<TValue, DomainErrors> Conversions ================
    public static ApiResponse<TValue> ToApiResponse<TValue>(
        this Result<TValue, DomainErrors> result,
        string? correlationId = null)
    {
        return result.Match(
            success: data => CreateApiResponse(data: data, correlationId: correlationId),
            failure: errors => errors.ToApiResponse<TValue>(correlationId));
    }

    public static ApiResponse<TValue> ToApiResponse<TValue>(
        this Result<TValue, DomainErrors> result,
        string successMessage,
        string? correlationId = null)
    {
        return result.Match(
            success: data => CreateApiResponse(data: data, message: successMessage, correlationId: correlationId),
            failure: errors => errors.ToApiResponse<TValue>(correlationId));
    }

    // ================ SuccessWithWarnings ================
    public static ApiResponse<T> ToApiResponse<T>(
        this SuccessWithWarnings<T> result,
        string? correlationId = null,
        DomainWarnings? warnings = null)
        => CreateApiResponse(
            warnings: warnings?.Warnings,
            data: result.Value,
            correlationId: correlationId);

    // ================ Error Conversion Helpers ================
    public static IReadOnlyList<ApiError>? ToApiErrors(this IReadOnlyList<DomainError> errors)
        => errors?.Select(e => e.ToApiError()).ToList();

    public static IReadOnlyList<ApiError>? ToApiErrors(this DomainErrors errors)
        => errors.Errors.ToApiErrors();

    public static ApiError[] ToApiErrorArray(this DomainErrors errors)
        => errors.ToApiErrors()?.ToArray() ?? Array.Empty<ApiError>();

    // ================ Error Analysis Helpers ================
    public static DomainError GetMostSevereError(this IEnumerable<DomainError> errors)
    {
        return !errors.Any()
            ? throw new ArgumentException("No errors provided", nameof(errors))
            : errors.MaxBy(e => ErrorSeverity.GetValueOrDefault(e.Type, 0));
    }

    public static ErrorType GetMostSevereErrorType(this DomainErrors errors)
        => errors.Errors.GetMostSevereError().Type;

    public static string GetAggregateErrorMessage(this DomainErrors errors)
        => string.Join("; ", errors.Errors.Select(e => e.Message));

    // ================ Error Details Formatting ================
    public static Dictionary<string, object>? ToErrorDetails(this DomainErrors errors)
    {
        return errors.Errors.Count == 0
            ? null
            : new Dictionary<string, object>
            {
                ["errors"] = errors.Errors
                .Select((e, i) => new
                {
                    Index = i,
                    e.Type,
                    e.Code,
                    e.Target,
                    Severity = e.Type.ToSeverityString()
                })
                .ToList()
            };
    }

    #endregion

    #region Helper Methods

    // CRUD Success Helpers
    private static IActionResult CreateGetActionResult<T>(T value)
    {
        return value is null ? new NoContentResult() : new OkObjectResult(value);
    }

    private static IActionResult CreateCreatedActionResult<T>(T value, string locationUri)
    {
        return new CreatedResult(locationUri, value);
    }

    private static IActionResult CreatePutActionResult<T>(T value)
    {
        return value is null ? new NoContentResult() : new OkObjectResult(value);
    }

    private static IActionResult CreateDeleteActionResult(bool deleted)
    {
        return deleted ? new NoContentResult() : new NotFoundResult();
    }

    private static IResult CreateGetMinimalApiResult<T>(T value)
    {
        return value is null ? TypedResults.NoContent() : TypedResults.Ok(value);
    }

    private static IResult CreateCreatedMinimalApiResult<T>(T value, string locationUri)
    {
        return TypedResults.Created(locationUri, value);
    }

    private static IResult CreatePutMinimalApiResult<T>(T value)
    {
        return value is null ? TypedResults.NoContent() : TypedResults.Ok(value);
    }

    private static IResult CreateDeleteMinimalApiResult(bool deleted)
    {
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    // New CreateSuccessResponse overload that accepts an explicit message
    private static IActionResult CreateSuccessResponse<TValue>(TValue value, string successMessage, string? correlationId)
    {
        var response = ApiResponse<TValue>.Success(
            data: value,
            new ApiResponseOptions
            {
                Message = successMessage,
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return new OkObjectResult(response);
    }

    private static IActionResult CreateSuccessResponse<TValue>(TValue value, string? correlationId)
    {
        var response = ApiResponse<TValue>.Success(
            data: value,
            new ApiResponseOptions
            {
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return new OkObjectResult(response);
    }

    private static IActionResult CreateWarningResponse<TValue>(
        TValue value,
        DomainWarnings warnings,
        string? correlationId)
    {
        var response = ApiResponse<TValue>.Warning(
            data: value,
            warnings: warnings.ToApiErrors()?.ToArray() ?? Array.Empty<ApiError>(),
            new ApiResponseOptions
            {
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return new OkObjectResult(response);
    }

    private static IResult CreateSuccessResult<TValue>(TValue value, string? correlationId)
    {
        var response = ApiResponse<TValue>.Success(
            data: value,
            new ApiResponseOptions
            {
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return TypedResults.Ok(response);
    }

    private static IResult CreateSuccessResult<TValue>(TValue value, string successMessage, string? correlationId)
    {
        var response = ApiResponse<TValue>.Success(
            data: value,
            new ApiResponseOptions
            {
                Message = successMessage,
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return TypedResults.Ok(response);
    }

    private static IResult CreateWarningResult<TValue>(
        TValue value,
        DomainWarnings warnings,
        string? correlationId)
    {
        var response = ApiResponse<TValue>.Warning(
            data: value,
            warnings: warnings.ToApiErrors()?.ToArray() ?? Array.Empty<ApiError>(),
            new ApiResponseOptions
            {
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );
        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return TypedResults.Ok(response);
    }

    // Paged Result Helpers
    private static IActionResult CreatePagedSuccessResponse<T>(
        PagedResult<T> pagedResult,
        string? baseUrl,
        string? correlationId)
    {
        // Add pagination metadata to the result
        var metadata = new Dictionary<string, object>(pagedResult.Metadata ?? new Dictionary<string, object>());
        metadata = metadata.WithPagination(
            pagedResult.PageIndex,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            baseUrl != null ? (page => $"{baseUrl}?page={page}&size={pagedResult.PageSize}") : null
        );

        // Add correlation ID if provided
        if (!string.IsNullOrEmpty(correlationId))
        {
            metadata = metadata.WithCorrelationId(correlationId);
        }

        var response = ApiResponse<PagedResult<T>>.Success(
            data: pagedResult,
            new ApiResponseOptions
            {
                Metadata = metadata,
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );

        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return new OkObjectResult(response);
    }

    private static IResult CreatePagedSuccessResult<T>(
        PagedResult<T> pagedResult,
        string? baseUrl,
        string? correlationId)
    {
        // Add pagination metadata to the result
        var metadata = new Dictionary<string, object>(pagedResult.Metadata ?? new Dictionary<string, object>());
        metadata = metadata.WithPagination(
            pagedResult.PageIndex,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            baseUrl != null ? (page => $"{baseUrl}?page={page}&size={pagedResult.PageSize}") : null
        );

        // Add correlation ID if provided
        if (!string.IsNullOrEmpty(correlationId))
        {
            metadata = metadata.WithCorrelationId(correlationId);
        }

        var response = ApiResponse<PagedResult<T>>.Success(
            data: pagedResult,
            new ApiResponseOptions
            {
                Metadata = metadata,
                CorrelationId = correlationId,
                StatusCode = HttpStatusCode.OK
            }
        );

        ConfigureResponse?.Invoke(response.ToBaseResponse());
        return TypedResults.Ok(response);
    }

    #endregion

    #region ApiResponse Extensions

    /// <summary>
    /// Converts generic ApiResponse&lt;T&gt; to base ApiResponse
    /// </summary>
    public static ApiResponse ToBaseResponse<T>(this ApiResponse<T> response)
    {
        var options = new ApiResponseOptions
        {
            Message = response.Message,
            Errors = response.Errors,
            Warnings = response.Warnings,
            Metadata = response.Metadata,
            CorrelationId = response.CorrelationId,
            StatusCode = response.StatusCode,
            TraceId = response.TraceId,
            Timestamp = response.Timestamp
        };

        return response.IsSuccess
            ? ApiResponse.Success(response.Data, options)
            : ApiResponse.Failure(response.Errors ?? Array.Empty<ApiError>(), options);
    }

    /// <summary>
    /// Converts ApiResponse&lt;T&gt; to IActionResult
    /// </summary>
    public static IActionResult ToActionResult<T>(this ApiResponse<T> response)
    {
        return response.ToBaseResponse().ToActionResult();
    }

    /// <summary>
    /// Converts ApiResponse&lt;T&gt; to IResult (for Minimal APIs)
    /// </summary>
    public static IResult ToMinimalApiResult<T>(this ApiResponse<T> response)
    {
        return response.ToBaseResponse().ToMinimalApiResult();
    }

    /// <summary>
    /// Converts base ApiResponse to IActionResult with full status code support
    /// </summary>
    public static IActionResult ToActionResult(this ApiResponse response)
    {
        return response.StatusCode switch
        {
            // Success (2xx)
            HttpStatusCode.OK => new OkObjectResult(response),
            HttpStatusCode.Created => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.Created },
            HttpStatusCode.Accepted => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.Accepted },
            HttpStatusCode.NoContent => new NoContentResult(),

            // Client Errors (4xx)
            HttpStatusCode.BadRequest => new BadRequestObjectResult(response),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            HttpStatusCode.Forbidden => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.Forbidden },
            HttpStatusCode.NotFound => new NotFoundObjectResult(response),
            HttpStatusCode.Conflict => new ConflictObjectResult(response),
            HttpStatusCode.UnprocessableEntity => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.UnprocessableEntity },
            HttpStatusCode.TooManyRequests => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.TooManyRequests },
            HttpStatusCode.RequestEntityTooLarge => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.RequestEntityTooLarge },
            HttpStatusCode.RequestUriTooLong => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.RequestUriTooLong },
            HttpStatusCode.UnsupportedMediaType => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.UnsupportedMediaType },
            (HttpStatusCode)428 => new ObjectResult(response) { StatusCode = 428 }, // PreconditionRequired
            (HttpStatusCode)431 => new ObjectResult(response) { StatusCode = 431 }, // RequestHeaderFieldsTooLarge
            (HttpStatusCode)451 => new ObjectResult(response) { StatusCode = 451 }, // UnavailableForLegalReasons

            // Server Errors (5xx)
            HttpStatusCode.InternalServerError => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.InternalServerError },
            HttpStatusCode.NotImplemented => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.NotImplemented },
            HttpStatusCode.BadGateway => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.BadGateway },
            HttpStatusCode.ServiceUnavailable => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.ServiceUnavailable },
            HttpStatusCode.GatewayTimeout => new ObjectResult(response)
            { StatusCode = (int)HttpStatusCode.GatewayTimeout },
            (HttpStatusCode)507 => new ObjectResult(response) { StatusCode = 507 }, // InsufficientStorage
            (HttpStatusCode)508 => new ObjectResult(response) { StatusCode = 508 }, // LoopDetected
            (HttpStatusCode)510 => new ObjectResult(response) { StatusCode = 510 }, // NotExtended
            (HttpStatusCode)511 => new ObjectResult(response) { StatusCode = 511 }, // NetworkAuthenticationRequired

            _ => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.InternalServerError }
        };
    }

    /// <summary>
    /// Converts ApiResponse to IResult for Minimal APIs
    /// </summary>
    public static IResult ToMinimalApiResult(this ApiResponse response)
    {
        return response.StatusCode switch
        {
            // Success (2xx)
            HttpStatusCode.OK => TypedResults.Ok(response),
            HttpStatusCode.Created => TypedResults.Created(string.Empty, response),
            HttpStatusCode.Accepted => TypedResults.Accepted(string.Empty, response),
            HttpStatusCode.NoContent => TypedResults.NoContent(),

            // Client Errors (4xx)
            HttpStatusCode.BadRequest => TypedResults.BadRequest(response),
            HttpStatusCode.Unauthorized => TypedResults.Json(response, statusCode: 401),
            HttpStatusCode.Forbidden => TypedResults.Json(response, statusCode: 403),
            HttpStatusCode.NotFound => TypedResults.NotFound(response),
            HttpStatusCode.Conflict => TypedResults.Conflict(response),
            HttpStatusCode.UnprocessableEntity => TypedResults.UnprocessableEntity(response),
            HttpStatusCode.TooManyRequests => TypedResults.Json(response, statusCode: 429),
            HttpStatusCode.RequestEntityTooLarge => TypedResults.Json(response, statusCode: 413),
            HttpStatusCode.RequestUriTooLong => TypedResults.Json(response, statusCode: 414),
            HttpStatusCode.UnsupportedMediaType => TypedResults.Json(response, statusCode: 415),
            (HttpStatusCode)428 => TypedResults.Json(response, statusCode: 428), // PreconditionRequired
            (HttpStatusCode)431 => TypedResults.Json(response, statusCode: 431), // RequestHeaderFieldsTooLarge
            (HttpStatusCode)451 => TypedResults.Json(response, statusCode: 451), // UnavailableForLegalReasons

            // Server Errors (5xx)
            HttpStatusCode.InternalServerError => TypedResults.Json(response, statusCode: 500),
            HttpStatusCode.NotImplemented => TypedResults.Json(response, statusCode: 501),
            HttpStatusCode.BadGateway => TypedResults.Json(response, statusCode: 502),
            HttpStatusCode.ServiceUnavailable => TypedResults.Json(response, statusCode: 503),
            HttpStatusCode.GatewayTimeout => TypedResults.Json(response, statusCode: 504),
            (HttpStatusCode)507 => TypedResults.Json(response, statusCode: 507), // InsufficientStorage
            (HttpStatusCode)508 => TypedResults.Json(response, statusCode: 508), // LoopDetected
            (HttpStatusCode)510 => TypedResults.Json(response, statusCode: 510), // NotExtended
            (HttpStatusCode)511 => TypedResults.Json(response, statusCode: 511), // NetworkAuthenticationRequired

            _ => TypedResults.Json(response, statusCode: 500)
        };
    }

    /// <summary>
    /// Adds pagination metadata with enhanced details
    /// </summary>
    public static Dictionary<string, object> WithPagination(
        this Dictionary<string, object> metadata,
        int page,
        int pageSize,
        long totalItems,
        Func<int, string>? pageUrlGenerator = null)
    {
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var pagination = new Dictionary<string, object>
        {
            [ApiMetadataKeys.PageIndex] = page,
            [ApiMetadataKeys.PageSize] = pageSize,
            [ApiMetadataKeys.TotalCount] = totalItems,
            [ApiMetadataKeys.TotalPages] = totalPages,
            [ApiMetadataKeys.HasNextPage] = page < totalPages,
            [ApiMetadataKeys.HasPreviousPage] = page > 1
        };

        if (pageUrlGenerator != null)
        {
            pagination[ApiMetadataKeys.Links] = new
            {
                first = pageUrlGenerator(1),
                prev = page > 1 ? pageUrlGenerator(page - 1) : null,
                next = page < totalPages ? pageUrlGenerator(page + 1) : null,
                last = pageUrlGenerator(totalPages)
            };
        }

        metadata[ApiMetadataKeys.Pagination] = pagination;
        return metadata;
    }

    /// <summary>
    /// Adds execution time metadata with precision control
    /// </summary>
    public static Dictionary<string, object> WithExecutionTime(
        this Dictionary<string, object>? metadata,
        TimeSpan executionTime,
        int precision = 2)
    {
        var result = metadata ?? new Dictionary<string, object>();
        result[ApiMetadataKeys.Duration] = new
        {
            Milliseconds = Math.Round(executionTime.TotalMilliseconds, precision),
            Seconds = Math.Round(executionTime.TotalSeconds, precision),
            Formatted = $"{Math.Round(executionTime.TotalMilliseconds, precision)}ms"
        };
        return result;
    }

    /// <summary>
    /// Adds correlation ID to metadata if not already present
    /// </summary>
    public static Dictionary<string, object> WithCorrelationId(
        this Dictionary<string, object>? metadata,
        string correlationId)
    {
        var result = metadata ?? new Dictionary<string, object>();
        if (!result.ContainsKey(ApiMetadataKeys.CorrelationId))
        {
            result[ApiMetadataKeys.CorrelationId] = correlationId;
        }
        return result;
    }

    #endregion

    #region Paged Result Extensions

    public static Dictionary<string, string?> BuildPaginationLinks<T>(
        this PagedResult<T> result,
        string baseRoute,
        int pageSize)
    {
        string Link(int page) => $"{baseRoute}?page={page}&size={pageSize}";

        return new()
        {
            ["first"] = Link(1),
            ["previous"] = result.HasPreviousPage ? Link(result.PageIndex - 1) : null,
            ["next"] = result.HasNextPage ? Link(result.PageIndex + 1) : null,
            ["last"] = Link(result.TotalPages)
        };
    }

    /// <summary>
    /// Converts Result&lt;PagedResult&lt;T&gt;&gt; to IActionResult with automatic pagination metadata
    /// </summary>
    public static IActionResult ToPagedActionResult<T>(
        this Result<PagedResult<T>, DomainError> result,
        string? baseUrl = null,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            pagedResult => CreatePagedSuccessResponse(pagedResult, baseUrl, correlationId),
            error => error.ToApiResponse<PagedResult<T>>(correlationId).ToActionResult()
        );
    }

    /// <summary>
    /// Converts Result&lt;PagedResult&lt;T&gt;&gt; to IActionResult with automatic pagination metadata (DomainErrors)
    /// </summary>
    public static IActionResult ToPagedActionResult<T>(
        this Result<PagedResult<T>, DomainErrors> result,
        string? baseUrl = null,
        string? correlationId = null)
    {
        return result.Match<IActionResult>(
            pagedResult => CreatePagedSuccessResponse(pagedResult, baseUrl, correlationId),
            errors => errors.ToApiResponse<PagedResult<T>>(correlationId).ToActionResult()
        );
    }

    /// <summary>
    /// Converts Result&lt;PagedResult&lt;T&gt;&gt; to IResult with automatic pagination metadata (Minimal API)
    /// </summary>
    public static IResult ToPagedMinimalApiResult<T>(
        this Result<PagedResult<T>, DomainError> result,
        string? baseUrl = null,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            pagedResult => CreatePagedSuccessResult(pagedResult, baseUrl, correlationId),
            error => error.ToApiResponse<PagedResult<T>>(correlationId).ToMinimalApiResult()
        );
    }

    /// <summary>
    /// Converts Result&lt;PagedResult&lt;T&gt;&gt; to IResult with automatic pagination metadata (Minimal API, DomainErrors)
    /// </summary>
    public static IResult ToPagedMinimalApiResult<T>(
        this Result<PagedResult<T>, DomainErrors> result,
        string? baseUrl = null,
        string? correlationId = null)
    {
        return result.Match<IResult>(
            pagedResult => CreatePagedSuccessResult(pagedResult, baseUrl, correlationId),
            errors => errors.ToApiResponse<PagedResult<T>>(correlationId).ToMinimalApiResult()
        );
    }

    /// <summary>
    /// Async version for Result&lt;PagedResult&lt;T&gt;&gt; to IActionResult
    /// </summary>
    public static async Task<IActionResult> ToPagedActionResultAsync<T>(
        this Task<Result<PagedResult<T>, DomainError>> resultTask,
        string? baseUrl = null,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPagedActionResult(baseUrl, correlationId);
    }

    /// <summary>
    /// Async version for Result&lt;PagedResult&lt;T&gt;&gt; to IActionResult (DomainErrors)
    /// </summary>
    public static async Task<IActionResult> ToPagedActionResultAsync<T>(
        this Task<Result<PagedResult<T>, DomainErrors>> resultTask,
        string? baseUrl = null,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPagedActionResult(baseUrl, correlationId);
    }

    /// <summary>
    /// Async version for Result&lt;PagedResult&lt;T&gt;&gt; to IResult (Minimal API)
    /// </summary>
    public static async Task<IResult> ToPagedMinimalApiResultAsync<T>(
        this Task<Result<PagedResult<T>, DomainError>> resultTask,
        string? baseUrl = null,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPagedMinimalApiResult(baseUrl, correlationId);
    }

    /// <summary>
    /// Async version for Result&lt;PagedResult&lt;T&gt;&gt; to IResult (Minimal API, DomainErrors)
    /// </summary>
    public static async Task<IResult> ToPagedMinimalApiResultAsync<T>(
        this Task<Result<PagedResult<T>, DomainErrors>> resultTask,
        string? baseUrl = null,
        string? correlationId = null)
    {
        var result = await resultTask;
        return result.ToPagedMinimalApiResult(baseUrl, correlationId);
    }

    #endregion

    #region Domain Warnings Extensions

    public static IReadOnlyList<ApiError>? ToApiErrors(this DomainWarnings warnings)
        => (IReadOnlyList<ApiError>?)warnings.Warnings.Select(w => w.ToApiError());

    public static ApiError[] ToApiErrorArray(this DomainWarnings warnings)
        => warnings.ToApiErrors()?.ToArray() ?? Array.Empty<ApiError>();

    public static Dictionary<string, object>? ToWarningDetails(this DomainWarnings warnings)
    {
        return !warnings.HasWarnings
            ? null
            : new Dictionary<string, object>
            {
                ["warnings"] = warnings.Warnings
                .Select((w, i) => new
                {
                    Index = i,
                    w.Type,
                    w.Code,
                    w.Target,
                    Severity = w.Type.ToSeverityString()
                })
                .ToList()
            };
    }

    public static DomainWarnings AddToCopy(
        this IReadOnlyList<DomainError> warnings,
        DomainError warning)
    {
        var newWarnings = new List<DomainError>(warnings) { warning };
        return new DomainWarnings(newWarnings);
    }

    public static DomainWarnings AddRangeToCopy(
        this IReadOnlyList<DomainError> warnings,
        IEnumerable<DomainError> additionalWarnings)
    {
        var newWarnings = new List<DomainError>(warnings);
        newWarnings.AddRange(additionalWarnings);
        return new DomainWarnings(newWarnings);
    }

    #endregion

    #region SuccessWithWarnings Extensions

    public static SuccessWithWarnings<T> WithWarning<T>(
        this T value,
        DomainError warning) => new(value, DomainWarnings.FromSingle(warning));

    public static SuccessWithWarnings<T> WithWarnings<T>(
        this T value,
        IEnumerable<DomainError> warnings) => new(value, DomainWarnings.Multiple(warnings));

    #endregion
}

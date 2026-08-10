namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Options bag for customising an <see cref="ApiResponse{T}"/> beyond its default values.
/// All properties are optional; supply only what you need.
/// </summary>
public sealed record ApiResponseOptions
{
    /// <summary>Human-readable message summarising the operation outcome.</summary>
    public string? Message { get; init; }
    /// <summary>List of errors to include in the response envelope.</summary>
    public IReadOnlyList<ApiError>? Errors { get; init; }
    /// <summary>List of non-fatal warnings to include in the response envelope.</summary>
    public IReadOnlyList<ApiError>? Warnings { get; init; }
    /// <summary>Arbitrary metadata key-value pairs attached to the response.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    /// <summary>Correlation identifier for distributed tracing.</summary>
    public string? CorrelationId { get; init; }
    /// <summary>Trace identifier — defaults to <see cref="Activity.Current"/>?.Id when not supplied.</summary>
    public string? TraceId { get; init; }
    /// <summary>ISO-8601 timestamp — defaults to <see cref="DateTimeOffset.UtcNow"/> when not supplied.</summary>
    public string? Timestamp { get; init; }
    /// <summary>HTTP status code to use for this response. Defaults to <see cref="HttpStatusCode.OK"/>.</summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
}

/// <summary>
/// Generic API response envelope.
/// Wraps <typeparamref name="T"/> data together with status, errors, warnings, and observability fields.
/// </summary>
public record ApiResponse<T>
{
    /// <summary>High-level operation status (Success / Error / Warning).</summary>
    public ResponseStatus Status { get; }
    /// <summary>HTTP status code as a string (e.g., <c>"200"</c>).</summary>
    public string Code { get; }
    /// <summary>Response payload. <c>null</c> on error responses.</summary>
    public T? Data { get; }
    /// <summary>Human-readable summary of the outcome.</summary>
    public string? Message { get; }
    /// <summary>List of errors when <see cref="Status"/> is <see cref="ResponseStatus.Error"/>.</summary>
    public IReadOnlyList<ApiError>? Errors { get; }
    /// <summary>Non-fatal warnings when <see cref="Status"/> is <see cref="ResponseStatus.Warning"/>.</summary>
    public IReadOnlyList<ApiError>? Warnings { get; }
    /// <summary>Arbitrary metadata key-value pairs.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; }
    /// <summary>Correlation ID for distributed tracing.</summary>
    public string? CorrelationId { get; }
    /// <summary>Trace ID sourced from <see cref="Activity.Current"/>.</summary>
    public string? TraceId { get; }
    /// <summary>ISO-8601 UTC timestamp when this response was created.</summary>
    public string Timestamp { get; }

    /// <summary>The HTTP status code. Excluded from JSON serialization (use <see cref="Code"/>).</summary>
    [JsonIgnore]
    public HttpStatusCode StatusCode { get; }

    /// <summary>Internal constructor — use the static factory methods instead.</summary>
    protected internal ApiResponse(
        ResponseStatus status,
        string code,
        ApiResponseOptions options,
        T? data = default)
    {
        Status = status;
        Code = code;
        Data = data;
        Message = options.Message;
        Errors = options.Errors;
        Warnings = options.Warnings;
        Metadata = options.Metadata;
        CorrelationId = options.CorrelationId;
        TraceId = options.TraceId ?? Activity.Current?.Id;
        Timestamp = options.Timestamp ?? DateTimeOffset.UtcNow.ToString("O");
        StatusCode = options.StatusCode;
    }

    /// <summary><c>true</c> when the operation succeeded.</summary>
    [JsonIgnore] public bool IsSuccess => Status == ResponseStatus.Success;
    /// <summary><c>true</c> when the operation failed.</summary>
    [JsonIgnore] public bool IsError => Status == ResponseStatus.Error;
    /// <summary><c>true</c> when the response carries one or more warnings.</summary>
    [JsonIgnore] public bool HasWarnings => Warnings?.Count > 0;
    /// <summary><c>true</c> when the response carries one or more errors.</summary>
    [JsonIgnore] public bool HasErrors => Errors?.Count > 0;
    /// <summary><c>true</c> when <see cref="Data"/> is not <c>null</c>.</summary>
    [JsonIgnore] public bool HasData => Data is not null;

    /// <summary>Creates a successful response wrapping <paramref name="data"/>.</summary>
    public static ApiResponse<T> Success(T data, ApiResponseOptions? options = null)
    {
        options = InitializeOptions(options, HttpStatusCode.OK);
        return new ApiResponse<T>(
            ResponseStatus.Success,
            ((int)options.StatusCode).ToString(),
            new ApiResponseOptions
            {
                Message = options.Message ?? "Operation completed successfully.",
                Errors = Array.Empty<ApiError>(),
                Warnings = options.Warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = options.StatusCode
            },
            data);
    }

    /// <summary>Creates a failure response from a single error.</summary>
    public static ApiResponse<T> Failure(ApiError error, ApiResponseOptions? options = null)
        => Failure(new[] { error }, options);

    /// <summary>Creates a failure response from a list of errors.</summary>
    public static ApiResponse<T> Failure(IReadOnlyList<ApiError>? errors, ApiResponseOptions? options = null)
    {
        options = InitializeOptions(options, HttpStatusCode.BadRequest);
        return new ApiResponse<T>(
            ResponseStatus.Error,
            ((int)options.StatusCode).ToString(),
            new ApiResponseOptions
            {
                Message = options.Message ?? "An error occurred.",
                Errors = errors ?? Array.Empty<ApiError>(),
                Warnings = options.Warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = options.StatusCode
            });
    }

    /// <summary>Creates a success response with non-fatal warnings.</summary>
    public static ApiResponse<T> Warning(T data, IReadOnlyList<ApiError> warnings, ApiResponseOptions? options = null)
    {
        options = InitializeOptions(options, HttpStatusCode.OK);
        return new ApiResponse<T>(
            ResponseStatus.Warning,
            ((int)options.StatusCode).ToString(),
            new ApiResponseOptions
            {
                Message = options.Message ?? "Operation completed with warnings.",
                Errors = Array.Empty<ApiError>(),
                Warnings = warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = options.StatusCode
            },
            data);
    }

    private static ApiResponseOptions InitializeOptions(ApiResponseOptions? options, HttpStatusCode defaultStatusCode)
    {
        return options == null
            ? new ApiResponseOptions { StatusCode = defaultStatusCode }
            : options.StatusCode == HttpStatusCode.OK && defaultStatusCode != HttpStatusCode.OK
            ? (options with { StatusCode = defaultStatusCode })
            : options;
    }
}

/// <summary>
/// Non-generic convenience wrapper for responses that carry no typed payload.
/// Inherits from <see cref="ApiResponse{T}"/> with <c>T = object?</c>.
/// </summary>
public sealed record ApiResponse : ApiResponse<object?>
{
    private ApiResponse(
        ResponseStatus status,
        string code,
        ApiResponseOptions options,
        object? data = null)
        : base(status, code, options, data)
    {
    }

    /// <summary>Creates a successful response (no typed payload).</summary>
    public static new ApiResponse Success(object? data = null, ApiResponseOptions? options = null)
    {
        options = InitializeOptions(options, HttpStatusCode.OK);
        return new ApiResponse(
            ResponseStatus.Success,
            ((int)options.StatusCode).ToString(),
            new ApiResponseOptions
            {
                Message = options.Message ?? "Operation completed successfully.",
                Errors = Array.Empty<ApiError>(),
                Warnings = options.Warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = options.StatusCode
            },
            data
        );
    }

    /// <summary>Creates a failure response from a list of errors.</summary>
    public static new ApiResponse Failure(IReadOnlyList<ApiError> errors, ApiResponseOptions? options = null)
    {
        options = InitializeOptions(options, HttpStatusCode.BadRequest);
        return new ApiResponse(
            ResponseStatus.Error,
            ((int)options.StatusCode).ToString(),
            new ApiResponseOptions
            {
                Message = options.Message ?? "An error occurred.",
                Errors = errors,
                Warnings = options.Warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = options.StatusCode
            });
    }

    /// <summary>Creates a failure response from a single error.</summary>
    public static new ApiResponse Failure(ApiError error, ApiResponseOptions? options = null)
        => Failure(new[] { error }, options);

    private static ApiResponseOptions InitializeOptions(ApiResponseOptions? options, HttpStatusCode defaultStatusCode)
    {
        return options == null
            ? new ApiResponseOptions { StatusCode = defaultStatusCode }
            : options.StatusCode == HttpStatusCode.OK && defaultStatusCode != HttpStatusCode.OK
            ? new ApiResponseOptions
            {
                Message = options.Message,
                Errors = options.Errors,
                Warnings = options.Warnings,
                Metadata = options.Metadata,
                CorrelationId = options.CorrelationId,
                TraceId = options.TraceId,
                Timestamp = options.Timestamp,
                StatusCode = defaultStatusCode
            }
            : options;
    }
}

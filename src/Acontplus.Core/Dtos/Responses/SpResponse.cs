namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Represents a standardised response from a stored procedure.
/// A Code of <c>"0"</c> indicates success; any other value is treated as an error.
/// </summary>
/// <remarks>
/// For new stored procedures prefer the generic <see cref="SpResponse{T}"/> which gives
/// full type safety and IntelliSense on the result payload.
/// Use <c>SpResponse&lt;dynamic&gt;</c> when the shape is not known at compile time.
/// </remarks>
public record SpResponse
{
    /// <summary>Result code returned by the stored procedure. <c>"0"</c> means success.</summary>
    public required string Code { get; set; }

    /// <summary>Primary result payload returned by the stored procedure.</summary>
    public dynamic? Result { get; set; }

    /// <summary>
    /// Deprecated secondary payload. Prefer <see cref="Result"/> for all new stored procedures.
    /// Retained for backward compatibility with existing procedures that still populate Payload.
    /// </summary>
    [Obsolete("Use Result instead. Payload will be removed in a future major version.")]
    public dynamic? Payload { get; set; }

    /// <summary>Human-readable message returned by the stored procedure.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the stored procedure signals success.
    /// Accepts <c>"0"</c> (standard) and <c>"1"</c> (legacy) as success codes.
    /// </summary>
    public bool IsSuccess => Code is "0" or "1";
}

/// <summary>
/// Strongly-typed stored procedure response.
/// </summary>
/// <typeparam name="T">
/// The type of the result payload. Use a concrete DTO for full compile-time safety,
/// or pass <c>dynamic</c> when the shape is not known at compile time.
/// </typeparam>
/// <example>
/// <code>
/// // Fully typed — IntelliSense + compile-time safety
/// SpResponse&lt;UserDto&gt; response = await repo.ExecuteSpAsync&lt;UserDto&gt;("GetUser");
/// var name = response.Result?.Name;
///
/// // Dynamic — same flexibility as the non-generic SpResponse
/// SpResponse&lt;dynamic&gt; response = await repo.ExecuteSpAsync&lt;dynamic&gt;("GetUser");
/// var name = response.Result.Name;
/// </code>
/// </example>
public record SpResponse<T>
{
    /// <summary>Result code returned by the stored procedure. <c>"0"</c> means success.</summary>
    public required string Code { get; set; }

    /// <summary>Typed result payload returned by the stored procedure.</summary>
    public T? Result { get; set; }

    /// <summary>Human-readable message returned by the stored procedure.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// Returns <c>true</c> when <see cref="Code"/> is <c>"0"</c> (success).
    /// New stored procedures should always return <c>"0"</c> — legacy <c>"1"</c> is not accepted here.
    /// </summary>
    public bool IsSuccess => Code == "0";
}

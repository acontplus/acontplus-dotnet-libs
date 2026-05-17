namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Represents a standardised response from a stored procedure.
/// A Code of <c>"0"</c> indicates success; any other value is treated as an error.
/// </summary>
public record SpResponse
{
    /// <summary>Result code returned by the stored procedure. <c>"0"</c> means success.</summary>
    public required string Code { get; set; }

    /// <summary>Primary result payload returned by the stored procedure.</summary>
    public object? Result { get; set; }

    /// <summary>
    /// Deprecated secondary payload. Prefer <see cref="Result"/> for all new stored procedures.
    /// Retained for backward compatibility with existing procedures that still populate Payload.
    /// </summary>
    [Obsolete("Use Result instead. Payload will be removed in a future major version.")]
    public object? Payload { get; set; }

    /// <summary>Human-readable message returned by the stored procedure.</summary>
    public string? Message { get; set; }

    /// <summary>Returns <c>true</c> when <see cref="Code"/> is <c>"0"</c> (success).</summary>
    public bool IsSuccess => Code == "0";
}

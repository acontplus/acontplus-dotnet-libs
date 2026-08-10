namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Represents an API error response with detailed information about the error.
/// </summary>
/// <param name="Code">The error code identifying the type of error.</param>
/// <param name="Message">A human-readable message describing the error.</param>
/// <param name="Target">The specific target of the error (e.g., property name or parameter).</param>
/// <param name="Details">Additional details about the error as key-value pairs.</param>
/// <param name="Severity">The severity level of the error (default: "error").</param>
/// <param name="Category">The category of the error (default: "system").</param>
/// <param name="HelpUrl">A URL to documentation or help resources for this error.</param>
/// <param name="SuggestedAction">A suggested action to resolve the error.</param>
/// <param name="TraceId">A trace identifier for tracking the error across systems.</param>
public sealed record ApiError(
    string Code,
    string Message,
    string? Target = null,
    Dictionary<string, object>? Details = null,
    string Severity = "error",
    string Category = "system",
    string? HelpUrl = null,
    string? SuggestedAction = null,
    string? TraceId = null);

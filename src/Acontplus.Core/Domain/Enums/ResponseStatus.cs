namespace Acontplus.Core.Domain.Enums;

/// <summary>Represents the overall status of an API response envelope.</summary>
public enum ResponseStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success,

    /// <summary>The operation failed due to an error.</summary>
    Error,

    /// <summary>The operation succeeded but produced one or more warnings.</summary>
    Warning
}

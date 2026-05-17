namespace Acontplus.Core.Domain.Common.Results;

/// <summary>
/// Represents warnings that don't prevent operation success but should be communicated.
/// </summary>
/// <param name="Warnings">Collection of individual warnings.</param>
public readonly record struct DomainWarnings(IReadOnlyList<DomainError> Warnings)
{
    /// <summary>
    /// Creates a DomainWarnings instance from a single warning.
    /// </summary>
    /// <param name="warning">The warning to wrap.</param>
    /// <returns>A DomainWarnings instance containing the single warning.</returns>
    public static DomainWarnings FromSingle(DomainError warning) => new([warning]);

    /// <summary>
    /// Creates a DomainWarnings instance from multiple warnings.
    /// </summary>
    /// <param name="warnings">The warnings to include.</param>
    /// <returns>A DomainWarnings instance containing the specified warnings.</returns>
    public static DomainWarnings Multiple(params DomainError[] warnings) => new(warnings);

    /// <summary>
    /// Creates a DomainWarnings instance from a collection of warnings.
    /// </summary>
    /// <param name="warnings">The collection of warnings to include.</param>
    /// <returns>A DomainWarnings instance containing the specified warnings.</returns>
    public static DomainWarnings Multiple(IEnumerable<DomainError> warnings) => new(warnings.ToList());

    /// <summary>
    /// Implicitly converts a DomainError to a DomainWarnings instance.
    /// </summary>
    /// <param name="warning">The warning to convert.</param>
    public static implicit operator DomainWarnings(DomainError warning) => FromSingle(warning);

    /// <summary>
    /// Implicitly converts an array of DomainErrors to a DomainWarnings instance.
    /// </summary>
    /// <param name="warnings">The array of warnings to convert.</param>
    public static implicit operator DomainWarnings(DomainError[] warnings) => Multiple(warnings);

    /// <summary>
    /// Implicitly converts a list of DomainErrors to a DomainWarnings instance.
    /// </summary>
    /// <param name="warnings">The list of warnings to convert.</param>
    public static implicit operator DomainWarnings(List<DomainError> warnings) => Multiple(warnings);

    /// <summary>
    /// Gets a value indicating whether there are any warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Determines whether there are any warnings of the specified type.
    /// </summary>
    /// <param name="type">The error type to check for.</param>
    /// <returns>true if there are warnings of the specified type; otherwise, false.</returns>
    public bool HasWarningsOfType(ErrorType type) => Warnings.Any(w => w.Type == type);

    /// <summary>
    /// Gets all warnings of the specified type.
    /// </summary>
    /// <param name="type">The error type to filter by.</param>
    /// <returns>A collection of warnings matching the specified type.</returns>
    public IEnumerable<DomainError> GetWarningsOfType(ErrorType type) => Warnings.Where(w => w.Type == type);

    /// <summary>
    /// Gets an aggregate message containing all warnings.
    /// </summary>
    /// <returns>A formatted string containing all warning messages, or "No warnings" if there are none.</returns>
    public string GetAggregateWarningMessage() => Warnings.Count switch
    {
        0 => "No warnings",
        1 => Warnings[0].Message,
        _ => $"Multiple warnings occurred ({Warnings.Count}): " +
             string.Join("; ", Warnings.Select(w => $"[{w.Type}] {w.Message}"))
    };
}

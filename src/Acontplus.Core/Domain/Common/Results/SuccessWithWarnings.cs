namespace Acontplus.Core.Domain.Common.Results;

/// <summary>
/// Represents a successful result that includes warnings
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class SuccessWithWarnings<T>
{
    /// <summary>
    /// The successful result value
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The warnings associated with the result
    /// </summary>
    public DomainWarnings Warnings { get; }

    /// <summary>
    /// Initializes a new instance of SuccessWithWarnings
    /// </summary>
    /// <param name="value">The success value</param>
    /// <param name="warnings">The warnings</param>
    public SuccessWithWarnings(T value, DomainWarnings warnings)
    {
        Value = value;
        Warnings = warnings;
    }

    /// <summary>
    /// Initializes a new instance of SuccessWithWarnings
    /// </summary>
    /// <param name="value">The success value</param>
    /// <param name="warnings">The warnings</param>
    public SuccessWithWarnings(T value, params DomainError[] warnings)
    {
        Value = value;
        Warnings = new DomainWarnings(warnings);
    }

    /// <summary>
    /// Creates a success with warnings result
    /// </summary>
    /// <param name="value">The success value</param>
    /// <param name="warnings">The warnings</param>
    /// <returns>A new SuccessWithWarnings instance</returns>
    public static SuccessWithWarnings<T> Create(T value, DomainWarnings warnings)
        => new(value, warnings);

    /// <summary>
    /// Creates a success with warnings result
    /// </summary>
    /// <param name="value">The success value</param>
    /// <param name="warnings">The warnings</param>
    /// <returns>A new SuccessWithWarnings instance</returns>
    public static SuccessWithWarnings<T> Create(T value, params DomainError[] warnings)
        => new(value, warnings);

    /// <summary>
    /// Converts the result to a string representation
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString() =>
        $"Success: {Value}, Warnings: {Warnings.Warnings.Count}";
}

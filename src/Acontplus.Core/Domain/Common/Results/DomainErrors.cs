namespace Acontplus.Core.Domain.Common.Results;

/// <summary>
/// Represents multiple domain errors that can occur during validation or complex operations.
/// </summary>
/// <param name="Errors">Collection of individual domain errors.</param>
public readonly record struct DomainErrors(IReadOnlyList<DomainError> Errors)
{
    private static readonly ErrorType[] SeverityOrder =
    {
        // Server Errors (5xx) - highest severity first
        ErrorType.Internal,
        ErrorType.External,
        ErrorType.ServiceUnavailable,
        ErrorType.Timeout,
        ErrorType.NotImplemented,
        ErrorType.HttpVersionNotSupported,
        ErrorType.InsufficientStorage,
        ErrorType.LoopDetected,
        ErrorType.NotExtended,
        ErrorType.NetworkAuthRequired,

        // Client Errors (4xx) - higher severity first
        ErrorType.RequestTimeout,
        ErrorType.UnavailableForLegal,
        ErrorType.Forbidden,
        ErrorType.Unauthorized,
        ErrorType.RateLimited,
        ErrorType.Conflict,
        ErrorType.NotFound,
        ErrorType.Validation,
        ErrorType.BadRequest,
        ErrorType.MethodNotAllowed,
        ErrorType.NotAcceptable,
        ErrorType.PayloadTooLarge,
        ErrorType.UriTooLong,
        ErrorType.UnsupportedMediaType,
        ErrorType.RangeNotSatisfiable,
        ErrorType.ExpectationFailed,
        ErrorType.PreconditionFailed,
        ErrorType.PreconditionRequired,
        ErrorType.RequestHeadersTooLarge
    };

    #region Factory Methods

    /// <summary>
    /// Creates a DomainErrors instance from a single domain error.
    /// </summary>
    /// <param name="error">The domain error to wrap.</param>
    /// <returns>A DomainErrors instance containing the single error.</returns>
    public static DomainErrors FromSingle(DomainError error) => new([error]);
    
    /// <summary>
    /// Creates a DomainErrors instance from multiple domain errors.
    /// </summary>
    /// <param name="errors">The domain errors to include.</param>
    /// <returns>A DomainErrors instance containing the specified errors.</returns>
    public static DomainErrors Multiple(params DomainError[] errors) => new(errors);
    
    /// <summary>
    /// Creates a DomainErrors instance from a collection of domain errors.
    /// </summary>
    /// <param name="errors">The collection of domain errors to include.</param>
    /// <returns>A DomainErrors instance containing the specified errors.</returns>
    public static DomainErrors Multiple(IEnumerable<DomainError> errors) => new(errors.ToList());

    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Implicitly converts a single <see cref="DomainError"/> to a <see cref="DomainErrors"/> instance.
    /// </summary>
    /// <param name="error">The domain error to convert.</param>
    public static implicit operator DomainErrors(DomainError error) => FromSingle(error);
    
    /// <summary>
    /// Implicitly converts an array of <see cref="DomainError"/> to a <see cref="DomainErrors"/> instance.
    /// </summary>
    /// <param name="errors">The array of domain errors to convert.</param>
    public static implicit operator DomainErrors(DomainError[] errors) => Multiple(errors);
    
    /// <summary>
    /// Implicitly converts a list of <see cref="DomainError"/> to a <see cref="DomainErrors"/> instance.
    /// </summary>
    /// <param name="errors">The list of domain errors to convert.</param>
    public static implicit operator DomainErrors(List<DomainError> errors) => Multiple(errors);

    #endregion

    #region Error Analysis Methods

    /// <summary>
    /// Determines whether the collection contains any errors of the specified type.
    /// </summary>
    /// <param name="type">The error type to check for.</param>
    /// <returns>True if at least one error of the specified type exists; otherwise, false.</returns>
    public bool HasErrorsOfType(ErrorType type) => Errors.Any(e => e.Type == type);
    
    /// <summary>
    /// Gets all errors of the specified type from the collection.
    /// </summary>
    /// <param name="type">The error type to filter by.</param>
    /// <returns>An enumerable of errors matching the specified type.</returns>
    public IEnumerable<DomainError> GetErrorsOfType(ErrorType type) => Errors.Where(e => e.Type == type);

    /// <summary>
    /// Gets the most severe error type from the collection based on predefined severity order.
    /// Server errors (5xx) are considered more severe than client errors (4xx).
    /// </summary>
    /// <returns>The most severe error type, or the default value if no errors exist.</returns>
    public ErrorType GetMostSevereErrorType() => Errors
        .Select(e => e.Type)
        .OrderBy(t => Array.IndexOf(SeverityOrder, t))
        .FirstOrDefault();

    /// <summary>
    /// Creates an aggregate error message from all errors in the collection.
    /// </summary>
    /// <returns>A formatted string containing all error messages.</returns>
    public string GetAggregateErrorMessage() => Errors.Count switch
    {
        0 => "No errors provided",
        1 => Errors[0].Message,
        _ => $"Multiple errors occurred ({Errors.Count}): " +
             string.Join("; ", Errors.Select(e => $"[{e.Type}] {e.Message}"))
    };

    #endregion
}

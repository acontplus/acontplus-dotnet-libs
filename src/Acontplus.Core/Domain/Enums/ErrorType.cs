namespace Acontplus.Core.Domain.Enums;

/// <summary>
/// Classifies the nature of a domain error and drives HTTP status code mapping.
/// Values are grouped into client errors (4xx) and server errors (5xx).
/// </summary>
public enum ErrorType
{
    // ── Client Errors (4xx) ──────────────────────────────────────────────────────
    /// <summary>422 – Business rule or field-level validation failed.</summary>
    Validation,
    /// <summary>400 – Malformed request syntax or invalid parameters.</summary>
    BadRequest,
    /// <summary>404 – Requested resource does not exist.</summary>
    NotFound,
    /// <summary>409 – State conflict (duplicate key, optimistic-concurrency, etc.).</summary>
    Conflict,
    /// <summary>401 – Request requires authentication.</summary>
    Unauthorized,
    /// <summary>403 – Caller is authenticated but not authorised.</summary>
    Forbidden,
    /// <summary>405 – HTTP method not allowed on this endpoint.</summary>
    MethodNotAllowed,
    /// <summary>406 – Client's Accept header cannot be satisfied.</summary>
    NotAcceptable,
    /// <summary>413 – Request payload exceeds the allowed size limit.</summary>
    PayloadTooLarge,
    /// <summary>414 – Request URI is too long.</summary>
    UriTooLong,
    /// <summary>415 – Content-Type is not supported by this endpoint.</summary>
    UnsupportedMediaType,
    /// <summary>416 – Requested byte range is not satisfiable.</summary>
    RangeNotSatisfiable,
    /// <summary>417 – Expect header cannot be met.</summary>
    ExpectationFailed,
    /// <summary>412 – Conditional request precondition failed.</summary>
    PreconditionFailed,
    /// <summary>428 – Request must be conditional (precondition required).</summary>
    PreconditionRequired,
    /// <summary>431 – One or more request header fields are too large.</summary>
    RequestHeadersTooLarge,
    /// <summary>451 – Resource unavailable for legal reasons.</summary>
    UnavailableForLegal,
    /// <summary>429 – Client has sent too many requests in a given time window.</summary>
    RateLimited,

    // ── Server Errors (5xx) ──────────────────────────────────────────────────────
    /// <summary>500 – An unexpected server-side error occurred.</summary>
    Internal,
    /// <summary>501 – Feature or operation is not yet implemented.</summary>
    NotImplemented,
    /// <summary>502 – An upstream/external service returned an invalid response.</summary>
    External,
    /// <summary>503 – Service is temporarily unavailable (maintenance, overload).</summary>
    ServiceUnavailable,
    /// <summary>504 – An upstream service timed out (gateway timeout).</summary>
    Timeout,
    /// <summary>408 – The client request timed out before the server could respond.</summary>
    RequestTimeout,
    /// <summary>505 – HTTP version used in the request is not supported.</summary>
    HttpVersionNotSupported,
    /// <summary>507 – Server is out of storage for the operation.</summary>
    InsufficientStorage,
    /// <summary>508 – Infinite loop detected while processing the request.</summary>
    LoopDetected,
    /// <summary>510 – Further extensions to the request are required.</summary>
    NotExtended,
    /// <summary>511 – Network authentication is required.</summary>
    NetworkAuthRequired
}

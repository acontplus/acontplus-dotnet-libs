namespace Acontplus.Core.Abstractions.Infrastructure.Http;

/// <summary>
/// Abstraction over outbound HTTP calls.
/// Decouples application code from <see cref="System.Net.Http.HttpClient"/> so that
/// implementations can plug in retry policies, circuit-breakers, or mocking without
/// changing the calling code.
/// </summary>
public interface IHttpClientService
{
    // ── GET ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a GET request and deserialises the JSON response body into <typeparamref name="TResponse"/>.
    /// Returns <c>null</c> when the response body is empty or the status code is 204.
    /// </summary>
    Task<TResponse?> GetAsync<TResponse>(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TResponse : class;

    // ── POST ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a POST request with a JSON-serialised body and deserialises the response.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Sends a POST request with a JSON-serialised body without expecting a typed response.
    /// </summary>
    Task PostAsync<TRequest>(
        string url,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class;

    // ── PUT ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a PUT request with a JSON-serialised body and deserialises the response.
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    // ── PATCH ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a PATCH request with a JSON-serialised body and deserialises the response.
    /// </summary>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(
        string url,
        TRequest body,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    // ── DELETE ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a DELETE request. Returns <c>true</c> when the response indicates success.
    /// </summary>
    Task<bool> DeleteAsync(
        string url,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}

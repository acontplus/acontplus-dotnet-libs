namespace Acontplus.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="FilterRequest"/> (and its subtype <see cref="PaginationRequest"/>)
/// providing fluent builders and type-safe filter value retrieval.
/// </summary>
public static class FilterRequestExtensions
{
    // ── Fluent builders ──────────────────────────────────────────────────────────

    /// <summary>Returns a copy of <paramref name="filter"/> with the given search term.</summary>
    public static FilterRequest WithSearch(this FilterRequest filter, string searchTerm)
        => filter with { SearchTerm = searchTerm };

    /// <summary>Returns a copy of <paramref name="filter"/> with the given sort field and direction.</summary>
    public static FilterRequest WithSort(this FilterRequest filter, string sortBy, SortDirection direction = SortDirection.Asc)
        => filter with { SortBy = sortBy, SortDirection = direction };

    /// <summary>Returns a copy of <paramref name="filter"/> with the provided filters merged in (existing keys are overwritten).</summary>
    public static FilterRequest WithFilters(this FilterRequest filter, IReadOnlyDictionary<string, object> filters)
    {
        var merged = filter.Filters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                     ?? new Dictionary<string, object>();
        foreach (var kvp in filters)
            merged[kvp.Key] = kvp.Value;
        return filter with { Filters = merged };
    }

    /// <summary>Returns a copy of <paramref name="filter"/> with a single additional filter entry.</summary>
    public static FilterRequest WithFilter(this FilterRequest filter, string key, object value)
    {
        var merged = filter.Filters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                     ?? new Dictionary<string, object>();
        merged[key] = value;
        return filter with { Filters = merged };
    }

    // ── Value retrieval ──────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a filter value by key and converts it to <typeparamref name="T"/>.
    /// <para>
    /// Conversion priority:
    /// <list type="number">
    ///   <item>Direct cast — fastest path when the stored value is already the right type.</item>
    ///   <item><see cref="Convert.ChangeType(object, Type)"/> — handles all <see cref="IConvertible"/> types
    ///         (int, bool, decimal, DateTime, …) and values stored as strings (query-string origin).</item>
    ///   <item><see cref="TypeDescriptor"/> — handles enums, Guid, and other types with registered converters.</item>
    /// </list>
    /// Nullable value types (e.g. <c>int?</c>, <c>bool?</c>) are supported: the underlying type is
    /// extracted automatically so <c>GetFilterValue&lt;int?&gt;("companyId")</c> works as expected.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Target type, including nullable value types.</typeparam>
    /// <param name="filter">The filter request.</param>
    /// <param name="key">The filter key to retrieve.</param>
    /// <param name="defaultValue">Returned when the key is absent, null, or conversion fails.</param>
    /// <returns>The converted value, or <paramref name="defaultValue"/>.</returns>
    public static T? GetFilterValue<T>(this FilterRequest filter, string key, T? defaultValue = default)
    {
        if (filter.Filters == null || !filter.Filters.TryGetValue(key, out var raw) || raw == null)
            return defaultValue;

        // 1. Direct cast — no allocation, covers exact-type and already-boxed matches
        if (raw is T typed)
            return typed;

        // 2. Unwrap nullable so conversion targets the underlying type (int? → int, etc.)
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var stringValue = raw.ToString();

        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        // 3. Convert.ChangeType — fast for all IConvertible primitives and string-origin values
        try
        {
            return (T)Convert.ChangeType(stringValue, targetType);
        }
        catch { /* fall through */ }

        // 4. TypeDescriptor — enums, Guid, custom type converters
        try
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
                return (T?)converter.ConvertFromInvariantString(stringValue);
        }
        catch { /* fall through */ }

        return defaultValue;
    }

    /// <summary>
    /// Tries to retrieve and convert a filter value by key.
    /// Returns <c>true</c> when the key exists <em>and</em> the value converts successfully to
    /// <typeparamref name="T"/>; <c>false</c> when the key is absent or conversion fails.
    /// </summary>
    /// <typeparam name="T">Target type, including nullable value types.</typeparam>
    /// <param name="filter">The filter request.</param>
    /// <param name="key">The filter key to retrieve.</param>
    /// <param name="value">The converted value when this method returns <c>true</c>; otherwise <c>default</c>.</param>
    public static bool TryGetFilterValue<T>(this FilterRequest filter, string key, out T? value)
    {
        if (filter.Filters == null || !filter.Filters.ContainsKey(key))
        {
            value = default;
            return false;
        }

        value = GetFilterValue<T>(filter, key);
        return value is not null;
    }
}

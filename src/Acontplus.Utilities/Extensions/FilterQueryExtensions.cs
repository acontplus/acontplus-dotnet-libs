using System.ComponentModel;
using Acontplus.Core.Dtos.Requests;
using Acontplus.Utilities.Dtos;

namespace Acontplus.Utilities.Extensions;

/// <summary>
/// Extension methods for FilterQuery to provide helper functionality.
/// </summary>
public static class FilterQueryExtensions
{
    /// <summary>
    /// Gets a filter value by key with type safety and conversion support.
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
    /// <param name="query">The filter query.</param>
    /// <param name="key">The filter key to retrieve.</param>
    /// <param name="defaultValue">Returned when the key is absent, null, or conversion fails.</param>
    /// <returns>The converted value, or <paramref name="defaultValue"/>.</returns>
    public static T? GetFilterValue<T>(this FilterQuery query, string key, T? defaultValue = default)
        => query.Filters.GetFilterValue(key, defaultValue);

    /// <summary>
    /// Tries to get a filter value by key with type safety and conversion support.
    /// Returns <c>true</c> when the key exists <em>and</em> the value converts successfully to
    /// <typeparamref name="T"/>; <c>false</c> when the key is absent or conversion fails.
    /// </summary>
    /// <typeparam name="T">Target type, including nullable value types.</typeparam>
    /// <param name="query">The filter query.</param>
    /// <param name="key">The filter key to retrieve.</param>
    /// <param name="value">The converted value when this method returns <c>true</c>; otherwise <c>default</c>.</param>
    public static bool TryGetFilterValue<T>(this FilterQuery query, string key, out T? value)
    {
        if (query.Filters == null)
        {
            value = default;
            return false;
        }

        return query.Filters.TryGetFilterValue(key, out value);
    }

    /// <summary>
    /// Converts a <see cref="FilterQuery"/> to a <see cref="FilterRequest"/>.
    /// This is a direct property-to-property conversion that replaces the need for
    /// external mapping libraries (e.g., Mapster).
    /// </summary>
    /// <param name="query">The source filter query.</param>
    /// <returns>A new <see cref="FilterRequest"/> with the same filter values.</returns>
    public static FilterRequest ToFilterRequest(this FilterQuery query)
    {
        return new FilterRequest
        {
            SortBy = query.SortBy,
            SortDirection = query.SortDirection ?? SortDirection.Asc,
            SearchTerm = query.SearchTerm,
            Filters = query.Filters
        };
    }

    /// <summary>
    /// Converts a <see cref="PaginationQuery"/> to a <see cref="PaginationRequest"/>.
    /// This is a direct property-to-property conversion that replaces the need for
    /// external mapping libraries (e.g., Mapster).
    /// </summary>
    /// <param name="query">The source pagination query.</param>
    /// <returns>A new <see cref="PaginationRequest"/> with the same pagination and filter values.</returns>
    public static PaginationRequest ToPaginationRequest(this PaginationQuery query)
    {
        return new PaginationRequest
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            SortBy = query.SortBy,
            SortDirection = query.SortDirection ?? SortDirection.Asc,
            SearchTerm = query.SearchTerm,
            Filters = query.Filters
        };
    }
}

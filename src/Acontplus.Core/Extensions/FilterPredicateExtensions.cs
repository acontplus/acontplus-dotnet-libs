using System.Reflection;

namespace Acontplus.Core.Extensions;

/// <summary>
/// Extension methods for converting filters to LINQ expressions.
/// </summary>
public static class FilterPredicateExtensions
{
    /// <summary>
    /// Converts a dictionary of filters to a LINQ expression predicate.
    /// This is a basic implementation that handles common filter types.
    /// For complex scenarios, consider using the Specification pattern.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="filters">The filters dictionary</param>
    /// <returns>A LINQ expression predicate</returns>
    public static Expression<Func<T, bool>> ToPredicate<T>(this IReadOnlyDictionary<string, object>? filters)
    {
        if (filters == null || !filters.Any())
            return x => true;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            var propertyExpression = GetPropertyExpression<T>(parameter, filter.Key);
            if (propertyExpression == null) continue;

            var valueExpression = Expression.Constant(filter.Value);
            var comparisonExpression = CreateComparisonExpression(propertyExpression, valueExpression, filter.Value);

            combinedExpression = combinedExpression == null
                ? comparisonExpression
                : Expression.AndAlso(combinedExpression, comparisonExpression);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

    /// <summary>
    /// Creates a predicate for a specific property and value.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="propertyName">The property name</param>
    /// <param name="value">The filter value</param>
    /// <returns>A LINQ expression predicate</returns>
    public static Expression<Func<T, bool>> CreatePredicate<T>(string propertyName, object value)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyExpression = GetPropertyExpression<T>(parameter, propertyName);

        if (propertyExpression == null)
            return x => true;

        var valueExpression = Expression.Constant(value);
        var comparisonExpression = CreateComparisonExpression(propertyExpression, valueExpression, value);

        return Expression.Lambda<Func<T, bool>>(comparisonExpression, parameter);
    }

    /// <summary>
    /// Creates a predicate for string contains operation.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="propertyName">The property name</param>
    /// <param name="searchTerm">The search term</param>
    /// <returns>A LINQ expression predicate</returns>
    public static Expression<Func<T, bool>> CreateContainsPredicate<T>(string propertyName, string searchTerm)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyExpression = GetPropertyExpression<T>(parameter, propertyName);

        if (propertyExpression == null || propertyExpression.Type != typeof(string))
            return x => true;

        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var searchTermExpression = Expression.Constant(searchTerm, typeof(string));
        var containsExpression = Expression.Call(propertyExpression, containsMethod!, searchTermExpression);

        return Expression.Lambda<Func<T, bool>>(containsExpression, parameter);
    }

    /// <summary>
    /// Creates a predicate for range operations (between two values).
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="propertyName">The property name</param>
    /// <param name="minValue">The minimum value</param>
    /// <param name="maxValue">The maximum value</param>
    /// <returns>A LINQ expression predicate</returns>
    public static Expression<Func<T, bool>> CreateRangePredicate<T>(string propertyName, object minValue, object maxValue)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyExpression = GetPropertyExpression<T>(parameter, propertyName);

        if (propertyExpression == null)
            return x => true;

        var minExpression = Expression.Constant(minValue);
        var maxExpression = Expression.Constant(maxValue);

        var greaterThanOrEqual = Expression.GreaterThanOrEqual(propertyExpression, minExpression);
        var lessThanOrEqual = Expression.LessThanOrEqual(propertyExpression, maxExpression);
        var rangeExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

        return Expression.Lambda<Func<T, bool>>(rangeExpression, parameter);
    }

    private static Expression? GetPropertyExpression<T>(ParameterExpression parameter, string propertyName)
    {
        try
        {
            var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            return property != null ? Expression.Property(parameter, property) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Expression CreateComparisonExpression(Expression propertyExpression, Expression valueExpression, object value)
    {
        // Handle null values
        if (value == null)
        {
            return Expression.Equal(propertyExpression, Expression.Constant(null, propertyExpression.Type));
        }

        // Handle string comparisons (case-insensitive)
        if (propertyExpression.Type == typeof(string) && value is string stringValue)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            var propertyToLower = Expression.Call(propertyExpression, toLowerMethod!);
            var valueToLower = Expression.Constant(stringValue.ToLower(), typeof(string));
            return Expression.Equal(propertyToLower, valueToLower);
        }

        // Handle enum comparisons
        if (propertyExpression.Type.IsEnum && value is string enumString &&
            Enum.TryParse(propertyExpression.Type, enumString, true, out var enumValue))
        {
            return Expression.Equal(propertyExpression, Expression.Constant(enumValue, propertyExpression.Type));
        }

        // Handle boolean comparisons
        if (propertyExpression.Type == typeof(bool) && value is string boolString &&
            bool.TryParse(boolString, out var boolValue))
        {
            return Expression.Equal(propertyExpression, Expression.Constant(boolValue, typeof(bool)));
        }

        // Default equality comparison
        return Expression.Equal(propertyExpression, valueExpression);
    }
}

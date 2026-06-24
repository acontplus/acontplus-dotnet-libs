using System.Reflection;

namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Abstract base class that holds the raw rule data collected during profile configuration.
/// <see cref="MappingExpression{TSource, TTarget}"/> inherits from this class and populates
/// these collections via the fluent API. The expression builder reads from these collections
/// during delegate compilation.
/// </summary>
public abstract class MappingExpressionBase
{
    /// <summary>
    /// Initialises a new <see cref="MappingExpressionBase"/> for the specified type pair.
    /// </summary>
    /// <param name="pair">The source-to-target type pair this expression describes.</param>
    internal MappingExpressionBase(TypePair pair)
    {
        Pair = pair;
        MemberRules = new Dictionary<string, LambdaExpression?>(StringComparer.Ordinal);
        DelegateResolvers = new Dictionary<string, Func<object, object?>>(StringComparer.Ordinal);
        CtorParamRules = new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The source-to-target type pair this expression describes.</summary>
    public TypePair Pair { get; }

    /// <summary>
    /// Custom member mapping rules keyed by destination property name.
    /// A <c>null</c> value indicates an <c>Ignore</c> rule for that member.
    /// </summary>
    public Dictionary<string, LambdaExpression?> MemberRules { get; }

    /// <summary>
    /// Delegate-based resolvers keyed by destination property name.
    /// These store boxed <c>Func&lt;object, object?&gt;</c> wrappers and are incompatible
    /// with <c>ProjectTo</c> because LINQ query providers cannot inspect compiled delegates.
    /// </summary>
    public Dictionary<string, Func<object, object?>> DelegateResolvers { get; }

    /// <summary>
    /// Constructor parameter rules keyed by parameter name (case-insensitive).
    /// Each value is a <see cref="LambdaExpression"/> that resolves the source value
    /// for the named constructor parameter on the target type.
    /// </summary>
    public Dictionary<string, LambdaExpression> CtorParamRules { get; }

    /// <summary>
    /// Indicates whether <c>.ReverseMap()</c> was called, signalling that the inverse
    /// <see cref="TypePair"/> should be registered using name-based convention matching.
    /// </summary>
    public bool HasReverseMap { get; internal set; }

    /// <summary>
    /// Validates that the destination expression resolves to a writable property
    /// (public setter or init-only setter) on the target type.
    /// </summary>
    /// <param name="destinationExpression">The destination member expression to validate.</param>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <returns>The property name extracted from the expression.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the expression does not resolve to a writable property of <typeparamref name="TTarget"/>.
    /// </exception>
    protected static string ValidateDestinationExpression<TTarget, TProperty>(
        Expression<Func<TTarget, TProperty>> destinationExpression)
    {
        if (destinationExpression.Body is not MemberExpression memberExpression)
        {
            throw new InvalidOperationException(
                $"The destination expression must resolve to a property of '{typeof(TTarget).Name}'. " +
                $"Expression '{destinationExpression}' does not resolve to a member access.");
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new InvalidOperationException(
                $"The destination expression must resolve to a property of '{typeof(TTarget).Name}'. " +
                $"Member '{memberExpression.Member.Name}' is not a property.");
        }

        // Check for public setter or init-only setter
        var setter = propertyInfo.GetSetMethod(nonPublic: true);
        if (setter is null)
        {
            throw new InvalidOperationException(
                $"Destination property '{propertyInfo.Name}' on '{typeof(TTarget).Name}' " +
                $"does not have a setter and cannot be mapped.");
        }

        // Accept public setters or init-only setters (which are marked with IsExternalInit)
        var isPublicSetter = setter.IsPublic;
        var isInitOnly = setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

        if (!isPublicSetter && !isInitOnly)
        {
            throw new InvalidOperationException(
                $"Destination property '{propertyInfo.Name}' on '{typeof(TTarget).Name}' " +
                $"does not have a public or init-only setter and cannot be mapped.");
        }

        return propertyInfo.Name;
    }
}

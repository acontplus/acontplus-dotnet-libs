namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Fluent builder for configuring the mapping from
/// <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>.
/// Instantiated by <see cref="MappingProfile.CreateMap{TSource, TTarget}"/>
/// and provides chainable methods for custom member, ignore, constructor, and
/// reverse-map configuration.
/// </summary>
/// <typeparam name="TSource">The source type to map from.</typeparam>
/// <typeparam name="TTarget">The target type to map to.</typeparam>
public sealed class MappingExpression<TSource, TTarget> : MappingExpressionBase
{
    /// <summary>
    /// Initialises a new <see cref="MappingExpression{TSource, TTarget}"/> for the
    /// specified type pair, linked to the owning profile's registration dictionary.
    /// </summary>
    /// <param name="pair">The source-to-target type pair this expression describes.</param>
    /// <param name="registrations">
    /// The owning profile's registration dictionary, used for reverse-map registration.
    /// </param>
    internal MappingExpression(TypePair pair, Dictionary<TypePair, MappingExpressionBase> registrations)
        : base(pair)
    {
        _registrations = registrations;
    }

    private readonly Dictionary<TypePair, MappingExpressionBase> _registrations;

    /// <summary>
    /// Maps a destination member via a source member expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the destination property.</typeparam>
    /// <param name="destination">
    /// An expression selecting the destination property to configure.
    /// Must resolve to a writable property of <typeparamref name="TTarget"/>.
    /// </param>
    /// <param name="source">
    /// An expression selecting the source member whose value will be assigned
    /// to the destination property.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown at configuration time if <paramref name="destination"/> does not resolve
    /// to a writable property of <typeparamref name="TTarget"/>.
    /// </exception>
    public MappingExpression<TSource, TTarget> ForMember<TProperty>(
        Expression<Func<TTarget, TProperty>> destination,
        Expression<Func<TSource, TProperty>> source)
    {
        var propertyName = ValidateDestinationExpression<TTarget, TProperty>(destination);
        MemberRules[propertyName] = source;
        return this;
    }

    /// <summary>
    /// Maps a destination member via an arbitrary delegate resolver.
    /// Note: this overload is incompatible with <c>ProjectTo</c> because LINQ query
    /// providers cannot inspect compiled delegates.
    /// </summary>
    /// <typeparam name="TProperty">The type of the destination property.</typeparam>
    /// <param name="destination">
    /// An expression selecting the destination property to configure.
    /// Must resolve to a writable property of <typeparamref name="TTarget"/>.
    /// </param>
    /// <param name="resolver">
    /// A delegate that computes the destination property value from the source object.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown at configuration time if <paramref name="destination"/> does not resolve
    /// to a writable property of <typeparamref name="TTarget"/>.
    /// </exception>
    public MappingExpression<TSource, TTarget> ForMember<TProperty>(
        Expression<Func<TTarget, TProperty>> destination,
        Func<TSource, TProperty> resolver)
    {
        var propertyName = ValidateDestinationExpression<TTarget, TProperty>(destination);

        // Wrap the typed resolver into a boxed Func<object, object?> for storage
        DelegateResolvers[propertyName] = src => resolver((TSource)src);

        return this;
    }

    /// <summary>
    /// Excludes a destination member from forward-direction mapping.
    /// The exclusion does not propagate to any reverse mapping registered via
    /// <see cref="ReverseMap"/>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the destination property.</typeparam>
    /// <param name="destination">
    /// An expression selecting the destination property to ignore.
    /// Must resolve to a writable property of <typeparamref name="TTarget"/>.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown at configuration time if <paramref name="destination"/> does not resolve
    /// to a writable property of <typeparamref name="TTarget"/>.
    /// </exception>
    public MappingExpression<TSource, TTarget> Ignore<TProperty>(
        Expression<Func<TTarget, TProperty>> destination)
    {
        var propertyName = ValidateDestinationExpression<TTarget, TProperty>(destination);
        MemberRules[propertyName] = null;
        return this;
    }

    /// <summary>
    /// Binds a source member expression to a named constructor parameter on
    /// <typeparamref name="TTarget"/>. Validation that the parameter exists on the
    /// selected constructor occurs at delegate-compilation time, not here.
    /// </summary>
    /// <typeparam name="TProperty">The type of the constructor parameter.</typeparam>
    /// <param name="paramName">
    /// The name of the constructor parameter to bind (case-insensitive matching).
    /// </param>
    /// <param name="source">
    /// An expression selecting the source member whose value will be passed
    /// as the constructor argument.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="paramName"/> is <c>null</c> or empty.
    /// </exception>
    public MappingExpression<TSource, TTarget> ForCtorParam<TProperty>(
        string paramName,
        Expression<Func<TSource, TProperty>> source)
    {
        if (string.IsNullOrWhiteSpace(paramName))
        {
            throw new ArgumentNullException(nameof(paramName),
                "Constructor parameter name cannot be null or empty.");
        }

        CtorParamRules[paramName] = source;
        return this;
    }

    /// <summary>
    /// Registers the inverse <see cref="TypePair"/> (<typeparamref name="TTarget"/> →
    /// <typeparamref name="TSource"/>) using only name-based convention matching.
    /// Forward <c>ForMember</c>, <c>ForCtorParam</c>, and <c>Ignore</c> rules are NOT
    /// carried over to the reverse direction.
    /// </summary>
    /// <returns>This instance for fluent chaining.</returns>
    public MappingExpression<TSource, TTarget> ReverseMap()
    {
        HasReverseMap = true;
        return this;
    }
}

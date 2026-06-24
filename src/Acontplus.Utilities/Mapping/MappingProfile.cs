namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Base class for grouping a set of <c>CreateMap</c> registrations.
/// Derive from this class and register instances via <c>services.AddObjectMapper(profile)</c>.
/// </summary>
/// <remarks>
/// The derived class constructor should call <see cref="CreateMap{TSource, TTarget}"/> one or
/// more times to define the mappings owned by this profile.
/// </remarks>
public abstract class MappingProfile
{
    /// <summary>
    /// Gets the internal collection of registrations keyed by <see cref="TypePair"/>.
    /// This is read by <c>MapperConfiguration</c> during the build phase.
    /// </summary>
    internal Dictionary<TypePair, MappingExpressionBase> Registrations { get; } = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="MappingProfile"/> class.
    /// </summary>
    protected MappingProfile() { }

    /// <summary>
    /// Registers a mapping from <typeparamref name="TSource"/> to <typeparamref name="TTarget"/>
    /// and returns a fluent builder for further configuration.
    /// </summary>
    /// <typeparam name="TSource">The source type to map from.</typeparam>
    /// <typeparam name="TTarget">The target type to map to.</typeparam>
    /// <returns>
    /// A <see cref="MappingExpression{TSource, TTarget}"/> instance for fluent configuration
    /// of member mappings, ignores, constructor parameters, and reverse mapping.
    /// </returns>
    /// <remarks>
    /// Calling this method a second time for the same <see cref="TypePair"/> within the same
    /// profile overwrites the previous registration for that pair, fulfilling requirement 3.1.
    /// <para>
    /// The inverse <see cref="TypePair"/> is NOT registered by this method. Use
    /// <see cref="MappingExpression{TSource, TTarget}.ReverseMap"/> to explicitly register
    /// the reverse direction with convention-only rules (requirement 3.6).
    /// </para>
    /// </remarks>
    protected MappingExpression<TSource, TTarget> CreateMap<TSource, TTarget>()
    {
        var pair = new TypePair(typeof(TSource), typeof(TTarget));
        var expression = new MappingExpression<TSource, TTarget>(pair, Registrations);

        // Overwrite any previous registration for the same pair (requirement 3.1)
        Registrations[pair] = expression;

        return expression;
    }
}

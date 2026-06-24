using System.Reflection;

namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Immutable snapshot of all registered mapping profiles.
/// Call <see cref="Build"/> to validate configurations, compile delegates, and produce
/// a <see cref="MapperRegistry"/>.
/// After <see cref="Build"/> returns, no further profiles may be added.
/// </summary>
public sealed class MapperConfiguration
{
    private readonly Dictionary<TypePair, MappingExpressionBase> _registrations = [];
    private bool _isBuilt;

    /// <summary>
    /// Adds a <see cref="MappingProfile"/> to the configuration.
    /// All registrations from the profile are merged into the internal configuration store.
    /// </summary>
    /// <param name="profile">The profile instance containing mapping registrations.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="profile"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called after <see cref="Build"/> has been invoked, indicating that the
    /// configuration is sealed and no further profiles may be registered.
    /// </exception>
    public MapperConfiguration AddProfile(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (_isBuilt)
        {
            throw new InvalidOperationException(
                "The configuration is sealed. No further profiles may be registered after Build() has been called.");
        }

        foreach (var (pair, expression) in profile.Registrations)
        {
            _registrations[pair] = expression;
        }

        return this;
    }

    /// <summary>
    /// Validates all registered <see cref="TypePair"/>s in a single pass, compiles delegates
    /// for each one, and returns a fully initialised <see cref="MapperRegistry"/>.
    /// Throws a single <see cref="InvalidOperationException"/> listing every violation if
    /// any <c>ForMember</c> target does not exist or has an incompatible resolved type.
    /// Seals this configuration — further calls to <see cref="AddProfile"/> will throw.
    /// </summary>
    /// <returns>A fully initialised <see cref="MapperRegistry"/> ready for mapping operations.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more validation violations are found across registered type pairs.
    /// The message lists every violation found during validation.
    /// </exception>
    public MapperRegistry Build()
    {
        _isBuilt = true;

        // Handle ReverseMap registrations before validation
        var reverseRegistrations = new Dictionary<TypePair, MappingExpressionBase>();

        foreach (var (pair, expression) in _registrations)
        {
            if (expression.HasReverseMap)
            {
                var reversePair = new TypePair(pair.TargetType, pair.SourceType);

                // Only register reverse if not already explicitly registered
                if (!_registrations.ContainsKey(reversePair))
                {
                    reverseRegistrations[reversePair] = new ConventionOnlyExpression(reversePair);
                }
            }
        }

        // Merge reverse registrations into the main set
        foreach (var (pair, expression) in reverseRegistrations)
        {
            _registrations[pair] = expression;
        }

        // Validation phase: collect all violations across all pairs
        var violations = new List<string>();

        foreach (var (pair, expression) in _registrations)
        {
            ValidateExpression(pair, expression, violations);
        }

        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, violations));
        }

        // Compilation phase: create the registry and register pairs
        var registry = new MapperRegistry();

        // Compilation hook: if a compile delegate is provided, use it to compile each pair.
        // Otherwise, pairs are registered for later compilation when ExpressionBuilder is available.
        if (CompileDelegate is not null)
        {
            foreach (var (pair, expression) in _registrations)
            {
                var compiledDelegate = CompileDelegate(pair, expression, registry);
                registry.Register(pair, compiledDelegate);
                registry.RegisterConfiguration(pair, expression);
            }
        }
        else
        {
            // Store configurations even without compilation so ProjectTo can access them
            foreach (var (pair, expression) in _registrations)
            {
                registry.RegisterConfiguration(pair, expression);
            }
        }

        return registry;
    }

    /// <summary>
    /// Internal hook for delegate compilation. Set by the ExpressionBuilder/DelegateCompiler
    /// subsystem when it is available. When null, Build() validates and creates the registry
    /// without compiling delegates (pairs will be compiled on first use via convention mapping).
    /// </summary>
    internal Func<TypePair, MappingExpressionBase?, MapperRegistry, Delegate>? CompileDelegate { get; set; }

    /// <summary>
    /// Gets the merged registrations dictionary. Used internally for testing and by
    /// future compilation infrastructure.
    /// </summary>
    internal IReadOnlyDictionary<TypePair, MappingExpressionBase> Registrations => _registrations;

    private static void ValidateExpression(
        TypePair pair,
        MappingExpressionBase expression,
        List<string> violations)
    {
        var targetType = pair.TargetType;
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Validate MemberRules (expression-based ForMember rules)
        foreach (var (memberName, lambdaExpression) in expression.MemberRules)
        {
            var destinationProperty = Array.Find(targetProperties,
                p => string.Equals(p.Name, memberName, StringComparison.Ordinal));

            if (destinationProperty is null)
            {
                violations.Add(
                    $"{pair}: member '{memberName}' does not exist on {targetType.Name}");
                continue;
            }

            // For non-null lambda expressions (null means Ignore rule), validate type compatibility
            if (lambdaExpression is not null)
            {
                var resolvedType = lambdaExpression.ReturnType;
                var destinationType = destinationProperty.PropertyType;

                if (!IsAssignableTo(resolvedType, destinationType))
                {
                    violations.Add(
                        $"{pair}: member '{memberName}' — resolved type '{resolvedType.Name}' is not assignable to destination type '{destinationType.Name}'");
                }
            }
        }

        // Validate DelegateResolvers (delegate-based ForMember rules)
        // For delegates, we can only verify the destination property exists (can't type-check boxed delegates)
        foreach (var (memberName, _) in expression.DelegateResolvers)
        {
            var destinationProperty = Array.Find(targetProperties,
                p => string.Equals(p.Name, memberName, StringComparison.Ordinal));

            if (destinationProperty is null)
            {
                violations.Add(
                    $"{pair}: member '{memberName}' does not exist on {targetType.Name}");
            }
        }
    }

    private static bool IsAssignableTo(Type sourceType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(sourceType))
        {
            return true;
        }

        // Handle Nullable<T> destination: source T is assignable to Nullable<T>
        if (Nullable.GetUnderlyingType(destinationType) is { } underlyingDest
            && underlyingDest.IsAssignableFrom(sourceType))
        {
            return true;
        }

        // Handle Nullable<T> source: Nullable<T> underlying is assignable to destination
        if (Nullable.GetUnderlyingType(sourceType) is { } underlyingSrc
            && destinationType.IsAssignableFrom(underlyingSrc))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Internal expression class for convention-only reverse-map registrations.
    /// Contains no custom rules — all members map by name convention.
    /// </summary>
    private sealed class ConventionOnlyExpression : MappingExpressionBase
    {
        internal ConventionOnlyExpression(TypePair pair) : base(pair) { }
    }
}

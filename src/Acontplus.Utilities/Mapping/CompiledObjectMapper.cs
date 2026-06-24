using Acontplus.Utilities.Mapping.Internal;

namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Default implementation of <see cref="IObjectMapper"/> backed by pre-compiled
/// <c>Expression</c>-tree delegates. Register as a singleton via
/// <c>services.AddObjectMapper(...)</c>.
/// </summary>
public sealed class CompiledObjectMapper : IObjectMapper
{
    private readonly MapperRegistry _registry;

    /// <summary>Initialises the mapper with a fully built <see cref="MapperRegistry"/>.</summary>
    /// <param name="registry">
    /// A fully initialised registry containing compiled delegates for all registered type pairs.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registry"/> is <c>null</c>.
    /// </exception>
    public CompiledObjectMapper(MapperRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Exposes the internal registry for use by the backward-compatibility static shim
    /// when registering late <c>CreateMap</c> calls after <c>Build()</c>.
    /// </summary>
    internal MapperRegistry Registry => _registry;

    /// <inheritdoc />
    public TTarget Map<TSource, TTarget>(TSource source)
    {
        if (source is null)
            return default!;

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var pair = new TypePair(sourceType, targetType);

        // Try declared types first (fast path for explicitly registered pairs)
        if (_registry.TryGet(pair, out var directDelegate))
        {
            var func = (Func<TSource, TTarget>)directDelegate!;
            return func(source);
        }

        // Fall back to runtime type of source (for interface/base class declared types)
        var runtimeSourceType = source.GetType();
        if (runtimeSourceType != sourceType)
        {
            var runtimePair = new TypePair(runtimeSourceType, targetType);
            var runtimeDelegate = _registry.GetOrAdd(
                runtimePair,
                p => DelegateCompiler.CompileConvention(p, _registry));

            // Use DynamicInvoke since the delegate is Func<RuntimeType, TTarget> not Func<TSource, TTarget>
            var result = runtimeDelegate.DynamicInvoke(source);
            return (TTarget)result!;
        }

        // No existing registration and runtime == declared, compile convention on demand
        var conventionDelegate = _registry.GetOrAdd(
            pair,
            p => DelegateCompiler.CompileConvention(p, _registry));

        var conventionFunc = (Func<TSource, TTarget>)conventionDelegate;
        return conventionFunc(source);
    }

    /// <inheritdoc />
    public TTarget Map<TSource, TTarget>(TSource source, TTarget destination)
    {
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));

        if (source is null)
            return destination;

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var pair = new TypePair(sourceType, targetType);

        // Map source to a fresh instance using the compiled delegate
        var mapped = MapInternal<TSource, TTarget>(source, pair);

        // Copy all writable properties from the mapped result onto the destination
        CopyProperties(mapped, destination);

        return destination;
    }

    /// <inheritdoc />
    public IEnumerable<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source)
    {
        if (source is null)
            return [];

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var pair = new TypePair(sourceType, targetType);

        // Resolve the element mapping delegate once
        var elementDelegate = ResolveDelegate<TSource, TTarget>(pair);

        return source.Select(elementDelegate);
    }

    /// <inheritdoc />
    public IQueryable<TTarget> ProjectTo<TSource, TTarget>(IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var pair = new TypePair(sourceType, targetType);

        // Try to get explicit config from registry's configurations (if available)
        _registry.TryGetConfiguration(pair, out var config);

        // Build projection expression
        LambdaExpression projectionExpr;
        try
        {
            projectionExpr = ExpressionBuilder.BuildProjectionExpression(pair, config);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"No registration and no valid convention projection exists for {pair}.");
        }

        // Cast to Expression<Func<TSource, TTarget>> and pass to Select
        var typedExpression = (Expression<Func<TSource, TTarget>>)projectionExpr;
        return source.Select(typedExpression);
    }

    /// <summary>
    /// Resolves and invokes the compiled delegate for the specified type pair.
    /// Falls back to runtime type resolution when the declared source type differs
    /// from the actual runtime type.
    /// </summary>
    private TTarget MapInternal<TSource, TTarget>(TSource source, TypePair pair)
    {
        // Try declared types first
        if (_registry.TryGet(pair, out var directDelegate))
        {
            var func = (Func<TSource, TTarget>)directDelegate!;
            return func(source);
        }

        // Fall back to runtime type of source
        var runtimeSourceType = source!.GetType();
        if (runtimeSourceType != pair.SourceType)
        {
            var runtimePair = new TypePair(runtimeSourceType, pair.TargetType);
            var runtimeDelegate = _registry.GetOrAdd(
                runtimePair,
                p => DelegateCompiler.CompileConvention(p, _registry));

            var result = runtimeDelegate.DynamicInvoke(source);
            return (TTarget)result!;
        }

        // Compile convention on demand
        var conventionDelegate = _registry.GetOrAdd(
            pair,
            p => DelegateCompiler.CompileConvention(p, _registry));

        var conventionFunc = (Func<TSource, TTarget>)conventionDelegate;
        return conventionFunc(source);
    }

    /// <summary>
    /// Resolves the typed mapping delegate for the specified type pair.
    /// Uses the registry cache or compiles a convention delegate on demand.
    /// </summary>
    private Func<TSource, TTarget> ResolveDelegate<TSource, TTarget>(TypePair pair)
    {
        if (_registry.TryGet(pair, out var existingDelegate))
        {
            return (Func<TSource, TTarget>)existingDelegate!;
        }

        var conventionDelegate = _registry.GetOrAdd(
            pair,
            p => DelegateCompiler.CompileConvention(p, _registry));

        return (Func<TSource, TTarget>)conventionDelegate;
    }

    /// <summary>
    /// Copies all writable public properties from <paramref name="source"/> to
    /// <paramref name="destination"/>. Used by the "map into existing" overload to
    /// apply mapped values onto an existing target instance.
    /// </summary>
    private static void CopyProperties<TTarget>(TTarget source, TTarget destination)
    {
        if (source is null)
            return;

        var properties = typeof(TTarget).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite)
                continue;

            var setter = prop.GetSetMethod();
            if (setter is null)
                continue;

            var value = prop.GetValue(source);
            prop.SetValue(destination, value);
        }
    }
}

using System.Reflection;
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

    // Separate cache for compiled copy-delegates used by the into-existing Map overload.
    // Key: TypePair(T, T) — source and target are the same type.
    // Value: Action<T, T> compiled once from an expression tree.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Delegate>
        _copyDelegateCache = new();

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

    /// <inheritdoc />
    public TTarget Map<TSource, TTarget>(TSource source)
    {
        if (source is null)
            return default!;

        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var pair = new TypePair(sourceType, targetType);

        // Fast path — explicitly registered or already cached convention pair
        if (_registry.TryGet(pair, out var directDelegate))
        {
            var func = (Func<TSource, TTarget>)directDelegate!;
            return func(source);
        }

        // Fall back to runtime type of source (interface / base-class declared types)
        var runtimeSourceType = source.GetType();
        if (runtimeSourceType != sourceType)
        {
            var runtimePair = new TypePair(runtimeSourceType, targetType);
            var runtimeDelegate = _registry.GetOrAdd(
                runtimePair,
                p => DelegateCompiler.CompileConvention(p, _registry));

            // DynamicInvoke is unavoidable here: the delegate is Func<ConcreteType, TTarget>
            // and we only know ConcreteType at runtime.
            var result = runtimeDelegate.DynamicInvoke(source);
            return (TTarget)result!;
        }

        // Unregistered pair whose source runtime type equals the declared type — compile once
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

        var pair = new TypePair(typeof(TSource), typeof(TTarget));

        // Map source to a fresh instance then copy onto the caller-supplied destination.
        var mapped = MapInternal<TSource, TTarget>(source, pair);

        // Copy via a compiled Action<TTarget, TTarget> — zero per-call reflection.
        GetOrCompileCopyDelegate<TTarget>()(mapped, destination);

        return destination;
    }

    /// <inheritdoc />
    public IEnumerable<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source)
    {
        if (source is null)
            return [];

        var pair = new TypePair(typeof(TSource), typeof(TTarget));
        var elementDelegate = ResolveDelegate<TSource, TTarget>(pair);

        return source.Select(elementDelegate);
    }

    /// <inheritdoc />
    public IQueryable<TTarget> ProjectTo<TSource, TTarget>(IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        var pair = new TypePair(typeof(TSource), typeof(TTarget));

        _registry.TryGetConfiguration(pair, out var config);

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

        var typedExpression = (Expression<Func<TSource, TTarget>>)projectionExpr;
        return source.Select(typedExpression);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private TTarget MapInternal<TSource, TTarget>(TSource source, TypePair pair)
    {
        if (_registry.TryGet(pair, out var directDelegate))
            return ((Func<TSource, TTarget>)directDelegate!)(source);

        var runtimeSourceType = source!.GetType();
        if (runtimeSourceType != pair.SourceType)
        {
            var runtimePair = new TypePair(runtimeSourceType, pair.TargetType);
            var runtimeDelegate = _registry.GetOrAdd(
                runtimePair,
                p => DelegateCompiler.CompileConvention(p, _registry));

            return (TTarget)runtimeDelegate.DynamicInvoke(source)!;
        }

        var conventionDelegate = _registry.GetOrAdd(
            pair,
            p => DelegateCompiler.CompileConvention(p, _registry));

        return ((Func<TSource, TTarget>)conventionDelegate)(source);
    }

    private Func<TSource, TTarget> ResolveDelegate<TSource, TTarget>(TypePair pair)
    {
        if (_registry.TryGet(pair, out var existing))
            return (Func<TSource, TTarget>)existing!;

        var compiled = _registry.GetOrAdd(
            pair,
            p => DelegateCompiler.CompileConvention(p, _registry));

        return (Func<TSource, TTarget>)compiled;
    }

    /// <summary>
    /// Returns a compiled <c>Action&lt;T, T&gt;</c> that copies all writable public
    /// properties from the first argument onto the second. The delegate is built once
    /// from an <see cref="Expression"/> tree and cached per type — zero reflection at
    /// call time.
    /// </summary>
    private Action<TTarget, TTarget> GetOrCompileCopyDelegate<TTarget>()
    {
        var del = _copyDelegateCache.GetOrAdd(typeof(TTarget), static t =>
        {
            // (TTarget src, TTarget dst) => { dst.P1 = src.P1; dst.P2 = src.P2; ... }
            var src  = Expression.Parameter(t, "src");
            var dst  = Expression.Parameter(t, "dst");

            var assignments = t
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p =>
                {
                    if (!p.CanRead || !p.CanWrite) return false;
                    var setter = p.GetSetMethod();
                    return setter is not null && setter.IsPublic;
                })
                .Select(p => (Expression)Expression.Assign(
                    Expression.Property(dst, p),
                    Expression.Property(src, p)));

            var body  = Expression.Block(assignments);
            var lambda = Expression.Lambda<Action<TTarget, TTarget>>(body, src, dst);
            return lambda.Compile();
        });

        return (Action<TTarget, TTarget>)del;
    }
}

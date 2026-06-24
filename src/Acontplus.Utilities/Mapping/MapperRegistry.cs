using System.Collections.Concurrent;

namespace Acontplus.Utilities.Mapping;

/// <summary>
/// Thread-safe store for compiled <c>Func&lt;TSource, TTarget&gt;</c> mapping delegates,
/// keyed by <see cref="TypePair"/>. After startup, all operations are lock-free concurrent reads.
/// Unregistered pairs are compiled exactly once on first use via <see cref="Lazy{T}"/> factories.
/// </summary>
public sealed class MapperRegistry
{
    private readonly ConcurrentDictionary<TypePair, Lazy<Delegate>> _delegates = new();
    private readonly ConcurrentDictionary<TypePair, MappingExpressionBase?> _configurations = new();

    /// <summary>
    /// Registers a pre-compiled delegate for the specified <paramref name="pair"/>.
    /// Overwrites any existing entry for the same <see cref="TypePair"/>.
    /// </summary>
    /// <param name="pair">The source-to-target type pair identifying the mapping route.</param>
    /// <param name="compiledDelegate">The compiled mapping delegate to store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="compiledDelegate"/> is <c>null</c>.
    /// </exception>
    internal void Register(TypePair pair, Delegate compiledDelegate)
    {
        ArgumentNullException.ThrowIfNull(compiledDelegate);
        _delegates[pair] = new Lazy<Delegate>(compiledDelegate);
    }

    /// <summary>
    /// Returns the compiled delegate for the specified <paramref name="pair"/>.
    /// If the pair is not yet registered, the <paramref name="factory"/> is invoked exactly once
    /// (even under concurrent access) to produce the delegate, which is then cached for all
    /// subsequent calls.
    /// </summary>
    /// <param name="pair">The source-to-target type pair identifying the mapping route.</param>
    /// <param name="factory">
    /// A factory function that compiles and returns a mapping delegate for the given
    /// <see cref="TypePair"/>. Invoked at most once per <paramref name="pair"/>.
    /// </param>
    /// <returns>The compiled mapping delegate for the specified <paramref name="pair"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <paramref name="factory"/> cannot produce a valid delegate for the pair
    /// (e.g., no satisfiable constructor, no matched members for convention mapping).
    /// </exception>
    internal Delegate GetOrAdd(TypePair pair, Func<TypePair, Delegate> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var lazy = _delegates.GetOrAdd(
            pair,
            static (key, f) => new Lazy<Delegate>(() => f(key)),
            factory);

        return lazy.Value;
    }

    /// <summary>
    /// Attempts to retrieve the compiled delegate for the specified <paramref name="pair"/>.
    /// </summary>
    /// <param name="pair">The source-to-target type pair identifying the mapping route.</param>
    /// <param name="compiledDelegate">
    /// When this method returns <c>true</c>, contains the compiled delegate for the pair;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a compiled delegate exists (or has been lazily initialised) for the
    /// specified <paramref name="pair"/>; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGet(TypePair pair, out Delegate? compiledDelegate)
    {
        if (_delegates.TryGetValue(pair, out var lazy))
        {
            compiledDelegate = lazy.Value;
            return true;
        }

        compiledDelegate = null;
        return false;
    }

    /// <summary>
    /// Stores the <see cref="MappingExpressionBase"/> configuration for the specified
    /// <paramref name="pair"/>. Used by <c>ProjectTo</c> to retrieve member rules
    /// when building projection expressions.
    /// </summary>
    /// <param name="pair">The source-to-target type pair identifying the mapping route.</param>
    /// <param name="config">
    /// The configuration to store, or <c>null</c> for convention-only mappings.
    /// </param>
    internal void RegisterConfiguration(TypePair pair, MappingExpressionBase? config)
    {
        _configurations[pair] = config;
    }

    /// <summary>
    /// Attempts to retrieve the <see cref="MappingExpressionBase"/> configuration for the
    /// specified <paramref name="pair"/>.
    /// </summary>
    /// <param name="pair">The source-to-target type pair identifying the mapping route.</param>
    /// <param name="config">
    /// When this method returns <c>true</c>, contains the configuration for the pair;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if a configuration exists for the specified <paramref name="pair"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetConfiguration(TypePair pair, out MappingExpressionBase? config)
    {
        return _configurations.TryGetValue(pair, out config);
    }
}

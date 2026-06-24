namespace Acontplus.Utilities.Mapping.Internal;

/// <summary>
/// Compiles <see cref="Expression"/> trees into <c>Func&lt;TSource, TTarget&gt;</c> delegates.
/// Handles flat, nested, collection, and constructor-based mappings.
/// </summary>
internal static class DelegateCompiler
{
    /// <summary>
    /// Compiles the provided expression tree into a typed delegate and returns it
    /// wrapped as a <see cref="Delegate"/>.
    /// </summary>
    /// <param name="expression">
    /// A <see cref="LambdaExpression"/> previously built by <see cref="ExpressionBuilder"/>.
    /// </param>
    /// <returns>The compiled delegate ready for invocation.</returns>
    internal static Delegate Compile(LambdaExpression expression)
    {
        return expression.Compile();
    }

    /// <summary>
    /// Builds and compiles a delegate for a convention-based <see cref="TypePair"/>
    /// with no explicit <see cref="MappingExpressionBase"/> configuration.
    /// </summary>
    /// <param name="pair">The source-to-target type pair to build a convention mapping for.</param>
    /// <param name="registry">
    /// The mapper registry used to resolve nested pair delegates during expression building.
    /// </param>
    /// <returns>The compiled convention mapping delegate.</returns>
    internal static Delegate CompileConvention(TypePair pair, MapperRegistry registry)
    {
        var expression = ExpressionBuilder.BuildMappingExpression(
            pair,
            config: null,
            registry,
            new HashSet<TypePair>());

        return expression.Compile();
    }
}

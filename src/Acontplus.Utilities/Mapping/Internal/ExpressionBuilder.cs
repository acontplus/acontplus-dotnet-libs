using System.Reflection;

namespace Acontplus.Utilities.Mapping.Internal;

/// <summary>
/// Builds <see cref="Expression"/> trees for mapping and projection operations.
/// Does not perform compilation — returns <see cref="LambdaExpression"/> instances only.
/// </summary>
internal static class ExpressionBuilder
{
    /// <summary>
    /// Set of types considered "simple" for the purposes of flat mapping.
    /// Includes numeric primitives, bool, char, string, decimal, DateTime, DateTimeOffset,
    /// TimeSpan, Guid, enums, and their <see cref="Nullable{T}"/> wrappers.
    /// </summary>
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float), typeof(double),
        typeof(bool), typeof(char),
        typeof(string), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset),
        typeof(TimeSpan), typeof(Guid)
    ];

    /// <summary>
    /// Builds a <see cref="LambdaExpression"/> for the given <see cref="TypePair"/> and optional
    /// <see cref="MappingExpressionBase"/> configuration. Handles flat member mappings with
    /// case-insensitive name matching, type conversion, ForMember rules, delegate resolvers,
    /// and Ignore rules. When the target type lacks a parameterless constructor, applies
    /// constructor-parameter mapping with priority-based argument resolution.
    /// </summary>
    /// <param name="pair">The source-to-target type pair to build the expression for.</param>
    /// <param name="config">
    /// Optional configuration containing custom member rules, delegate resolvers, and ignore rules.
    /// When <c>null</c>, convention-only mapping is applied.
    /// </param>
    /// <param name="registry">
    /// The mapper registry for resolving nested pair delegates. Not used for flat mapping
    /// but accepted for future nested/cycle support.
    /// </param>
    /// <param name="inProgress">
    /// Set of type pairs currently being compiled, used for cycle detection.
    /// Not used for flat mapping but accepted for future nested support.
    /// </param>
    /// <returns>
    /// A <see cref="LambdaExpression"/> of the form <c>(TSource source) => new TTarget { ... }</c>
    /// with member init bindings for all resolvable destination properties.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the target type has no parameterless constructor and no public constructor
    /// is fully satisfiable, or when a <c>ForCtorParam</c> rule references a parameter that
    /// does not exist on any constructor of the target type.
    /// </exception>
    internal static LambdaExpression BuildMappingExpression(
        TypePair pair,
        MappingExpressionBase? config,
        MapperRegistry registry,
        HashSet<TypePair> inProgress)
    {
        var sourceType = pair.SourceType;
        var targetType = pair.TargetType;

        var sourceParam = Expression.Parameter(sourceType, "source");

        var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Determine whether to use parameterless constructor or constructor-parameter mapping
        var hasParameterlessCtor = targetType.GetConstructor(Type.EmptyTypes) is not null;
        var hasCtorParamRules = config?.CtorParamRules.Count > 0;

        if (hasParameterlessCtor && !hasCtorParamRules)
        {
            // Existing behaviour: use parameterless constructor with MemberInit
            return BuildParameterlessCtorExpression(
                sourceParam, sourceProperties, targetType, targetProperties, config, registry, inProgress);
        }

        // Constructor-parameter mapping path
        return BuildConstructorMappingExpression(
            pair, sourceParam, sourceProperties, targetType, targetProperties, config, registry, inProgress);
    }

    /// <summary>
    /// Builds a mapping expression using the parameterless constructor and member init bindings.
    /// This is the original flat-mapping path.
    /// </summary>
    private static LambdaExpression BuildParameterlessCtorExpression(
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Type targetType,
        PropertyInfo[] targetProperties,
        MappingExpressionBase? config,
        MapperRegistry registry,
        HashSet<TypePair> inProgress)
    {
        var bindings = new List<MemberBinding>();

        foreach (var destProp in targetProperties)
        {
            if (!HasWritableSetter(destProp))
                continue;

            var binding = BuildMemberBinding(
                destProp, sourceParam, sourceProperties, config, registry, inProgress);

            if (binding is not null)
                bindings.Add(binding);
        }

        var newExpr = Expression.New(targetType);
        var memberInit = Expression.MemberInit(newExpr, bindings);
        var lambda = Expression.Lambda(memberInit, sourceParam);

        return lambda;
    }

    /// <summary>
    /// Builds a mapping expression using constructor-parameter mapping.
    /// Selects the best public constructor, resolves arguments, and wraps with
    /// <see cref="MemberInitExpression"/> for remaining settable properties.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no public constructor is fully satisfiable or when a <c>ForCtorParam</c>
    /// rule references a non-existent parameter.
    /// </exception>
    private static LambdaExpression BuildConstructorMappingExpression(
        TypePair pair,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Type targetType,
        PropertyInfo[] targetProperties,
        MappingExpressionBase? config,
        MapperRegistry registry,
        HashSet<TypePair> inProgress)
    {
        var ctorParamRules = config?.CtorParamRules
            ?? new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase);

        // Validate ForCtorParam rules reference existing parameters on at least one constructor
        ValidateCtorParamRules(pair, targetType, ctorParamRules);

        // Select the best constructor
        var (selectedCtor, argExpressions) = SelectBestConstructor(
            pair, targetType, sourceParam, sourceProperties, ctorParamRules);

        // Build the NewExpression with constructor arguments
        var newExpr = Expression.New(selectedCtor, argExpressions);

        // Determine which property names are already covered by constructor parameters
        var ctorParamNames = new HashSet<string>(
            selectedCtor.GetParameters().Select(p => p.Name!),
            StringComparer.OrdinalIgnoreCase);

        // Build member bindings for remaining settable properties NOT covered by constructor
        var bindings = new List<MemberBinding>();

        foreach (var destProp in targetProperties)
        {
            // Skip properties already set via constructor parameter
            if (ctorParamNames.Contains(destProp.Name))
                continue;

            // Skip init-only properties — these can only be set via constructor
            if (IsInitOnlySetter(destProp))
                continue;

            // Skip non-writable properties
            if (!HasWritableSetter(destProp))
                continue;

            var binding = BuildMemberBinding(
                destProp, sourceParam, sourceProperties, config, registry, inProgress);

            if (binding is not null)
                bindings.Add(binding);
        }

        // If there are remaining bindings, wrap with MemberInitExpression
        Expression body;
        if (bindings.Count > 0)
        {
            body = Expression.MemberInit(newExpr, bindings);
        }
        else
        {
            body = newExpr;
        }

        return Expression.Lambda(body, sourceParam);
    }

    /// <summary>
    /// Validates that all <c>ForCtorParam</c> rule names correspond to an actual parameter
    /// on at least one public constructor of the target type. Throws
    /// <see cref="InvalidOperationException"/> if a rule names a non-existent parameter.
    /// </summary>
    private static void ValidateCtorParamRules(
        TypePair pair,
        Type targetType,
        Dictionary<string, LambdaExpression> ctorParamRules)
    {
        if (ctorParamRules.Count == 0)
            return;

        var constructors = targetType.GetConstructors();
        var allParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ctor in constructors)
        {
            foreach (var param in ctor.GetParameters())
            {
                if (param.Name is not null)
                    allParamNames.Add(param.Name);
            }
        }

        foreach (var ruleName in ctorParamRules.Keys)
        {
            if (!allParamNames.Contains(ruleName))
            {
                throw new InvalidOperationException(
                    $"{pair}: ForCtorParam rule names parameter '{ruleName}' which does not exist on any constructor of '{targetType.Name}'");
            }
        }
    }

    /// <summary>
    /// Selects the best public constructor for the target type based on the number of
    /// satisfiable parameters. A constructor is "fully satisfiable" when ALL its parameters
    /// can be resolved. Among fully satisfiable constructors, the one with the most parameters
    /// is preferred. Ties are broken by <see cref="Type.GetConstructors()"/> order (first wins).
    /// </summary>
    /// <returns>
    /// A tuple of the selected constructor and its resolved argument expressions.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no public constructor is fully satisfiable.
    /// </exception>
    private static (ConstructorInfo Constructor, Expression[] Arguments) SelectBestConstructor(
        TypePair pair,
        Type targetType,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Dictionary<string, LambdaExpression> ctorParamRules)
    {
        var constructors = targetType.GetConstructors();

        ConstructorInfo? bestCtor = null;
        Expression[]? bestArgs = null;
        var bestParamCount = -1;

        // Track unsatisfied parameters for error reporting
        List<string>? unsatisfiedParams = null;

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new Expression[parameters.Length];
            var allSatisfied = true;
            var currentUnsatisfied = new List<string>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var argExpr = ResolveConstructorParameter(
                    param, sourceParam, sourceProperties, ctorParamRules);

                if (argExpr is not null)
                {
                    args[i] = argExpr;
                }
                else
                {
                    allSatisfied = false;
                    currentUnsatisfied.Add(param.Name ?? $"arg{i}");
                }
            }

            if (allSatisfied && parameters.Length > bestParamCount)
            {
                bestCtor = ctor;
                bestArgs = args;
                bestParamCount = parameters.Length;
            }

            // Track the constructor with most parameters for error message
            if (!allSatisfied && (unsatisfiedParams is null || parameters.Length > (unsatisfiedParams.Count + bestParamCount)))
            {
                unsatisfiedParams = currentUnsatisfied;
            }
        }

        if (bestCtor is null || bestArgs is null)
        {
            var paramList = unsatisfiedParams is not null
                ? string.Join(", ", unsatisfiedParams)
                : "unknown";

            throw new InvalidOperationException(
                $"Cannot create an instance of '{targetType.Name}'. No public constructor is fully satisfiable. Unsatisfied parameters: {paramList}");
        }

        return (bestCtor, bestArgs);
    }

    /// <summary>
    /// Resolves a single constructor parameter's argument expression using the priority order:
    /// <c>ForCtorParam</c> rule → convention name match → declared default value.
    /// Returns <c>null</c> if the parameter cannot be resolved.
    /// </summary>
    private static Expression? ResolveConstructorParameter(
        ParameterInfo param,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Dictionary<string, LambdaExpression> ctorParamRules)
    {
        var paramName = param.Name ?? string.Empty;
        var paramType = param.ParameterType;

        // Priority 1: ForCtorParam rule (case-insensitive match on name)
        if (ctorParamRules.TryGetValue(paramName, out var ctorRule))
        {
            var ruleBody = ReplaceParameter(ctorRule.Body, ctorRule.Parameters[0], sourceParam);
            return EnsureType(ruleBody, paramType);
        }

        // Priority 2: Convention name match — source property matches by name (case-insensitive)
        var matchingSourceProps = sourceProperties
            .Where(sp => string.Equals(sp.Name, paramName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingSourceProps.Length == 1)
        {
            var sourceProp = matchingSourceProps[0];

            // Check type assignability
            if (IsAssignableForCtorParam(sourceProp.PropertyType, paramType))
            {
                var sourceAccess = Expression.Property(sourceParam, sourceProp);
                return BuildCtorParamValueExpression(sourceAccess, sourceProp.PropertyType, paramType);
            }
        }

        // Priority 3: Declared default value
        if (param.HasDefaultValue)
        {
            return Expression.Constant(param.DefaultValue, paramType);
        }

        // Cannot resolve
        return null;
    }

    /// <summary>
    /// Determines whether a source type is assignable to a constructor parameter type,
    /// considering direct assignability, nullable conversions, and numeric conversions.
    /// </summary>
    private static bool IsAssignableForCtorParam(Type sourceType, Type paramType)
    {
        if (paramType.IsAssignableFrom(sourceType))
            return true;

        // Handle Nullable<T> → T and T → Nullable<T>
        var srcUnderlying = Nullable.GetUnderlyingType(sourceType);
        var paramUnderlying = Nullable.GetUnderlyingType(paramType);

        if (paramUnderlying is not null && paramUnderlying.IsAssignableFrom(sourceType))
            return true;

        if (srcUnderlying is not null && paramType.IsAssignableFrom(srcUnderlying))
            return true;

        // Numeric/convertible compatibility
        var effectiveSource = srcUnderlying ?? sourceType;
        var effectiveParam = paramUnderlying ?? paramType;

        if (IsNumericOrConvertible(effectiveSource) && IsNumericOrConvertible(effectiveParam))
            return true;

        // Simple type conversions via Convert.ChangeType
        if (IsSimpleType(sourceType) && IsSimpleType(paramType))
            return true;

        return false;
    }

    /// <summary>
    /// Builds the value expression for a constructor parameter from a matched source property,
    /// handling type conversion and null safety for value-type parameters.
    /// </summary>
    private static Expression BuildCtorParamValueExpression(
        Expression sourceAccess,
        Type sourceType,
        Type paramType)
    {
        // Build conversion expression
        var conversionExpr = BuildTypeConversionExpression(sourceAccess, sourceType, paramType);
        if (conversionExpr is null)
        {
            // Fallback to simple convert
            conversionExpr = Expression.Convert(sourceAccess, paramType);
        }

        // If source is a non-nullable value type, no null guard needed
        if (sourceType.IsValueType && Nullable.GetUnderlyingType(sourceType) is null)
            return conversionExpr;

        // For nullable source → non-nullable value type parameter: use default when null
        if (paramType.IsValueType && Nullable.GetUnderlyingType(paramType) is null)
        {
            return BuildNullGuardedExpression(sourceAccess, conversionExpr, sourceType, paramType);
        }

        // For nullable source → nullable/ref param: pass null through
        return BuildNullGuardedExpression(sourceAccess, conversionExpr, sourceType, paramType);
    }

    /// <summary>
    /// Determines whether a property has an init-only setter (not a regular public setter).
    /// A property is considered init-only if it has a setter marked with the
    /// <c>IsExternalInit</c> modifier but that setter is NOT public.
    /// Properties with public setters that are also init (e.g., <c>public init</c>) are
    /// treated as regular writable properties.
    /// </summary>
    private static bool IsInitOnlySetter(PropertyInfo property)
    {
        var setter = property.GetSetMethod(nonPublic: true);
        if (setter is null)
            return false;

        // If it has a public setter, it's a regular writable property regardless of init
        if (setter.IsPublic)
        {
            // Check if it's specifically an init-only public setter
            var isInitOnly = setter.ReturnParameter
                .GetRequiredCustomModifiers()
                .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

            return isInitOnly;
        }

        // Non-public setter with IsExternalInit → init-only (record property)
        var hasInitModifier = setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

        return hasInitModifier;
    }

    /// <summary>
    /// Builds a member binding for a single destination property, applying the member
    /// resolution priority: Ignore → ForMember expression → delegate resolver → convention match.
    /// For convention-matched complex-type properties, recursively builds nested mapping
    /// expressions with cycle detection. Returns <c>null</c> if the member should be omitted
    /// from bindings.
    /// </summary>
    private static MemberBinding? BuildMemberBinding(
        PropertyInfo destProp,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        MappingExpressionBase? config,
        MapperRegistry registry,
        HashSet<TypePair> inProgress)
    {
        var destName = destProp.Name;

        // Priority 1: Check for Ignore rule (MemberRules[name] == null)
        if (config?.MemberRules.TryGetValue(destName, out var memberRule) == true)
        {
            if (memberRule is null)
            {
                // Ignore rule — omit from bindings entirely
                return null;
            }

            // Priority 2: ForMember expression rule — use provided LambdaExpression body
            var ruleBody = ReplaceParameter(memberRule.Body, memberRule.Parameters[0], sourceParam);
            var convertedRuleBody = EnsureType(ruleBody, destProp.PropertyType);
            return Expression.Bind(destProp, convertedRuleBody);
        }

        // Priority 3: Delegate resolver
        if (config?.DelegateResolvers.TryGetValue(destName, out var delegateResolver) == true)
        {
            var resolverExpr = BuildDelegateResolverExpression(
                sourceParam, delegateResolver, destProp.PropertyType);
            return Expression.Bind(destProp, resolverExpr);
        }

        // Priority 4: Convention name match (case-insensitive)
        var matchingSourceProps = sourceProperties
            .Where(sp => string.Equals(sp.Name, destName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Skip if ambiguous (two+ source members match case-insensitively)
        if (matchingSourceProps.Length != 1)
            return null;

        var sourceProp = matchingSourceProps[0];

        // Check if both source and destination are collection types — collection mapping takes priority
        if (IsCollectionType(destProp.PropertyType) && IsCollectionType(sourceProp.PropertyType))
        {
            // Factory compiles element delegates on demand via ExpressionBuilder
            Func<TypePair, Delegate> factory = elementPair =>
            {
                var elementLambda = BuildMappingExpression(elementPair, config: null, registry, new HashSet<TypePair>());
                return elementLambda.Compile();
            };

            return BuildCollectionMemberBinding(destProp, sourceProp, sourceParam, registry, factory);
        }

        // Nested complex-type mapping: both source and destination are non-simple, non-collection types
        if (!IsSimpleType(destProp.PropertyType) && !IsSimpleType(sourceProp.PropertyType))
        {
            var nestedExpr = BuildNestedComplexTypeExpression(
                sourceParam, sourceProp, destProp, registry, inProgress);
            if (nestedExpr is null)
                return null;

            return Expression.Bind(destProp, nestedExpr);
        }

        // Build the value expression with null handling
        var valueExpr = BuildConventionValueExpression(sourceParam, sourceProp, destProp);
        if (valueExpr is null)
            return null;

        return Expression.Bind(destProp, valueExpr);
    }

    /// <summary>
    /// Builds the value expression for a convention-matched property, handling type
    /// compatibility and null source values.
    /// </summary>
    private static Expression? BuildConventionValueExpression(
        ParameterExpression sourceParam,
        PropertyInfo sourceProp,
        PropertyInfo destProp)
    {
        var sourceAccess = Expression.Property(sourceParam, sourceProp);
        var sourceType = sourceProp.PropertyType;
        var destType = destProp.PropertyType;

        // Build the conversion expression (without null handling first)
        var conversionExpr = BuildTypeConversionExpression(sourceAccess, sourceType, destType);
        if (conversionExpr is null)
            return null;

        // If source type is a value type and not nullable, no null check needed
        if (sourceType.IsValueType && Nullable.GetUnderlyingType(sourceType) is null)
            return conversionExpr;

        // Wrap with null handling for reference types and Nullable<T>
        return BuildNullGuardedExpression(sourceAccess, conversionExpr, sourceType, destType);
    }

    /// <summary>
    /// Builds a type conversion expression from the source member access to the destination type.
    /// Returns <c>null</c> if conversion is not possible.
    /// </summary>
    private static Expression? BuildTypeConversionExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destType)
    {
        // Same type or directly assignable
        if (destType.IsAssignableFrom(sourceType))
            return sourceAccess;

        // Handle Nullable<T> destination where source T is assignable
        var destUnderlying = Nullable.GetUnderlyingType(destType);
        var srcUnderlying = Nullable.GetUnderlyingType(sourceType);

        // int -> int? (source is assignable to underlying of nullable dest)
        if (destUnderlying is not null && destUnderlying.IsAssignableFrom(sourceType))
            return Expression.Convert(sourceAccess, destType);

        // int? -> int (nullable source to non-nullable dest of same underlying)
        if (srcUnderlying is not null && destType.IsAssignableFrom(srcUnderlying))
        {
            // Get the .Value property access
            return Expression.Convert(sourceAccess, destType);
        }

        // Both are simple types — try compatible conversion via Expression.Convert
        var effectiveSourceType = srcUnderlying ?? sourceType;
        var effectiveDestType = destUnderlying ?? destType;

        if (IsNumericOrConvertible(effectiveSourceType) && IsNumericOrConvertible(effectiveDestType))
        {
            // Direct Expression.Convert for numeric/primitive conversions (int -> long, etc.)
            return Expression.Convert(sourceAccess, destType);
        }

        // Both simple types but not directly convertible — use Convert.ChangeType with try/catch
        if (IsSimpleType(sourceType) && IsSimpleType(destType))
        {
            return BuildChangeTypeExpression(sourceAccess, sourceType, destType);
        }

        return null;
    }

    /// <summary>
    /// Builds a <c>Convert.ChangeType</c> expression wrapped in a try/catch that returns
    /// <c>default(TDest)</c> on failure.
    /// </summary>
    private static Expression BuildChangeTypeExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destType)
    {
        var effectiveDestType = Nullable.GetUnderlyingType(destType) ?? destType;

        // Box the source value to object for Convert.ChangeType
        Expression boxedSource = sourceAccess;
        if (sourceType.IsValueType)
            boxedSource = Expression.Convert(sourceAccess, typeof(object));

        // Convert.ChangeType(source, typeof(destType))
        var changeTypeCall = Expression.Call(
            typeof(Convert),
            nameof(Convert.ChangeType),
            Type.EmptyTypes,
            boxedSource,
            Expression.Constant(effectiveDestType));

        // Unbox the result
        Expression unboxed = Expression.Convert(changeTypeCall, destType);

        // Default value for destination type
        var defaultValue = Expression.Default(destType);

        // try { Convert.ChangeType(...) } catch { default(TDest) }
        var tryCatch = Expression.TryCatch(
            unboxed,
            Expression.Catch(typeof(Exception), defaultValue));

        return tryCatch;
    }

    /// <summary>
    /// Wraps a value expression with a null guard. When the source is null, produces
    /// <c>null</c> for nullable reference types or <c>default(T)</c> for non-nullable value types.
    /// </summary>
    private static Expression BuildNullGuardedExpression(
        Expression sourceAccess,
        Expression valueExpression,
        Type sourceType,
        Type destType)
    {
        Expression nullCheck;

        if (Nullable.GetUnderlyingType(sourceType) is not null)
        {
            // For Nullable<T>, check .HasValue
            nullCheck = Expression.Not(
                Expression.Property(sourceAccess, nameof(Nullable<int>.HasValue)));
        }
        else
        {
            // For reference types, check == null
            nullCheck = Expression.Equal(sourceAccess, Expression.Constant(null, sourceType));
        }

        // When null: assign null for nullable ref/nullable value types, default for value types
        Expression nullValue;
        if (IsNullableType(destType))
        {
            nullValue = Expression.Default(destType);
        }
        else if (destType.IsValueType)
        {
            nullValue = Expression.Default(destType);
        }
        else
        {
            nullValue = Expression.Default(destType);
        }

        // Ensure value expression and null value have the same type
        var typedValueExpr = EnsureType(valueExpression, destType);

        return Expression.Condition(nullCheck, nullValue, typedValueExpr);
    }

    /// <summary>
    /// Builds an expression that invokes a boxed <c>Func&lt;object, object?&gt;</c> delegate
    /// resolver, casting the result to the destination property type.
    /// </summary>
    private static Expression BuildDelegateResolverExpression(
        ParameterExpression sourceParam,
        Func<object, object?> resolver,
        Type destType)
    {
        // Capture the delegate as a constant
        var resolverConstant = Expression.Constant(resolver, typeof(Func<object, object?>));

        // Box the source parameter to object
        Expression boxedSource = sourceParam.Type.IsValueType
            ? Expression.Convert(sourceParam, typeof(object))
            : sourceParam;

        // Invoke the delegate: resolver(source)
        var invocation = Expression.Invoke(resolverConstant, boxedSource);

        // Convert the object? result to the destination type
        if (destType.IsValueType && Nullable.GetUnderlyingType(destType) is null)
        {
            // Non-nullable value type: unbox with Convert
            return Expression.Convert(invocation, destType);
        }

        if (destType.IsValueType)
        {
            // Nullable value type: unbox
            return Expression.Convert(invocation, destType);
        }

        // Reference type: cast
        return Expression.Convert(invocation, destType);
    }

    /// <summary>
    /// Replaces occurrences of <paramref name="oldParam"/> in <paramref name="body"/>
    /// with <paramref name="newParam"/>.
    /// </summary>
    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression oldParam,
        ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(body);
    }

    /// <summary>
    /// Replaces occurrences of <paramref name="oldParam"/> in <paramref name="body"/>
    /// with an arbitrary <paramref name="replacement"/> expression.
    /// Used for inlining nested lambda bodies where the parameter is replaced with a
    /// property access expression.
    /// </summary>
    private static Expression ReplaceParameter(
        Expression body,
        ParameterExpression oldParam,
        Expression replacement)
    {
        return new ParameterExpressionReplacer(oldParam, replacement).Visit(body);
    }

    /// <summary>
    /// Ensures that the expression is of the specified type, adding a conversion if necessary.
    /// </summary>
    private static Expression EnsureType(Expression expression, Type targetType)
    {
        if (expression.Type == targetType)
            return expression;

        return Expression.Convert(expression, targetType);
    }

    /// <summary>
    /// Determines whether a type is considered a "simple type" for mapping purposes.
    /// </summary>
    internal static bool IsSimpleType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var effectiveType = underlying ?? type;

        return SimpleTypes.Contains(effectiveType) || effectiveType.IsEnum;
    }

    /// <summary>
    /// Determines whether a type supports direct numeric/primitive conversion via
    /// <c>Expression.Convert</c>.
    /// </summary>
    private static bool IsNumericOrConvertible(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte)
            || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint)
            || type == typeof(long) || type == typeof(ulong)
            || type == typeof(float) || type == typeof(double)
            || type == typeof(decimal)
            || type == typeof(char);
    }

    /// <summary>
    /// Determines whether a type is nullable (either a <see cref="Nullable{T}"/> or a reference type).
    /// </summary>
    private static bool IsNullableType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is not null)
            return true;

        // Reference types are nullable
        return !type.IsValueType;
    }

    /// <summary>
    /// Determines whether a destination property has a writable setter (public set or init-only).
    /// </summary>
    private static bool HasWritableSetter(PropertyInfo property)
    {
        var setter = property.GetSetMethod(nonPublic: true);
        if (setter is null)
            return false;

        // Accept public setters
        if (setter.IsPublic)
            return true;

        // Accept init-only setters (marked with IsExternalInit modifier)
        var isInitOnly = setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

        return isInitOnly;
    }

    /// <summary>
    /// Builds a <see cref="MemberBinding"/> for a collection-type destination property.
    /// Resolves element types, obtains or compiles the element mapping delegate from the registry,
    /// builds a <c>Enumerable.Select(...).ToList()/ToArray()</c> expression, and wraps with a null guard.
    /// </summary>
    /// <param name="destProp">The destination collection property to bind.</param>
    /// <param name="sourceProp">The source collection property to read from.</param>
    /// <param name="sourceParam">The source parameter expression.</param>
    /// <param name="registry">The mapper registry for resolving element delegates.</param>
    /// <param name="factory">Factory function for compiling element delegates on demand.</param>
    /// <returns>A <see cref="MemberBinding"/> for the collection mapping, or <c>null</c> if types cannot be resolved.</returns>
    internal static MemberBinding? BuildCollectionMemberBinding(
        PropertyInfo destProp,
        PropertyInfo sourceProp,
        ParameterExpression sourceParam,
        MapperRegistry registry,
        Func<TypePair, Delegate> factory)
    {
        var srcElementType = GetCollectionElementType(sourceProp.PropertyType);
        var destElementType = GetCollectionElementType(destProp.PropertyType);

        if (srcElementType is null || destElementType is null)
            return null;

        var elementPair = new TypePair(srcElementType, destElementType);

        // Get or compile the element mapping delegate from the registry
        var elementDelegate = registry.GetOrAdd(elementPair, factory);

        // Build the mapping expression:
        // source.Items != null
        //   ? source.Items.Select(x => elementDelegate(x)).ToList()/ToArray()
        //   : null / new List<T>()
        var sourceAccess = Expression.Property(sourceParam, sourceProp);

        // Build the Select + materialisation expression
        var selectExpr = BuildSelectExpression(sourceAccess, elementDelegate, srcElementType, destElementType);

        // Apply materialisation (.ToArray() or .ToList())
        var materialised = BuildMaterialisationExpression(selectExpr, destProp.PropertyType, destElementType);
        if (materialised is null)
            return null;

        // Null guard
        var nullGuarded = BuildCollectionNullGuard(sourceAccess, materialised, destProp.PropertyType, destElementType);

        return Expression.Bind(destProp, nullGuarded);
    }

    /// <summary>
    /// Determines whether a type is a collection type (implements <see cref="IEnumerable"/>
    /// but is not <see cref="string"/>).
    /// </summary>
    internal static bool IsCollectionType(Type type)
    {
        if (type == typeof(string))
            return false;

        if (type.IsArray)
            return true;

        if (type.IsGenericType)
        {
            var genDef = type.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) ||
                genDef == typeof(IList<>) ||
                genDef == typeof(ICollection<>) ||
                genDef == typeof(IEnumerable<>) ||
                genDef == typeof(IReadOnlyList<>) ||
                genDef == typeof(IReadOnlyCollection<>))
            {
                return true;
            }
        }

        // Check if it implements IEnumerable<T>
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    /// <summary>
    /// Extracts the generic element type from a collection type.
    /// For arrays returns the element type; for generic collections returns the generic argument.
    /// </summary>
    internal static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
        {
            var genDef = collectionType.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) ||
                genDef == typeof(IList<>) ||
                genDef == typeof(ICollection<>) ||
                genDef == typeof(IEnumerable<>) ||
                genDef == typeof(IReadOnlyList<>) ||
                genDef == typeof(IReadOnlyCollection<>))
            {
                return collectionType.GetGenericArguments()[0];
            }
        }

        // Fallback: find IEnumerable<T> interface
        var enumerableInterface = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }

    /// <summary>
    /// Builds a <c>Enumerable.Select(source, elementMapper)</c> expression that projects
    /// each element through the element mapping delegate.
    /// </summary>
    private static Expression BuildSelectExpression(
        Expression sourceCollection,
        Delegate elementDelegate,
        Type srcElementType,
        Type destElementType)
    {
        // Store the delegate as a constant in the expression tree
        var funcType = typeof(Func<,>).MakeGenericType(srcElementType, destElementType);
        var delegateConst = Expression.Constant(elementDelegate, funcType);

        // Build: Enumerable.Select(sourceCollection, delegateConst)
        var selectMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Select) &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(srcElementType, destElementType);

        return Expression.Call(selectMethod, sourceCollection, delegateConst);
    }

    /// <summary>
    /// Appends the appropriate materialisation method call to the <paramref name="selectExpression"/>:
    /// <c>.ToArray()</c> for <c>T[]</c> destinations, <c>.ToList()</c> for all other supported collection types.
    /// </summary>
    private static Expression? BuildMaterialisationExpression(
        Expression selectExpression,
        Type destCollectionType,
        Type destElementType)
    {
        if (destCollectionType.IsArray)
        {
            // .ToArray()
            var toArrayMethod = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.ToArray), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(destElementType);

            return Expression.Call(toArrayMethod, selectExpression);
        }

        // All other supported types materialise via .ToList()
        var toListMethod = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.ToList), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(destElementType);

        var listExpr = Expression.Call(toListMethod, selectExpression);

        // For concrete List<T>, return directly
        if (destCollectionType.IsGenericType)
        {
            var genDef = destCollectionType.GetGenericTypeDefinition();
            if (genDef == typeof(List<>) ||
                genDef == typeof(IList<>) ||
                genDef == typeof(ICollection<>) ||
                genDef == typeof(IEnumerable<>) ||
                genDef == typeof(IReadOnlyList<>) ||
                genDef == typeof(IReadOnlyCollection<>))
            {
                // ToList() returns List<T> which is assignable to all these interfaces
                return Expression.Convert(listExpr, destCollectionType);
            }
        }

        return listExpr;
    }

    /// <summary>
    /// Wraps the materialised collection expression with a null guard:
    /// if source is null → <c>null</c> (nullable dest) or <c>new List&lt;T&gt;()</c> / <c>new T[0]</c> (non-nullable dest).
    /// </summary>
    private static Expression BuildCollectionNullGuard(
        Expression sourceAccess,
        Expression materialisedExpression,
        Type destCollectionType,
        Type destElementType)
    {
        // null check: source.Items == null
        var nullCheck = Expression.Equal(
            sourceAccess,
            Expression.Constant(null, sourceAccess.Type));

        // Determine the null-case expression
        Expression nullValue;
        if (IsNullableType(destCollectionType))
        {
            // Nullable destination: assign null
            nullValue = Expression.Constant(null, destCollectionType);
        }
        else
        {
            // Non-nullable destination: assign empty collection of correct type
            nullValue = BuildEmptyCollectionExpression(destCollectionType, destElementType);
        }

        // Ensure types match
        var typedMaterialised = EnsureType(materialisedExpression, destCollectionType);

        return Expression.Condition(nullCheck, nullValue, typedMaterialised);
    }

    /// <summary>
    /// Builds an expression that creates an empty collection of the appropriate concrete type
    /// for non-nullable destination collection properties.
    /// </summary>
    private static Expression BuildEmptyCollectionExpression(Type destCollectionType, Type destElementType)
    {
        if (destCollectionType.IsArray)
        {
            // Array.Empty<T>()
            var arrayEmptyMethod = typeof(Array)
                .GetMethod(nameof(Array.Empty), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(destElementType);

            return Expression.Call(arrayEmptyMethod);
        }

        // new List<T>() for all other collection types
        var listType = typeof(List<>).MakeGenericType(destElementType);
        var listCtor = listType.GetConstructor(Type.EmptyTypes)!;
        Expression newList = Expression.New(listCtor);

        // Cast to the destination type if needed
        if (destCollectionType != listType)
        {
            newList = Expression.Convert(newList, destCollectionType);
        }

        return newList;
    }

    /// <summary>
    /// Builds an expression for mapping a nested complex-type property. Handles cycle detection
    /// by producing a deferred <c>Lazy&lt;Func&lt;TNestedSource, TNestedTarget&gt;&gt;</c> closure
    /// when a cycle is detected, or inlining the nested lambda body when no cycle exists.
    /// The result is wrapped in a null guard for the source property.
    /// </summary>
    private static Expression? BuildNestedComplexTypeExpression(
        ParameterExpression sourceParam,
        PropertyInfo sourceProp,
        PropertyInfo destProp,
        MapperRegistry registry,
        HashSet<TypePair> inProgress)
    {
        var nestedSourceType = sourceProp.PropertyType;
        var nestedTargetType = destProp.PropertyType;
        var nestedPair = new TypePair(nestedSourceType, nestedTargetType);

        // Ensure the target type has at least a parameterless constructor or some public constructor
        // to be considered mappable as a complex type
        var targetConstructors = nestedTargetType.GetConstructors();
        if (targetConstructors.Length == 0 && nestedTargetType.GetConstructor(Type.EmptyTypes) is null)
            return null;

        var sourceAccess = Expression.Property(sourceParam, sourceProp);

        Expression mappingBody;

        if (inProgress.Contains(nestedPair))
        {
            // Cycle detected: create a deferred Lazy<Func<TNestedSource, TNestedTarget>>
            mappingBody = BuildDeferredCyclicMapping(sourceAccess, nestedSourceType, nestedTargetType, registry);
        }
        else
        {
            // No cycle: recurse to build nested mapping expression
            inProgress.Add(nestedPair);
            try
            {
                var nestedLambda = BuildMappingExpression(nestedPair, config: null, registry, inProgress);

                // Inline the nested lambda body by replacing its parameter with the source property access
                var nestedParam = nestedLambda.Parameters[0];
                mappingBody = ReplaceParameter(nestedLambda.Body, nestedParam, sourceAccess);
            }
            finally
            {
                inProgress.Remove(nestedPair);
            }
        }

        // Wrap with null guard for the source property
        return BuildNestedNullGuard(sourceAccess, mappingBody, nestedSourceType, nestedTargetType);
    }

    /// <summary>
    /// Builds a deferred mapping invocation for cyclic type pairs. Creates a
    /// <c>Lazy&lt;Func&lt;TNestedSource, TNestedTarget&gt;&gt;</c> that resolves the compiled
    /// delegate from the registry after all compilation completes. The expression tree captures
    /// <c>Expression.Invoke(Expression.Property(lazyConst, "Value"), sourceExpr)</c>.
    /// </summary>
    private static Expression BuildDeferredCyclicMapping(
        Expression sourceAccess,
        Type nestedSourceType,
        Type nestedTargetType,
        MapperRegistry registry)
    {
        var nestedPair = new TypePair(nestedSourceType, nestedTargetType);

        // Create the Func<TNestedSource, TNestedTarget> type
        var funcType = typeof(Func<,>).MakeGenericType(nestedSourceType, nestedTargetType);

        // Create the Lazy<Func<TNestedSource, TNestedTarget>> type
        var lazyType = typeof(Lazy<>).MakeGenericType(funcType);

        // Build the factory delegate that resolves from the registry at runtime
        // The factory is Func<Func<TNestedSource, TNestedTarget>> — wrapping the registry resolution
        var factoryFuncType = typeof(Func<>).MakeGenericType(funcType);

        // Use a helper to create the Lazy instance via reflection to preserve generic types
        var lazyInstance = CreateLazyRegistryResolver(registry, nestedPair, funcType, lazyType, factoryFuncType);

        // ConstantExpression holding the Lazy<Func<>> instance
        var lazyConst = Expression.Constant(lazyInstance, lazyType);

        // Access Lazy.Value to get the Func<TNestedSource, TNestedTarget>
        var valueProperty = lazyType.GetProperty("Value")!;
        var lazyValueAccess = Expression.Property(lazyConst, valueProperty);

        // Invoke the Func with the source access: lazyInstance.Value(source.Property)
        return Expression.Invoke(lazyValueAccess, sourceAccess);
    }

    /// <summary>
    /// Creates a <c>Lazy&lt;Func&lt;TNestedSource, TNestedTarget&gt;&gt;</c> instance whose factory
    /// resolves the compiled mapping delegate from the registry. The <c>Lazy</c> ensures the delegate
    /// is fetched exactly once, on first invocation (which occurs after <c>Build()</c> completes).
    /// </summary>
    private static object CreateLazyRegistryResolver(
        MapperRegistry registry,
        TypePair pair,
        Type funcType,
        Type lazyType,
        Type factoryFuncType)
    {
        // Build a factory expression: () => (Func<TSource, TTarget>)registry.GetOrAdd(pair, fallback)
        // We use Expression trees here because we need to produce a strongly-typed Func<> at runtime

        // Capture registry and pair
        var registryConst = Expression.Constant(registry);
        var pairConst = Expression.Constant(pair, typeof(TypePair));
        var fallbackConst = Expression.Constant(
            (Func<TypePair, Delegate>)(p => throw new InvalidOperationException(
                $"Cyclic mapping delegate for '{p}' was not found in the registry.")),
            typeof(Func<TypePair, Delegate>));

        // Call registry.GetOrAdd(pair, fallback)
        var getOrAddMethod = typeof(MapperRegistry).GetMethod(
            "GetOrAdd",
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(TypePair), typeof(Func<TypePair, Delegate>)])!;

        var getOrAddCall = Expression.Call(registryConst, getOrAddMethod, pairConst, fallbackConst);

        // Cast the Delegate to the specific Func<TNestedSource, TNestedTarget>
        var castExpr = Expression.Convert(getOrAddCall, funcType);

        // Create: () => (Func<TNestedSource, TNestedTarget>)registry.GetOrAdd(pair, fallback)
        var factoryLambda = Expression.Lambda(factoryFuncType, castExpr);
        var factoryDelegate = factoryLambda.Compile();

        // Create new Lazy<Func<TNestedSource, TNestedTarget>>(factory)
        var lazyCtor = lazyType.GetConstructor([factoryFuncType])!;
        return lazyCtor.Invoke([factoryDelegate]);
    }

    /// <summary>
    /// Wraps a nested complex-type mapping expression with a null guard.
    /// When the source property is null, assigns <c>null</c> for nullable reference types
    /// or <c>default(TTarget)</c> for non-nullable value types.
    /// </summary>
    private static Expression BuildNestedNullGuard(
        Expression sourceAccess,
        Expression mappingExpression,
        Type nestedSourceType,
        Type nestedTargetType)
    {
        // For value-type source properties (structs), no null check is needed
        if (nestedSourceType.IsValueType && Nullable.GetUnderlyingType(nestedSourceType) is null)
            return mappingExpression;

        // Build null check
        Expression nullCheck;
        if (Nullable.GetUnderlyingType(nestedSourceType) is not null)
        {
            // Nullable<T>: check .HasValue
            nullCheck = Expression.Not(
                Expression.Property(sourceAccess, nameof(Nullable<int>.HasValue)));
        }
        else
        {
            // Reference type: check == null
            nullCheck = Expression.Equal(sourceAccess, Expression.Constant(null, nestedSourceType));
        }

        // When null: produce null for nullable ref types or default for non-nullable value types
        Expression nullValue;
        if (nestedTargetType.IsValueType && Nullable.GetUnderlyingType(nestedTargetType) is null)
        {
            nullValue = Expression.Default(nestedTargetType);
        }
        else
        {
            nullValue = Expression.Constant(null, nestedTargetType);
        }

        // Ensure the mapping expression matches the target type
        var typedMapping = EnsureType(mappingExpression, nestedTargetType);

        return Expression.Condition(nullCheck, nullValue, typedMapping);
    }

    /// <summary>
    /// Builds a pure <see cref="LambdaExpression"/> suitable for passing to
    /// <c>IQueryable&lt;TSource&gt;.Select(...)</c>. Uses the same member-resolution rules as
    /// <see cref="BuildMappingExpression"/> but throws <see cref="NotSupportedException"/> for
    /// any member that requires <c>Convert.ChangeType</c> or uses a <c>Func&lt;&gt;</c> delegate resolver.
    /// Nested complex-type pairs are inlined recursively; cycles throw <see cref="NotSupportedException"/>.
    /// </summary>
    /// <param name="pair">The source-to-target type pair to build the projection expression for.</param>
    /// <param name="config">
    /// Optional configuration containing custom member rules, delegate resolvers, and ignore rules.
    /// When <c>null</c>, convention-only projection is applied.
    /// </param>
    /// <returns>
    /// A <see cref="LambdaExpression"/> of the form <c>(TSource source) =&gt; new TTarget { ... }</c>
    /// suitable for LINQ query provider translation.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when a member requires <c>Convert.ChangeType</c>, uses a <c>Func&lt;&gt;</c> delegate resolver,
    /// or when a cycle is detected in nested complex-type projection.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the target type has no satisfiable constructor.
    /// </exception>
    internal static LambdaExpression BuildProjectionExpression(
        TypePair pair,
        MappingExpressionBase? config)
    {
        return BuildProjectionExpressionInternal(pair, config, new HashSet<TypePair>());
    }

    /// <summary>
    /// Internal recursive implementation of <see cref="BuildProjectionExpression"/> with
    /// cycle detection via an <paramref name="inProgress"/> set.
    /// </summary>
    private static LambdaExpression BuildProjectionExpressionInternal(
        TypePair pair,
        MappingExpressionBase? config,
        HashSet<TypePair> inProgress)
    {
        var sourceType = pair.SourceType;
        var targetType = pair.TargetType;

        // Cycle detection
        if (!inProgress.Add(pair))
        {
            throw new NotSupportedException(
                $"Cycle detected for '{pair}' during projection expression building. " +
                "ProjectTo does not support cyclic type references.");
        }

        try
        {
            var sourceParam = Expression.Parameter(sourceType, "source");

            var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Check for delegate resolvers upfront — they are never supported in projections
            if (config?.DelegateResolvers.Count > 0)
            {
                var firstMember = config.DelegateResolvers.Keys.First();
                throw new NotSupportedException(
                    $"Member '{firstMember}' on '{targetType.Name}' uses a Func<> delegate resolver " +
                    "which is not supported by ProjectTo. Use an Expression-based ForMember overload instead.");
            }

            // Determine whether to use parameterless constructor or constructor-parameter mapping
            var hasParameterlessCtor = targetType.GetConstructor(Type.EmptyTypes) is not null;
            var hasCtorParamRules = config?.CtorParamRules.Count > 0;

            if (hasParameterlessCtor && !hasCtorParamRules)
            {
                return BuildProjectionParameterlessCtorExpression(
                    sourceParam, sourceProperties, targetType, targetProperties, config, inProgress);
            }

            return BuildProjectionConstructorExpression(
                pair, sourceParam, sourceProperties, targetType, targetProperties, config, inProgress);
        }
        finally
        {
            inProgress.Remove(pair);
        }
    }

    /// <summary>
    /// Builds a projection expression using the parameterless constructor and member init bindings.
    /// Produces a pure expression tree without <c>Convert.ChangeType</c> or delegate resolvers.
    /// </summary>
    private static LambdaExpression BuildProjectionParameterlessCtorExpression(
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Type targetType,
        PropertyInfo[] targetProperties,
        MappingExpressionBase? config,
        HashSet<TypePair> inProgress)
    {
        var bindings = new List<MemberBinding>();

        foreach (var destProp in targetProperties)
        {
            if (!HasWritableSetter(destProp))
                continue;

            var binding = BuildProjectionMemberBinding(
                destProp, sourceParam, sourceProperties, config, inProgress);

            if (binding is not null)
                bindings.Add(binding);
        }

        var newExpr = Expression.New(targetType);
        var memberInit = Expression.MemberInit(newExpr, bindings);
        return Expression.Lambda(memberInit, sourceParam);
    }

    /// <summary>
    /// Builds a projection expression using constructor-parameter mapping.
    /// Produces a pure expression tree with a <see cref="NewExpression"/> containing named arguments.
    /// </summary>
    private static LambdaExpression BuildProjectionConstructorExpression(
        TypePair pair,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Type targetType,
        PropertyInfo[] targetProperties,
        MappingExpressionBase? config,
        HashSet<TypePair> inProgress)
    {
        var ctorParamRules = config?.CtorParamRules
            ?? new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase);

        // Validate ForCtorParam rules reference existing parameters
        ValidateCtorParamRules(pair, targetType, ctorParamRules);

        // Select the best constructor (same logic as BuildMappingExpression)
        var (selectedCtor, argExpressions) = SelectBestProjectionConstructor(
            pair, targetType, sourceParam, sourceProperties, ctorParamRules);

        // Build the NewExpression with constructor arguments
        var newExpr = Expression.New(selectedCtor, argExpressions);

        // Determine which property names are already covered by constructor parameters
        var ctorParamNames = new HashSet<string>(
            selectedCtor.GetParameters().Select(p => p.Name!),
            StringComparer.OrdinalIgnoreCase);

        // Build member bindings for remaining settable properties NOT covered by constructor
        var bindings = new List<MemberBinding>();

        foreach (var destProp in targetProperties)
        {
            if (ctorParamNames.Contains(destProp.Name))
                continue;

            if (IsInitOnlySetter(destProp))
                continue;

            if (!HasWritableSetter(destProp))
                continue;

            var binding = BuildProjectionMemberBinding(
                destProp, sourceParam, sourceProperties, config, inProgress);

            if (binding is not null)
                bindings.Add(binding);
        }

        Expression body;
        if (bindings.Count > 0)
        {
            body = Expression.MemberInit(newExpr, bindings);
        }
        else
        {
            body = newExpr;
        }

        return Expression.Lambda(body, sourceParam);
    }

    /// <summary>
    /// Selects the best public constructor for projection, applying the same resolution rules
    /// as <see cref="SelectBestConstructor"/> but without delegate-based resolution.
    /// </summary>
    private static (ConstructorInfo Constructor, Expression[] Arguments) SelectBestProjectionConstructor(
        TypePair pair,
        Type targetType,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Dictionary<string, LambdaExpression> ctorParamRules)
    {
        var constructors = targetType.GetConstructors();

        ConstructorInfo? bestCtor = null;
        Expression[]? bestArgs = null;
        var bestParamCount = -1;
        List<string>? unsatisfiedParams = null;

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var args = new Expression[parameters.Length];
            var allSatisfied = true;
            var currentUnsatisfied = new List<string>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var argExpr = ResolveProjectionConstructorParameter(
                    param, sourceParam, sourceProperties, ctorParamRules);

                if (argExpr is not null)
                {
                    args[i] = argExpr;
                }
                else
                {
                    allSatisfied = false;
                    currentUnsatisfied.Add(param.Name ?? $"arg{i}");
                }
            }

            if (allSatisfied && parameters.Length > bestParamCount)
            {
                bestCtor = ctor;
                bestArgs = args;
                bestParamCount = parameters.Length;
            }

            if (!allSatisfied && (unsatisfiedParams is null || parameters.Length > (unsatisfiedParams.Count + bestParamCount)))
            {
                unsatisfiedParams = currentUnsatisfied;
            }
        }

        if (bestCtor is null || bestArgs is null)
        {
            var paramList = unsatisfiedParams is not null
                ? string.Join(", ", unsatisfiedParams)
                : "unknown";

            throw new InvalidOperationException(
                $"Cannot create an instance of '{targetType.Name}'. No public constructor is fully satisfiable. Unsatisfied parameters: {paramList}");
        }

        return (bestCtor, bestArgs);
    }

    /// <summary>
    /// Resolves a constructor parameter for projection. Uses the same priority order as
    /// <see cref="ResolveConstructorParameter"/> (ForCtorParam → convention → default) but
    /// produces only pure expression-tree compatible values — no delegate-based resolution.
    /// </summary>
    private static Expression? ResolveProjectionConstructorParameter(
        ParameterInfo param,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        Dictionary<string, LambdaExpression> ctorParamRules)
    {
        var paramName = param.Name ?? string.Empty;
        var paramType = param.ParameterType;

        // Priority 1: ForCtorParam rule (expression-based)
        if (ctorParamRules.TryGetValue(paramName, out var ctorRule))
        {
            var ruleBody = ReplaceParameter(ctorRule.Body, ctorRule.Parameters[0], sourceParam);
            return EnsureType(ruleBody, paramType);
        }

        // Priority 2: Convention name match
        var matchingSourceProps = sourceProperties
            .Where(sp => string.Equals(sp.Name, paramName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingSourceProps.Length == 1)
        {
            var sourceProp = matchingSourceProps[0];

            if (IsProjectionAssignable(sourceProp.PropertyType, paramType))
            {
                var sourceAccess = Expression.Property(sourceParam, sourceProp);
                return BuildProjectionTypeConversion(sourceAccess, sourceProp.PropertyType, paramType);
            }
        }

        // Priority 3: Declared default value
        if (param.HasDefaultValue)
        {
            return Expression.Constant(param.DefaultValue, paramType);
        }

        return null;
    }

    /// <summary>
    /// Builds a member binding for a single destination property in projection mode.
    /// Applies the member resolution priority: Ignore → ForMember expression → convention match.
    /// Throws <see cref="NotSupportedException"/> for delegate resolvers or <c>Convert.ChangeType</c>.
    /// </summary>
    private static MemberBinding? BuildProjectionMemberBinding(
        PropertyInfo destProp,
        ParameterExpression sourceParam,
        PropertyInfo[] sourceProperties,
        MappingExpressionBase? config,
        HashSet<TypePair> inProgress)
    {
        var destName = destProp.Name;

        // Priority 1: Check for Ignore rule (MemberRules[name] == null)
        if (config?.MemberRules.TryGetValue(destName, out var memberRule) == true)
        {
            if (memberRule is null)
            {
                // Ignore rule — assign CLR default in expression tree
                return Expression.Bind(destProp, Expression.Default(destProp.PropertyType));
            }

            // Priority 2: ForMember expression rule
            var ruleBody = ReplaceParameter(memberRule.Body, memberRule.Parameters[0], sourceParam);
            var convertedRuleBody = EnsureType(ruleBody, destProp.PropertyType);
            return Expression.Bind(destProp, convertedRuleBody);
        }

        // Priority 3: Delegate resolver — NOT supported in projection
        if (config?.DelegateResolvers.TryGetValue(destName, out _) == true)
        {
            throw new NotSupportedException(
                $"Member '{destName}' on '{destProp.DeclaringType?.Name ?? "unknown"}' uses a Func<> delegate resolver " +
                "which is not supported by ProjectTo. Use an Expression-based ForMember overload instead.");
        }

        // Priority 4: Convention name match (case-insensitive)
        var matchingSourceProps = sourceProperties
            .Where(sp => string.Equals(sp.Name, destName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingSourceProps.Length != 1)
            return null;

        var sourceProp = matchingSourceProps[0];
        var sourceAccess = Expression.Property(sourceParam, sourceProp);
        var sourceType = sourceProp.PropertyType;
        var destType = destProp.PropertyType;

        // Nested complex-type mapping: both source and destination are non-simple, non-collection types
        if (!IsSimpleType(destType) && !IsSimpleType(sourceType)
            && !IsCollectionType(destType) && !IsCollectionType(sourceType))
        {
            var nestedPair = new TypePair(sourceType, destType);
            var nestedLambda = BuildProjectionExpressionInternal(nestedPair, config: null, inProgress);

            // Inline the nested lambda body by replacing its parameter with the source property access
            var nestedParam = nestedLambda.Parameters[0];
            var inlinedBody = ReplaceParameter(nestedLambda.Body, nestedParam, sourceAccess);

            return Expression.Bind(destProp, inlinedBody);
        }

        // Build projection-safe type conversion
        var valueExpr = BuildProjectionValueExpression(sourceAccess, sourceType, destType);
        if (valueExpr is null)
            return null;

        return Expression.Bind(destProp, valueExpr);
    }

    /// <summary>
    /// Builds a value expression for projection, enforcing that no <c>Convert.ChangeType</c> is used.
    /// Throws <see cref="NotSupportedException"/> for type conversions that require runtime conversion.
    /// </summary>
    private static Expression? BuildProjectionValueExpression(
        Expression sourceAccess,
        Type sourceType,
        Type destType)
    {
        // Same type or directly assignable
        if (destType.IsAssignableFrom(sourceType))
            return sourceAccess;

        var destUnderlying = Nullable.GetUnderlyingType(destType);
        var srcUnderlying = Nullable.GetUnderlyingType(sourceType);

        // int -> int? (source is assignable to underlying of nullable dest)
        if (destUnderlying is not null && destUnderlying.IsAssignableFrom(sourceType))
            return Expression.Convert(sourceAccess, destType);

        // int? -> int (nullable source to non-nullable dest of same underlying)
        if (srcUnderlying is not null && destType.IsAssignableFrom(srcUnderlying))
            return Expression.Convert(sourceAccess, destType);

        // Numeric/convertible compatibility via Expression.Convert
        var effectiveSourceType = srcUnderlying ?? sourceType;
        var effectiveDestType = destUnderlying ?? destType;

        if (IsNumericOrConvertible(effectiveSourceType) && IsNumericOrConvertible(effectiveDestType))
            return Expression.Convert(sourceAccess, destType);

        // Both simple types but NOT directly convertible via Expression.Convert → would require Convert.ChangeType
        if (IsSimpleType(sourceType) && IsSimpleType(destType))
        {
            throw new NotSupportedException(
                $"Member '{((MemberExpression)sourceAccess).Member.Name}' on projection target requires Convert.ChangeType " +
                $"which is not supported by LINQ query providers. Source type: '{sourceType.Name}', destination type: '{destType.Name}'.");
        }

        return null;
    }

    /// <summary>
    /// Determines whether a source type is assignable to a destination type for projection purposes,
    /// without requiring <c>Convert.ChangeType</c>.
    /// </summary>
    private static bool IsProjectionAssignable(Type sourceType, Type destType)
    {
        if (destType.IsAssignableFrom(sourceType))
            return true;

        var srcUnderlying = Nullable.GetUnderlyingType(sourceType);
        var destUnderlying = Nullable.GetUnderlyingType(destType);

        // Handle Nullable<T> → T and T → Nullable<T>
        if (destUnderlying is not null && destUnderlying.IsAssignableFrom(sourceType))
            return true;

        if (srcUnderlying is not null && destType.IsAssignableFrom(srcUnderlying))
            return true;

        // Numeric/convertible compatibility (Expression.Convert supported)
        var effectiveSource = srcUnderlying ?? sourceType;
        var effectiveDest = destUnderlying ?? destType;

        if (IsNumericOrConvertible(effectiveSource) && IsNumericOrConvertible(effectiveDest))
            return true;

        return false;
    }

    /// <summary>
    /// Builds a type conversion expression for projection that is safe for LINQ query providers.
    /// Only uses <c>Expression.Convert</c> for directly convertible types.
    /// </summary>
    private static Expression BuildProjectionTypeConversion(
        Expression sourceAccess,
        Type sourceType,
        Type destType)
    {
        if (destType.IsAssignableFrom(sourceType))
            return sourceAccess;

        // All supported projection conversions use Expression.Convert
        return Expression.Convert(sourceAccess, destType);
    }

    /// <summary>
    /// Expression visitor that replaces one <see cref="ParameterExpression"/> with another.
    /// Used to rebind lambda bodies to the mapping source parameter.
    /// </summary>
    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        internal ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }

    /// <summary>
    /// Expression visitor that replaces a <see cref="ParameterExpression"/> with an arbitrary
    /// <see cref="Expression"/>. Used for inlining nested lambda bodies where the parameter
    /// is replaced with a property access expression (e.g., <c>source.Address</c>).
    /// </summary>
    private sealed class ParameterExpressionReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly Expression _replacement;

        internal ParameterExpressionReplacer(ParameterExpression oldParam, Expression replacement)
        {
            _oldParam = oldParam;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _replacement : base.VisitParameter(node);
        }
    }
}

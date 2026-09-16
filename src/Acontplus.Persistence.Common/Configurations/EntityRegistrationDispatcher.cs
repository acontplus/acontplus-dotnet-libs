using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Acontplus.Persistence.Common.Configurations;

/// <summary>
/// Non-generic registry storing <see cref="EntityRegistrationProvider"/> instances to avoid S2743 static fields in generic types.
/// </summary>
internal static class EntityRegistrationRegistry
{
    private static readonly ConcurrentDictionary<Type, EntityRegistrationProvider> Providers = new();

    public static void Initialize(Type type, EntityRegistrationProvider provider) =>
        Providers[type] = provider;

    public static EntityRegistrationProvider Get(Type type)
    {
        if (!Providers.TryGetValue(type, out var provider))
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            if (!Providers.TryGetValue(type, out provider))
            {
                throw new InvalidOperationException($"Registration provider not initialized for {type.Name}.");
            }
        }

        return provider;
    }
}

/// <summary>
/// Reusable base dispatcher that provides static registration methods for EF Core entity types across database providers.
/// </summary>
/// <typeparam name="TDerived">The provider-specific registration marker type.</typeparam>
public abstract class EntityRegistrationDispatcher<TDerived> where TDerived : class
{
    /// <summary>
    /// Initializes the underlying <see cref="EntityRegistrationProvider"/> for this derived registration type.
    /// </summary>
    /// <param name="provider">The registration provider instance.</param>
    protected static void InitializeProvider(EntityRegistrationProvider provider) =>
        EntityRegistrationRegistry.Initialize(typeof(TDerived), provider);

    /// <summary>
    /// Gets the registration provider, ensuring the derived class static constructor has executed.
    /// </summary>
    protected static EntityRegistrationProvider Provider =>
        EntityRegistrationRegistry.Get(typeof(TDerived));

    /// <summary>
    /// Registers entities with the ModelBuilder, applying base configurations,
    /// custom schema/table names, and optional specific entity configurations.
    /// </summary>
    public static void RegisterEntities(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        params Type[] entityTypes) =>
        Provider.Register(modelBuilder, dbContextType, nameMap, customConfigurations, entityTypes);

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        Provider.Register(modelBuilder, dbContextType, entityTypes);

    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public static void RegisterEntitiesWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas) =>
        Provider.RegisterWithSchemas(modelBuilder, dbContextType, entitySchemas);

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public static void RegisterEntitiesWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs) =>
        Provider.RegisterWithNames(modelBuilder, dbContextType, nameConfigs);

    /// <summary>
    /// Registers entities with specific custom configurations.
    /// </summary>
    public static void RegisterEntitiesWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        Provider.RegisterWithCustomConfigurations(modelBuilder, dbContextType, customConfigurations, entityTypes);
}

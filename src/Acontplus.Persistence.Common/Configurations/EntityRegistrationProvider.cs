namespace Acontplus.Persistence.Common.Configurations;

/// <summary>
/// Provides reusable registration logic for EF Core entity types across database providers.
/// </summary>
public class EntityRegistrationProvider(
    Type baseConfigurationTypeDefinition,
    Func<Type, bool> entityValidator,
    string invalidEntityMessage)
{
    /// <summary>
    /// Registers entities with the ModelBuilder applying base and custom configurations.
    /// </summary>
    public void Register(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        params Type[] entityTypes) =>
        EntityRegistrationHelper.RegisterEntities(
            modelBuilder,
            new EntityRegistrationOptions(
                dbContextType,
                baseConfigurationTypeDefinition,
                entityValidator,
                invalidEntityMessage,
                nameMap,
                customConfigurations),
            entityTypes);

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public void Register(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        Register(modelBuilder, dbContextType, null, null, entityTypes);

    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public void RegisterWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas) =>
        EntityRegistrationHelper.RegisterEntitiesWithSchemas(
            (mb, ctx, map, _, types) => Register(mb, ctx, map, null, types),
            modelBuilder,
            dbContextType,
            entitySchemas);

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public void RegisterWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs) =>
        EntityRegistrationHelper.RegisterEntitiesWithNames(
            (mb, ctx, map, _, types) => Register(mb, ctx, map, null, types),
            modelBuilder,
            dbContextType,
            nameConfigs);

    /// <summary>
    /// Registers entities with specific custom configurations.
    /// </summary>
    public void RegisterWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        Register(modelBuilder, dbContextType, null, customConfigurations, entityTypes);
}

/// <summary>
/// Entity registration provider for auditable entities inheriting from <see cref="Acontplus.Core.Domain.Common.Entities.BaseEntity"/>.
/// </summary>
public class AuditableEntityRegistrationProvider(
    Type baseConfigurationTypeDefinition,
    string invalidEntityMessage = "must be a concrete class inheriting from BaseEntity")
    : EntityRegistrationProvider(
        baseConfigurationTypeDefinition,
        EntityRegistrationHelper.IsValidAuditableEntity,
        invalidEntityMessage);

/// <summary>
/// Entity registration provider for simple entities not inheriting from <see cref="Acontplus.Core.Domain.Common.Entities.BaseEntity"/>.
/// </summary>
public class SimpleEntityRegistrationProvider(
    Type baseConfigurationTypeDefinition,
    string invalidEntityMessage = "must be a concrete class not inheriting from BaseEntity with an Id property")
    : EntityRegistrationProvider(
        baseConfigurationTypeDefinition,
        EntityRegistrationHelper.IsValidSimpleEntity,
        invalidEntityMessage);

using Acontplus.Persistence.Common.Configurations;

namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Provides methods for registering EF Core entity types with the PostgreSQL model builder.
/// </summary>
public static class BaseEntityRegistration
{
    /// <summary>
    /// Registers auditable entities with the ModelBuilder, applying base configurations,
    /// custom schema/table names, and optional specific entity configurations.
    /// </summary>
    public static void RegisterEntities(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        params Type[] entityTypes) =>
        EntityRegistrationHelper.RegisterEntities(
            modelBuilder,
            new EntityRegistrationOptions(
                dbContextType,
                typeof(BaseEntityTypeConfiguration<>),
                EntityRegistrationHelper.IsValidAuditableEntity,
                "must be a concrete class inheriting from BaseEntity",
                nameMap,
                customConfigurations),
            entityTypes);

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null, null, entityTypes);

    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public static void RegisterEntitiesWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas) =>
        EntityRegistrationHelper.RegisterEntitiesWithSchemas(
            (mb, ctx, map, _, types) => RegisterEntities(mb, ctx, map, null, types),
            modelBuilder,
            dbContextType,
            entitySchemas);

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public static void RegisterEntitiesWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs) =>
        EntityRegistrationHelper.RegisterEntitiesWithNames(
            (mb, ctx, map, _, types) => RegisterEntities(mb, ctx, map, null, types),
            modelBuilder,
            dbContextType,
            nameConfigs);

    /// <summary>
    /// Registers entities with specific custom configurations.
    /// </summary>
    public static void RegisterEntitiesWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null, customConfigurations, entityTypes);
}

using Acontplus.Persistence.Common.Configurations;

namespace Acontplus.Persistence.SqlServer.Configurations;

/// <summary>
/// Provides methods for registering simple (non-auditable) EF Core entity types with the SQL Server model builder.
/// </summary>
public static class SimpleEntityRegistration
{
    /// <summary>
    /// Registers non-auditable entities with the ModelBuilder, applying base configurations,
    /// custom schema/table names, and optional specific entity configurations.
    /// </summary>
    public static void RegisterEntities(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)> nameMap,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        EntityRegistrationHelper.RegisterEntities(
            modelBuilder,
            dbContextType,
            nameMap,
            customConfigurations,
            typeof(SimpleEntityTypeConfiguration<>),
            EntityRegistrationHelper.IsValidSimpleEntity,
            "must be a concrete class not inheriting from BaseEntity with an Id property",
            entityTypes);

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null!, null!, entityTypes);

    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public static void RegisterEntitiesWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas)
    {
        var schemaMap = EntityRegistrationHelper.CreateSchemaMap(entitySchemas);
        var entityTypes = entitySchemas.Select(x => x.entityType).ToArray();
        RegisterEntities(modelBuilder, dbContextType, schemaMap, null!, entityTypes);
    }

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public static void RegisterEntitiesWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs)
    {
        var nameMap = EntityRegistrationHelper.CreateNameMap(nameConfigs);
        var entityTypes = nameConfigs.Select(x => x.entityType).ToArray();
        RegisterEntities(modelBuilder, dbContextType, nameMap, null!, entityTypes);
    }

    /// <summary>
    /// Registers entities with specific custom configurations.
    /// </summary>
    public static void RegisterEntitiesWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null!, customConfigurations, entityTypes);
}

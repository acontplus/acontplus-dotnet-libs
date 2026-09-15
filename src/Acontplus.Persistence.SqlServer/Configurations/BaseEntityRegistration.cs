using Acontplus.Persistence.Common.Configurations;

namespace Acontplus.Persistence.SqlServer.Configurations;

/// <summary>
/// Provides methods for registering EF Core entity types with the SQL Server model builder.
/// </summary>
public static class BaseEntityRegistration
{
    private static readonly EntityRegistrationProvider RegistrationProvider = new AuditableEntityRegistrationProvider(
        typeof(BaseEntityTypeConfiguration<>),
        "must be a concrete class inheriting from AuditableEntity<>");

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
        RegistrationProvider.Register(modelBuilder, dbContextType, nameMap, customConfigurations, entityTypes);

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        RegistrationProvider.Register(modelBuilder, dbContextType, entityTypes);

    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public static void RegisterEntitiesWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas) =>
        RegistrationProvider.RegisterWithSchemas(modelBuilder, dbContextType, entitySchemas);

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public static void RegisterEntitiesWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs) =>
        RegistrationProvider.RegisterWithNames(modelBuilder, dbContextType, nameConfigs);

    /// <summary>
    /// Registers entities with specific custom configurations.
    /// </summary>
    public static void RegisterEntitiesWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        RegistrationProvider.RegisterWithCustomConfigurations(modelBuilder, dbContextType, customConfigurations, entityTypes);
}

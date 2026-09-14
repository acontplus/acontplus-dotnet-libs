using System.Reflection;

namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Provides methods for registering simple entities with the Entity Framework ModelBuilder,
/// supporting custom schema/table names and entity configurations.
/// </summary>
public static class SimpleEntityRegistration
{
    /// <summary>
    /// Gets the primary key type for an entity that inherits from Entity&lt;TKey&gt;
    /// </summary>
    private static Type GetPrimaryKeyType(Type entityType)
    {
        // Look for Entity<TKey> in the inheritance chain
        var currentType = entityType;
        while (currentType != null)
        {
            if (currentType.IsGenericType &&
                currentType.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return currentType.GetGenericArguments()[0]; // Return TKey
            }
            currentType = currentType.BaseType;
        }
        // Fallback to common key types if not found
        return typeof(int);
    }

    /// <summary>
    /// Registers non-auditable entities with the ModelBuilder, applying base configurations,
    /// custom schema/table names, and optional specific entity configurations.
    /// </summary>
    public static void RegisterEntities(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)> nameMap,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes)
    {
        foreach (var entityType in entityTypes)
        {
            RegisterSingleEntity(modelBuilder, dbContextType, nameMap, customConfigurations, entityType);
        }
    }

    /// <summary>
    /// Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null!, null!, entityTypes);

    private static void RegisterSingleEntity(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        Type entityType)
    {
        if (!IsValidEntity(entityType))
        {
            Console.WriteLine(
                $"Skipping type {entityType.Name} as it's not a valid simple entity (must be a concrete class with an Id property).");
            return;
        }

        var entityBuilder = modelBuilder.Entity(entityType);
        var naming = ResolveTableAndSchema(entityType, dbContextType, nameMap);
        ApplyTableAndSchema(entityBuilder, naming);

        ApplyBaseConfiguration(modelBuilder, entityType);
        ApplyCustomConfiguration(modelBuilder, entityType, customConfigurations);
    }

    private static bool IsValidEntity(Type entityType) =>
        entityType.IsClass && !entityType.IsAbstract && HasIdProperty(entityType);

    private readonly record struct TableNamingInfo(
        string TableName,
        string? SchemaName,
        bool IsTableExplicit,
        bool IsSchemaExplicit);

    private static TableNamingInfo ResolveTableAndSchema(
        Type entityType,
        Type? dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap)
    {
        string? tableName = null;
        string? schemaName = null;
        var isTableExplicit = false;
        var isSchemaExplicit = false;

        // 1. Prioritize nameMap
        if (nameMap != null && nameMap.TryGetValue(entityType, out var mapConfig))
        {
            if (mapConfig.table != null)
            {
                tableName = mapConfig.table;
                isTableExplicit = true;
            }

            if (mapConfig.schema != null)
            {
                schemaName = mapConfig.schema;
                isSchemaExplicit = true;
            }
        }

        // 2. Check [Table] attribute
        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        if (tableAttribute != null)
        {
            if (!isTableExplicit && tableAttribute.Name != null)
            {
                tableName = tableAttribute.Name;
                isTableExplicit = true;
            }

            if (!isSchemaExplicit && tableAttribute.Schema != null)
            {
                schemaName = tableAttribute.Schema;
                isSchemaExplicit = true;
            }
        }

        // 3. Fallback to DbSet property or class name
        if (tableName == null)
        {
            tableName = ResolveDbSetTableName(dbContextType, entityType) ?? entityType.Name;
        }

        return new TableNamingInfo(tableName, schemaName, isTableExplicit, isSchemaExplicit);
    }

    private static string? ResolveDbSetTableName(Type? dbContextType, Type entityType)
    {
        if (dbContextType == null || !typeof(DbContext).IsAssignableFrom(dbContextType))
            return null;

        var dbSetProperty = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                 p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                 p.PropertyType.GetGenericArguments()[0] == entityType);

        return dbSetProperty?.Name;
    }

    private static void ApplyTableAndSchema(EntityTypeBuilder entityBuilder, TableNamingInfo naming)
    {
        if (naming.IsTableExplicit)
        {
            entityBuilder.ToTable(naming.TableName, naming.SchemaName);
        }
        else if (naming.IsSchemaExplicit)
        {
            entityBuilder.Metadata.SetSchema(naming.SchemaName);
        }
    }

    private static void ApplyBaseConfiguration(ModelBuilder modelBuilder, Type entityType)
    {
        try
        {
            var baseConfigurationType = typeof(SimpleEntityTypeConfiguration<>).MakeGenericType(entityType);
            var baseConfiguration = Activator.CreateInstance(baseConfigurationType);
            modelBuilder.ApplyConfiguration((dynamic)baseConfiguration!);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Warning: Could not apply base configuration for entity {entityType.Name}: {ex.Message}");
        }
    }

    private static void ApplyCustomConfiguration(
        ModelBuilder modelBuilder,
        Type entityType,
        Dictionary<Type, Type>? customConfigurations)
    {
        if (customConfigurations == null || !customConfigurations.TryGetValue(entityType, out var customConfigType))
            return;

        if (!typeof(IEntityTypeConfiguration<>).MakeGenericType(entityType).IsAssignableFrom(customConfigType))
        {
            Console.WriteLine(
                $"Warning: Custom configuration type {customConfigType.Name} for entity {entityType.Name} does not implement IEntityTypeConfiguration<{entityType.Name}>. Skipping custom configuration for this entity.");
            return;
        }

        try
        {
            var customConfigInstance = Activator.CreateInstance(customConfigType);
            modelBuilder.ApplyConfiguration((dynamic)customConfigInstance!);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Warning: Could not apply custom configuration for entity {entityType.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to check if a type has an Id property (for simple entities)
    /// </summary>
    private static bool HasIdProperty(Type entityType)
    {
        var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return idProperty != null && idProperty.CanRead && idProperty.CanWrite;
    }


    /// <summary>
    /// Registers entities, explicitly setting schemas for specified types.
    /// </summary>
    public static void RegisterEntitiesWithSchemas(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema)[] entitySchemas)
    {
        var schemaMap = entitySchemas.ToDictionary(
            x => x.entityType,
            x => (x.schema, table: (string?)null));
        var entityTypes = entitySchemas.Select(x => x.entityType).ToArray();
        RegisterEntities(modelBuilder, dbContextType, schemaMap!, null!, entityTypes);
    }

    /// <summary>
    /// Registers entities, explicitly setting schema and/or table names for specified types.
    /// </summary>
    public static void RegisterEntitiesWithNames(
        ModelBuilder modelBuilder,
        Type dbContextType,
        params (Type entityType, string schema, string table)[] nameConfigs)
    {
        var nameMap = nameConfigs.ToDictionary(
            x => x.entityType,
            x => (x.schema, x.table));
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

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Acontplus.Persistence.SqlServer.Configurations;

/// <summary>
/// Provides methods for registering simple (non-auditable) EF Core entity types with the SQL Server model builder.
/// </summary>
public static class SimpleEntityRegistration
{
    /// <summary>
    ///     Gets the primary key type for an entity that inherits from Entity&lt;TKey&gt;
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
    ///     Registers non-auditable entities with the ModelBuilder, applying base configurations,
    ///     custom schema/table names, and optional specific entity configurations.
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
            // Check if entity is a valid simple entity (concrete class with public Id property)
            var isValidEntity = entityType.IsClass &&
                                !entityType.IsAbstract &&
                                HasIdProperty(entityType);
            if (!isValidEntity)
            {
                Console.WriteLine(
                    $"Skipping type {entityType.Name} as it's not a valid simple entity (must be a concrete class with an Id property).");
                continue;
            }

            var entityBuilder = modelBuilder.Entity(entityType);
            string? determinedTableName = null;
            string? determinedSchemaName = null;
            var isTableNameExplicitlyProvided = false;
            var isSchemaNameExplicitlyProvided = false;

            // 1. Prioritize nameMap for both table and schema
            if (nameMap != null && nameMap.TryGetValue(entityType, out var mapConfig))
            {
                if (mapConfig.table != null)
                {
                    determinedTableName = mapConfig.table;
                    isTableNameExplicitlyProvided = true;
                }

                if (mapConfig.schema != null)
                {
                    determinedSchemaName = mapConfig.schema;
                    isSchemaNameExplicitlyProvided = true;
                }
            }

            // 2. If not set by nameMap, check [Table] attribute
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null)
            {
                if (!isTableNameExplicitlyProvided && tableAttribute.Name != null)
                {
                    determinedTableName = tableAttribute.Name;
                    isTableNameExplicitlyProvided = true;
                }

                if (!isSchemaNameExplicitlyProvided && tableAttribute.Schema != null)
                {
                    determinedSchemaName = tableAttribute.Schema;
                    isSchemaNameExplicitlyProvided = true;
                }
            }

            // 3. If table name is still null, try to get it from DbSet property, then fallback to class name
            if (determinedTableName == null)
            {
                if (dbContextType != null && typeof(DbContext).IsAssignableFrom(dbContextType))
                {
                    var dbSetProperty = dbContextType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                             p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                             p.PropertyType.GetGenericArguments()[0] == entityType);
                    if (dbSetProperty != null)
                    {
                        determinedTableName = dbSetProperty.Name;
                    }
                }

                determinedTableName ??= entityType.Name;
            }

            // Apply the determined name and schema based on explicit intent
            if (isTableNameExplicitlyProvided)
            {
                entityBuilder.ToTable(determinedTableName!, determinedSchemaName);
            }
            else if (isSchemaNameExplicitlyProvided)
            {
                entityBuilder.Metadata.SetSchema(determinedSchemaName);
            }

            // 4. Apply the EntityTypeConfiguration for common properties/conventions
            try
            {
                var keyType = GetPrimaryKeyType(entityType);
                var baseConfigurationType = typeof(SimpleEntityTypeConfiguration<>).MakeGenericType(entityType);
                var baseConfiguration = Activator.CreateInstance(baseConfigurationType);
                modelBuilder.ApplyConfiguration((dynamic)baseConfiguration!);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Warning: Could not apply base configuration for entity {entityType.Name}: {ex.Message}");
            }

            // 5. Apply any specific custom configuration for this entity
            if (customConfigurations != null && customConfigurations.TryGetValue(entityType, out var customConfigType))
            {
                if (!typeof(IEntityTypeConfiguration<>).MakeGenericType(entityType).IsAssignableFrom(customConfigType))
                {
                    Console.WriteLine(
                        $"Warning: Custom configuration type {customConfigType.Name} for entity {entityType.Name} does not implement IEntityTypeConfiguration<{entityType.Name}>. Skipping custom configuration for this entity.");
                }
                else
                {
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
            }
        }
    }

    /// <summary>
    ///     Helper method to check if a type has an Id property (for simple entities)
    /// </summary>
    private static bool HasIdProperty(Type entityType)
    {
        var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return idProperty != null && idProperty.CanRead && idProperty.CanWrite;
    }

    /// <summary>
    ///     Helper method to get the type of the Id property
    /// </summary>
    private static Type GetIdPropertyType(Type entityType)
    {
        var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return idProperty?.PropertyType ?? typeof(int);
    }

    /// <summary>
    ///     Registers entities with default conventions and base configuration.
    /// </summary>
    public static void RegisterEntities(ModelBuilder modelBuilder, Type dbContextType, params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null!, null!, entityTypes);

    /// <summary>
    ///     Registers entities, explicitly setting schemas for specified types.
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
    ///     Registers entities, explicitly setting schema and/or table names for specified types.
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
    ///     Registers entities with specific custom configurations.
    /// </summary>
    public static void RegisterEntitiesWithCustomConfigurations(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, Type> customConfigurations,
        params Type[] entityTypes) =>
        RegisterEntities(modelBuilder, dbContextType, null!, customConfigurations, entityTypes);
}

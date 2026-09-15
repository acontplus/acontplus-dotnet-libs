using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Acontplus.Core.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acontplus.Persistence.Common.Configurations;

/// <summary>
/// Provides unified helper methods for registering EF Core entity types with a model builder.
/// Handles table naming, schema resolution, base configurations, and custom entity configurations.
/// </summary>
public static class EntityRegistrationHelper
{
    private readonly record struct TableNamingInfo(
        string TableName,
        string? SchemaName,
        bool IsTableExplicit,
        bool IsSchemaExplicit);

    /// <summary>
    /// Registers entities with the ModelBuilder using a specified base configuration type definition and entity validator.
    /// </summary>
    /// <param name="modelBuilder">The EF Core ModelBuilder instance.</param>
    /// <param name="dbContextType">The DbContext type, used to resolve DbSet property names as default table names.</param>
    /// <param name="nameMap">Optional explicit mapping of entity types to schema and table names.</param>
    /// <param name="customConfigurations">Optional explicit mapping of entity types to custom IEntityTypeConfiguration types.</param>
    /// <param name="baseConfigurationTypeDefinition">The open generic type definition for the base entity configuration (e.g. BaseEntityTypeConfiguration&lt;&gt;).</param>
    /// <param name="isValidEntity">Predicate function determining if a type is valid for registration.</param>
    /// <param name="invalidEntityMessage">Description of the entity criteria for logging skipped types.</param>
    /// <param name="entityTypes">The entity types to register.</param>
    public static void RegisterEntities(
        ModelBuilder modelBuilder,
        Type dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        Type baseConfigurationTypeDefinition,
        Func<Type, bool> isValidEntity,
        string invalidEntityMessage,
        params Type[] entityTypes)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(isValidEntity);

        foreach (var entityType in entityTypes)
        {
            RegisterSingleEntity(
                modelBuilder,
                dbContextType,
                nameMap,
                customConfigurations,
                baseConfigurationTypeDefinition,
                isValidEntity,
                invalidEntityMessage,
                entityType);
        }
    }

    /// <summary>
    /// Determines whether a type is a valid auditable entity (concrete class inheriting from <see cref="BaseEntity"/>).
    /// </summary>
    /// <param name="entityType">The type to check.</param>
    /// <returns><c>true</c> if valid; otherwise <c>false</c>.</returns>
    public static bool IsValidAuditableEntity(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return entityType.IsClass && !entityType.IsAbstract && typeof(BaseEntity).IsAssignableFrom(entityType);
    }

    /// <summary>
    /// Determines whether a type is a valid simple entity (concrete class not inheriting from <see cref="BaseEntity"/> with a readable/writable Id property).
    /// </summary>
    /// <param name="entityType">The type to check.</param>
    /// <returns><c>true</c> if valid; otherwise <c>false</c>.</returns>
    public static bool IsValidSimpleEntity(Type entityType)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        return entityType.IsClass &&
               !entityType.IsAbstract &&
               !typeof(BaseEntity).IsAssignableFrom(entityType) &&
               HasIdProperty(entityType);
    }

    private static bool HasIdProperty(Type entityType)
    {
        var idProperty = entityType.GetProperty("Id");
        return idProperty != null && idProperty.CanRead && idProperty.CanWrite;
    }

    private static void RegisterSingleEntity(
        ModelBuilder modelBuilder,
        Type? dbContextType,
        Dictionary<Type, (string schema, string table)>? nameMap,
        Dictionary<Type, Type>? customConfigurations,
        Type baseConfigurationTypeDefinition,
        Func<Type, bool> isValidEntity,
        string invalidEntityMessage,
        Type entityType)
    {
        if (!isValidEntity(entityType))
        {
            Console.WriteLine(
                $"Skipping type {entityType.Name} as it's not a valid entity ({invalidEntityMessage}).");
            return;
        }

        var entityBuilder = modelBuilder.Entity(entityType);
        var naming = ResolveTableAndSchema(entityType, dbContextType, nameMap);
        ApplyTableAndSchema(entityBuilder, naming);

        ApplyBaseConfiguration(modelBuilder, entityType, baseConfigurationTypeDefinition);
        ApplyCustomConfiguration(modelBuilder, entityType, customConfigurations);
    }

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

    private static void ApplyBaseConfiguration(ModelBuilder modelBuilder, Type entityType, Type baseConfigurationTypeDefinition)
    {
        try
        {
            var baseConfigurationType = baseConfigurationTypeDefinition.MakeGenericType(entityType);
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
    /// Creates a dictionary mapping entity types to schema names.
    /// </summary>
    /// <param name="entitySchemas">Array of entity type and schema tuples.</param>
    /// <returns>A dictionary of entity type to schema/table tuples.</returns>
    public static Dictionary<Type, (string schema, string table)> CreateSchemaMap(
        params (Type entityType, string schema)[] entitySchemas)
    {
        ArgumentNullException.ThrowIfNull(entitySchemas);
        return entitySchemas.ToDictionary(
            x => x.entityType,
            x => (x.schema, table: (string)null!));
    }

    /// <summary>
    /// Creates a dictionary mapping entity types to schema and table names.
    /// </summary>
    /// <param name="nameConfigs">Array of entity type, schema, and table name tuples.</param>
    /// <returns>A dictionary of entity type to schema/table tuples.</returns>
    public static Dictionary<Type, (string schema, string table)> CreateNameMap(
        params (Type entityType, string schema, string table)[] nameConfigs)
    {
        ArgumentNullException.ThrowIfNull(nameConfigs);
        return nameConfigs.ToDictionary(
            x => x.entityType,
            x => (x.schema, x.table));
    }
}

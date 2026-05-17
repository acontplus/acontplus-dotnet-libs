namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Base configuration class for Entity Framework Core entity type configurations.
/// Provides common configuration for entities inheriting from <see cref="BaseEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public class BaseEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity

{
    /// <summary>
    /// Configures the entity type.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ConfigurePrimaryKey(builder);
        ConfigureTimestamps(builder);
        ConfigureStatusFields(builder);
        ConfigureExternalUserTracking(builder);
        ConfigureSoftDeleteAndIndexes(builder);
    }

    /// <summary>
    /// Configures the primary key for the entity.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigurePrimaryKey(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseIdentityByDefaultColumn() // PostgreSQL IDENTITY column
            .ValueGeneratedOnAdd();
    }

    /// <summary>
    /// Configures timestamp properties (CreatedAt, UpdatedAt, DeletedAt).
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigureTimestamps(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);
    }

    /// <summary>
    /// Configures status-related boolean fields (IsActive, IsDeleted, IsMobileRequest).
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigureStatusFields(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsMobileRequest)
            .HasDefaultValue(false)
            .IsRequired();
    }

    /// <summary>
    /// Configures external user tracking fields (CreatedBy, UpdatedBy, DeletedBy).
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigureExternalUserTracking(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedBy)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.Property(x => x.DeletedBy)
            .IsRequired(false)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");
    }

    /// <summary>
    /// Configures soft delete query filter and indexes.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigureSoftDeleteAndIndexes(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configures database indexes for the entity, including standard and partial indexes.
    /// </summary>
    /// <param name="builder">The builder used to configure the entity type.</param>
    protected virtual void ConfigureIndexes(EntityTypeBuilder<TEntity> builder)
    {
        var tableName = typeof(TEntity).Name.ToSnakeCase();

        // Standard indexes
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName($"ix_{tableName}_created_at");

        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName($"ix_{tableName}_created_by_user_id");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName($"ix_{tableName}_is_deleted");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName($"ix_{tableName}_is_active");

        // Composite index for active/non-deleted records
        builder.HasIndex(x => new { x.IsActive, x.IsDeleted })
            .HasDatabaseName($"ix_{tableName}_status");

        // Partial index for active records
        builder.HasIndex(x => x.CreatedAt)
            .HasFilter("is_deleted = false")
            .HasDatabaseName($"ix_{tableName}_created_at_active");

        // Consider adding these indexes if needed:
        // builder.HasIndex(x => x.UpdatedAt)
        //     .HasDatabaseName($"ix_{tableName}_updated_at");
        //
        // builder.HasIndex(x => new { x.IsActive, x.UpdatedAt })
        //     .HasDatabaseName($"ix_{tableName}_active_updated");
    }
}

namespace Acontplus.Persistence.Common.Configurations;

/// <summary>
/// Provider-agnostic base EF Core entity type configuration for auditable entities.
/// Provides common status fields, soft-delete query filters, and tracking column configuration.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public abstract class BaseEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// Applies base configuration including keys, timestamps, status fields, user tracking, and indexes.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        ConfigurePrimaryKey(builder);
        ConfigureTimestamps(builder);
        ConfigureStatusFields(builder);
        ConfigureExternalUserTracking(builder);
        ConfigureSoftDeleteAndIndexes(builder);
    }

    /// <summary>
    /// Configures the primary key and value generation strategy.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected abstract void ConfigurePrimaryKey(EntityTypeBuilder<TEntity> builder);

    /// <summary>
    /// Configures timestamp columns for creation, update, and deletion tracking.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected abstract void ConfigureTimestamps(EntityTypeBuilder<TEntity> builder);

    /// <summary>
    /// Configures status-related boolean fields (IsActive, IsDeleted, IsMobileRequest).
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
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
    /// Configures external user-tracking columns (CreatedBy, UpdatedBy, DeletedBy).
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected virtual void ConfigureExternalUserTracking(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedBy)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false)
            .HasMaxLength(100);

        builder.Property(x => x.DeletedBy)
            .IsRequired(false)
            .HasMaxLength(100);
    }

    /// <summary>
    /// Applies the global soft-delete query filter and configures database indexes.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected virtual void ConfigureSoftDeleteAndIndexes(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);
        ConfigureIndexes(builder);
    }

    /// <summary>
    /// Configures database indexes for common query patterns.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    protected abstract void ConfigureIndexes(EntityTypeBuilder<TEntity> builder);
}

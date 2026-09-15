namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Base configuration class for Entity Framework Core entity type configurations in PostgreSQL.
/// Provides common configuration for entities inheriting from <see cref="BaseEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type being configured.</typeparam>
public class BaseEntityTypeConfiguration<TEntity> : Common.Configurations.BaseEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    /// <inheritdoc />
    protected override void ConfigurePrimaryKey(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseIdentityByDefaultColumn() // PostgreSQL IDENTITY column
            .ValueGeneratedOnAdd();
    }

    /// <inheritdoc />
    protected override void ConfigureTimestamps(EntityTypeBuilder<TEntity> builder)
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

    /// <inheritdoc />
    protected override void ConfigureExternalUserTracking(EntityTypeBuilder<TEntity> builder)
    {
        base.ConfigureExternalUserTracking(builder);

        builder.Property(x => x.CreatedBy)
            .HasColumnType("varchar(100)");

        builder.Property(x => x.UpdatedBy)
            .HasColumnType("varchar(100)");

        builder.Property(x => x.DeletedBy)
            .HasColumnType("varchar(100)");
    }

    /// <inheritdoc />
    protected override void ConfigureIndexes(EntityTypeBuilder<TEntity> builder)
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
    }
}

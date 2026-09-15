using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acontplus.Persistence.SqlServer.Configurations;

/// <summary>
/// Base EF Core entity type configuration for auditable SQL Server entities.
/// </summary>
/// <typeparam name="TEntity">The entity type to configure.</typeparam>
public class BaseEntityTypeConfiguration<TEntity> : Common.Configurations.BaseEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    /// <inheritdoc />
    protected override void ConfigurePrimaryKey(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
    }

    /// <inheritdoc />
    protected override void ConfigureTimestamps(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasPrecision(7)
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasPrecision(7)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnType("datetime2")
            .HasPrecision(7)
            .IsRequired(false);
    }

    /// <inheritdoc />
    protected override void ConfigureExternalUserTracking(EntityTypeBuilder<TEntity> builder)
    {
        base.ConfigureExternalUserTracking(builder);

        builder.Property(x => x.CreatedBy)
            .IsUnicode(false);

        builder.Property(x => x.UpdatedBy)
            .IsUnicode(false);

        builder.Property(x => x.DeletedBy)
            .IsUnicode(false);
    }

    /// <inheritdoc />
    protected override void ConfigureIndexes(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedAt");

        builder.HasIndex(x => x.CreatedByUserId)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedByUserId");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsDeleted");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_IsActive");

        builder.HasIndex(x => new { x.IsActive, x.IsDeleted })
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_Status");
    }
}

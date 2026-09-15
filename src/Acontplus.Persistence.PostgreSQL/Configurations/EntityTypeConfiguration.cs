namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Base configuration for non-auditable entities.
/// </summary>
public class EntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity

{
    /// <summary>
    /// Configures the primary key for the entity.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public virtual void Configure(EntityTypeBuilder<TEntity> builder) =>
        builder.HasKey(x => x.Id);
}

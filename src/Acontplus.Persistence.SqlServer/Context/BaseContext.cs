namespace Acontplus.Persistence.SqlServer.Context;

/// <summary>
/// Options that control SQL Server-specific model builder behaviour.
/// </summary>
public class SqlServerModelBuilderOptions
{
  /// <summary>Gets or sets a value indicating whether decimal precision/scale conversion is applied. Defaults to <c>true</c>.</summary>
  public bool EnableDecimalConversion { get; set; } = true;
  /// <summary>Gets or sets a value indicating whether string properties are mapped as non-Unicode. Defaults to <c>true</c>.</summary>
  public bool EnableNonUnicodeStrings { get; set; } = true;
}

/// <summary>
/// Base EF Core database context for SQL Server with support for domain events, timestamp auditing, and soft deletes.
/// </summary>
/// <remarks>
/// Audit identity fields (<c>CreatedBy</c>, <c>CreatedByUserId</c>, <c>UpdatedBy</c>, <c>UpdatedByUserId</c>,
/// <c>DeletedBy</c>, <c>DeletedByUserId</c>, <c>IsMobileRequest</c>) are populated automatically by
/// <see cref="Acontplus.Persistence.SqlServer.Interceptors.AuditSaveChangesInterceptor"/>, which is registered as a singleton by <c>AddSqlServerPersistence</c>.
/// This context only manages timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>DeletedAt</c>) and the
/// hard-delete → soft-delete conversion.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public abstract class BaseContext(DbContextOptions options) : DbContext(options)
{
  private readonly IDomainEventDispatcher? _eventDispatcher;
  private readonly SqlServerModelBuilderOptions _sqlServerOptions = new();

  /// <summary>
  /// Initializes a new instance with a domain event dispatcher.
  /// </summary>
  /// <param name="options">The options to be used by the DbContext.</param>
  /// <param name="eventDispatcher">The domain event dispatcher.</param>
  protected BaseContext(DbContextOptions options, IDomainEventDispatcher eventDispatcher)
    : this(options)
  {
    _eventDispatcher = eventDispatcher;
  }

  /// <inheritdoc/>
  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await DispatchDomainEventsAsync();
    UpdateTimestamps();
    HandleSoftDeletes();
    return await base.SaveChangesAsync(cancellationToken);
  }

  /// <inheritdoc/>
  public override int SaveChanges()
  {
    DispatchDomainEventsAsync().GetAwaiter().GetResult();
    UpdateTimestamps();
    HandleSoftDeletes();
    return base.SaveChanges();
  }

  private async Task DispatchDomainEventsAsync()
  {
    if (_eventDispatcher is null) return;

    var entitiesWithEvents = ChangeTracker
      .Entries<IEntityWithDomainEvents>()
      .Where(e => e.Entity.DomainEvents.Any())
      .Select(e => e.Entity)
      .ToList();

    foreach (var entity in entitiesWithEvents)
    {
      var events = entity.DomainEvents.ToArray();
      entity.ClearDomainEvents();
      foreach (var domainEvent in events)
        await _eventDispatcher.Dispatch(domainEvent);
    }
  }

  /// <summary>
  /// Stamps <c>CreatedAt</c> on added entities and <c>UpdatedAt</c> on modified entities.
  /// User-identity fields are handled by <see cref="Acontplus.Persistence.SqlServer.Interceptors.AuditSaveChangesInterceptor"/>.
  /// </summary>
  private void UpdateTimestamps()
  {
    foreach (var entry in ChangeTracker.Entries<BaseEntity>()
               .Where(e => e.State is EntityState.Added or EntityState.Modified))
    {
      if (entry.State == EntityState.Added)
        entry.Entity.CreatedAt = DateTime.UtcNow;
      else
        entry.Entity.UpdatedAt = DateTime.UtcNow;
    }
  }

  /// <summary>
  /// Converts hard-deletes into soft-deletes and handles restore logic.
  /// User-identity fields on deletion (<c>DeletedBy</c>, <c>DeletedByUserId</c>) are handled
  /// by <see cref="Acontplus.Persistence.SqlServer.Interceptors.AuditSaveChangesInterceptor"/>.
  /// </summary>
  private void HandleSoftDeletes()
  {
    var entries = ChangeTracker.Entries<BaseEntity>()
      .Where(e => e.State == EntityState.Deleted ||
                  e.Property(nameof(BaseEntity.IsDeleted)).IsModified)
      .ToList();

    foreach (var entry in entries)
    {
      var entity = entry.Entity;

      if (entry.State == EntityState.Deleted || entity.IsDeleted)
      {
        // Convert hard-delete into a soft-delete
        entry.State = EntityState.Modified;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = DateTime.UtcNow;
      }
      else if (!entity.IsDeleted && entity.DeletedAt is not null)
      {
        // Restore from soft-delete — clear all deletion stamps
        entity.DeletedAt = null;
        entity.DeletedByUserId = null;
        entity.DeletedBy = null;
        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
      }
    }
  }

  /// <inheritdoc/>
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    ConfigureGlobalFilters(modelBuilder);
    ConfigureDateTimeProperties(modelBuilder);

    if (Database.IsSqlServer())
      ApplySqlServerConfigurations(modelBuilder);
  }

  private static void ConfigureGlobalFilters(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
      {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = Expression.Lambda(Expression.Not(property), parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(condition);
      }
    }
  }

  /// <summary>
  /// Applies UTC-aware value converters to all <see cref="DateTime"/> and nullable DateTime properties.
  /// </summary>
  protected virtual void ConfigureDateTimeProperties(ModelBuilder builder)
  {
    foreach (var entityType in builder.Model.GetEntityTypes())
    {
      foreach (var property in entityType.GetProperties()
                 .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
      {
        if (property.ClrType == typeof(DateTime))
        {
          builder.Entity(entityType.ClrType)
            .Property<DateTime>(property.Name)
            .HasConversion(
              v => v.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                : v.ToUniversalTime(),
              v => v);
        }
        else
        {
          builder.Entity(entityType.ClrType)
            .Property<DateTime?>(property.Name)
            .HasConversion(
              v => v.HasValue
                ? v.Value.Kind == DateTimeKind.Unspecified
                  ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                  : v.Value.ToUniversalTime()
                : (DateTime?)null,
              v => v);
        }
      }
    }
  }

  /// <summary>
  /// Applies SQL Server-specific model configurations such as decimal precision and non-Unicode string mappings.
  /// </summary>
  protected virtual void ApplySqlServerConfigurations(ModelBuilder builder)
  {
    if (_sqlServerOptions.EnableDecimalConversion)
    {
      foreach (var property in builder.Model.GetEntityTypes()
                 .SelectMany(t => t.GetProperties())
                 .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
      {
        property.SetPrecision(18);
        property.SetScale(2);
      }
    }

    if (_sqlServerOptions.EnableNonUnicodeStrings)
    {
      foreach (var property in builder.Model.GetEntityTypes()
                 .SelectMany(t => t.GetProperties())
                 .Where(p => p.ClrType == typeof(string) && p.GetColumnType() == null))
      {
        property.SetIsUnicode(false);
      }
    }
  }
}

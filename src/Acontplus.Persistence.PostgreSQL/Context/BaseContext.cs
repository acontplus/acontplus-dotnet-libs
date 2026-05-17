namespace Acontplus.Persistence.PostgreSQL.Context;

/// <summary>
/// Base context class for Entity Framework Core database contexts with support for domain events, auditing, and soft deletes.
/// </summary>
/// <param name="options">The options to be used by the DbContext.</param>
public abstract class BaseContext(DbContextOptions options) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _eventDispatcher;
    private readonly IAuditContext? _auditContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class with domain event dispatcher.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="eventDispatcher">The domain event dispatcher for handling domain events.</param>
    protected BaseContext(DbContextOptions options, IDomainEventDispatcher eventDispatcher)
        : this(options)
    {
        _eventDispatcher = eventDispatcher;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class with domain event dispatcher and audit context.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="eventDispatcher">The domain event dispatcher for handling domain events.</param>
    /// <param name="auditContext">The audit context for tracking user information.</param>
    protected BaseContext(
        DbContextOptions options,
        IDomainEventDispatcher eventDispatcher,
        IAuditContext auditContext)
        : this(options, eventDispatcher)
    {
        _auditContext = auditContext;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class with audit context.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="auditContext">The audit context for tracking user information.</param>
    protected BaseContext(DbContextOptions options, IAuditContext auditContext)
        : this(options)
    {
        _auditContext = auditContext;
    }

    /// <summary>
    /// Saves all changes, dispatches domain events, updates audit fields, and handles soft deletes asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();
        // Only update audit fields and handle soft deletes for auditable entities
        await UpdateAuditFieldsAsync();
        await HandleSoftDeletesAsync();

        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Saves all changes, dispatches domain events, updates audit fields, and handles soft deletes synchronously.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        DispatchDomainEventsAsync().GetAwaiter().GetResult();
        // Only update audit fields and handle soft deletes for auditable entities
        UpdateAuditFieldsAsync().GetAwaiter().GetResult();
        HandleSoftDeletesAsync().GetAwaiter().GetResult();

        return base.SaveChanges();
    }

    private async Task DispatchDomainEventsAsync()
    {
        if (_eventDispatcher == null) return;

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
            {
                await _eventDispatcher.Dispatch(domainEvent);
            }
        }
    }

    private Task UpdateAuditFieldsAsync()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified))
            .ToList();

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedByUserId = _auditContext?.UserId;
                entity.CreatedBy = _auditContext?.UserName;
                entity.IsMobileRequest = _auditContext?.IsMobile ?? entity.IsMobileRequest;
            }
            else
            {
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedByUserId = _auditContext?.UserId;
                entity.UpdatedBy = _auditContext?.UserName;
            }
        }

        return Task.CompletedTask;
    }

    private Task HandleSoftDeletesAsync()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                        (e.State == EntityState.Deleted ||
                         e.Property(nameof(BaseEntity.IsDeleted)).IsModified))
            .ToList();

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Deleted || entity.IsDeleted)
            {
                // Convert hard-delete into a soft-delete
                entry.State = EntityState.Modified;
                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedByUserId = _auditContext?.UserId;
                entity.DeletedBy = _auditContext?.UserName;
            }
            else if (!entity.IsDeleted && entity.DeletedAt is not null)
            {
                // Restore from soft-delete — clear all deletion stamps
                entity.DeletedAt = null;
                entity.DeletedByUserId = null;
                entity.DeletedBy = null;
                entity.IsActive = true;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedByUserId = _auditContext?.UserId;
                entity.UpdatedBy = _auditContext?.UserName;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures the model by applying global soft-delete filters and date-time conversions.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureGlobalFilters(modelBuilder);
        ConfigureDateTimeProperties(modelBuilder);
    }

    private static void ConfigureGlobalFilters(ModelBuilder builder)
    {
        // Only apply soft delete filter to auditable entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var condition = Expression.Lambda(Expression.Not(property), parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(condition);
            }
        }
    }

    /// <summary>
    /// Applies UTC-aware value converters to all <see cref="DateTime"/> and <see cref="Nullable{T}"/> DateTime properties.
    /// </summary>
    /// <param name="builder">The model builder.</param>
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
                            v => v
                        );
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
                            v => v
                        );
                }
            }
        }
    }
}

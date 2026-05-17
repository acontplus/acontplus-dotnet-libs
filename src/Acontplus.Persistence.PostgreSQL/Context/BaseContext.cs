namespace Acontplus.Persistence.PostgreSQL.Context;

public abstract class BaseContext(DbContextOptions options) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _eventDispatcher;
    private readonly IAuditContext? _auditContext;

    protected BaseContext(DbContextOptions options, IDomainEventDispatcher eventDispatcher)
        : this(options)
    {
        _eventDispatcher = eventDispatcher;
    }

    protected BaseContext(
        DbContextOptions options,
        IDomainEventDispatcher eventDispatcher,
        IAuditContext auditContext)
        : this(options, eventDispatcher)
    {
        _auditContext = auditContext;
    }

    protected BaseContext(DbContextOptions options, IAuditContext auditContext)
        : this(options)
    {
        _auditContext = auditContext;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();
        // Only update audit fields and handle soft deletes for auditable entities
        await UpdateAuditFieldsAsync();
        await HandleSoftDeletesAsync();

        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

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

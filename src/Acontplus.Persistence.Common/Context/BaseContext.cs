using System.Linq.Expressions;
using Acontplus.Core.Domain.Common.Entities;
using Acontplus.Core.Domain.Common.Events;

namespace Acontplus.Persistence.Common.Context;

/// <summary>
/// Base EF Core database context with support for domain events, timestamp auditing, and soft deletes.
/// </summary>
/// <remarks>
/// Audit identity fields (<c>CreatedBy</c>, <c>CreatedByUserId</c>, <c>UpdatedBy</c>, <c>UpdatedByUserId</c>,
/// <c>DeletedBy</c>, <c>DeletedByUserId</c>, <c>IsMobileRequest</c>) are populated automatically by
/// an audit interceptor registered as a singleton.
/// This context manages timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>DeletedAt</c>), domain events,
/// UTC datetime conversion, and the hard-delete → soft-delete conversion.
/// </remarks>
/// <param name="options">The options to be used by the DbContext.</param>
public abstract class BaseContext(DbContextOptions options) : DbContext(options)
{
    private readonly IDomainEventDispatcher? _eventDispatcher;

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
        await DispatchDomainEventsAsync(cancellationToken);
        UpdateTimestamps();
        HandleSoftDeletes();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override int SaveChanges()
    {
        DispatchDomainEventsAsync(CancellationToken.None).GetAwaiter().GetResult();
        UpdateTimestamps();
        HandleSoftDeletes();
        return base.SaveChanges();
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        if (_eventDispatcher is null) return;

        var entitiesWithEvents = ChangeTracker
            .Entries<IEntityWithDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _eventDispatcher.Dispatch(domainEvent);
            }
        }
    }

    /// <summary>
    /// Stamps <c>CreatedAt</c> on added entities and <c>UpdatedAt</c> on modified entities.
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
                entry.Entity.IsDeleted = true;
                entry.Entity.IsActive = false;
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
            else if (!entity.IsDeleted && entity.DeletedAt is not null)
            {
                // Restore from soft-delete — clear all deletion stamps
                entry.Entity.DeletedAt = null;
                entry.Entity.DeletedByUserId = null;
                entry.Entity.DeletedBy = null;
                entry.Entity.IsActive = true;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureGlobalFilters(modelBuilder);
        ConfigureDateTimeProperties(modelBuilder);
    }

    private static void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        var clrTypes = modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
            .Select(e => e.ClrType);

        foreach (var clrType in clrTypes)
        {
            var parameter = Expression.Parameter(clrType, "e");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var condition = Expression.Lambda(Expression.Not(property), parameter);
            modelBuilder.Entity(clrType).HasQueryFilter(condition);
        }
    }

    /// <summary>
    /// Applies UTC-aware value converters to all <see cref="DateTime"/> and nullable DateTime properties.
    /// </summary>
    /// <param name="builder">The ModelBuilder instance.</param>
    protected virtual void ConfigureDateTimeProperties(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            ConfigureEntityTypeDateTimeProperties(builder, entityType);
        }
    }

    private static void ConfigureEntityTypeDateTimeProperties(ModelBuilder builder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(DateTime))
            {
                ConfigureDateTimeProperty(builder, entityType, property.Name);
            }
            else if (property.ClrType == typeof(DateTime?))
            {
                ConfigureNullableDateTimeProperty(builder, entityType, property.Name);
            }
        }
    }

    private static void ConfigureDateTimeProperty(ModelBuilder builder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType, string propertyName)
    {
        builder.Entity(entityType.ClrType)
            .Property<DateTime>(propertyName)
            .HasConversion(
                v => v.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
                    : v.ToUniversalTime(),
                v => v);
    }

    private static void ConfigureNullableDateTimeProperty(ModelBuilder builder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType, string propertyName)
    {
        builder.Entity(entityType.ClrType)
            .Property<DateTime?>(propertyName)
            .HasConversion(
                v => ConvertToUtc(v),
                v => v);
    }

    private static DateTime? ConvertToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }
}

using Acontplus.Core.Abstractions.Context;
using Acontplus.Core.Domain.Common.Entities;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.Persistence.Common.Interceptors;

/// <summary>
/// A singleton EF Core interceptor that populates audit fields (<c>CreatedBy</c>,
/// <c>UpdatedBy</c>, <c>DeletedBy</c> and their UserId counterparts) on every
/// <see cref="BaseEntity"/> before changes are persisted.
/// </summary>
/// <remarks>
/// Resolves <see cref="IAuditContext"/> from a fresh DI scope on each save, so it is
/// safe to use with <c>AddDbContextPool</c> (singleton-compatible).
/// </remarks>
public class AuditSaveChangesInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        using var scope = _scopeFactory.CreateScope();
        var auditContext = scope.ServiceProvider.GetService<IAuditContext>();
        if (auditContext is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedByUserId = auditContext.UserId;
                    entry.Entity.CreatedBy = auditContext.UserName;
                    entry.Entity.IsMobileRequest = auditContext.IsMobile;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedByUserId = auditContext.UserId;
                    entry.Entity.UpdatedBy = auditContext.UserName;
                    if (entry.Entity.IsDeleted && entry.Entity.DeletedBy is null)
                    {
                        entry.Entity.DeletedByUserId = auditContext.UserId;
                        entry.Entity.DeletedBy = auditContext.UserName;
                    }
                    break;
            }
        }
    }
}

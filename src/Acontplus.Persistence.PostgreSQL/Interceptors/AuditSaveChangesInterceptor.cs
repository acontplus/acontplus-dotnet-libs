using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Acontplus.Persistence.PostgreSQL.Interceptors;

/// <summary>
/// A singleton EF Core interceptor that populates audit fields (<c>CreatedBy</c>,
/// <c>UpdatedBy</c>, <c>DeletedBy</c> and their UserId counterparts) on every
/// <see cref="BaseEntity"/> before changes are persisted.
/// </summary>
/// <remarks>
/// Registered automatically by <c>AddPostgresPersistence</c>.  Resolves
/// <see cref="IAuditContext"/> from a fresh DI scope on each save, so it is
/// safe to use with <c>AddDbContextPool</c> (singleton-compatible).
/// </remarks>
public sealed class AuditSaveChangesInterceptor(IServiceScopeFactory scopeFactory) : SaveChangesInterceptor
{
  /// <inheritdoc/>
  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    Apply(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  /// <inheritdoc/>
  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
  {
    Apply(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void Apply(DbContext? context)
  {
    if (context is null) return;

    using var scope = scopeFactory.CreateScope();
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

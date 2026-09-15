using Microsoft.Extensions.DependencyInjection;

namespace Acontplus.Persistence.PostgreSQL.Interceptors;

/// <summary>
/// A singleton EF Core interceptor that populates audit fields (<c>CreatedBy</c>,
/// <c>UpdatedBy</c>, <c>DeletedBy</c> and their UserId counterparts) on every
/// <see cref="Acontplus.Core.Domain.Common.Entities.BaseEntity"/> before changes are persisted.
/// </summary>
/// <remarks>
/// Registered automatically by <c>AddPostgresPersistence</c>. Resolves
/// <see cref="Acontplus.Core.Abstractions.Context.IAuditContext"/> from a fresh DI scope on each save, so it is
/// safe to use with <c>AddDbContextPool</c> (singleton-compatible).
/// </remarks>
public sealed class AuditSaveChangesInterceptor(IServiceScopeFactory scopeFactory)
    : Common.Interceptors.AuditSaveChangesInterceptor(scopeFactory);

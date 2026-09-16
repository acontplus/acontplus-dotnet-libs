using Acontplus.Core.Domain.Common.Events;

namespace Acontplus.Persistence.PostgreSQL.Context;

/// <summary>
/// Base EF Core database context for PostgreSQL with support for domain events, timestamp auditing, and soft deletes.
/// </summary>
/// <remarks>
/// Audit identity fields are populated automatically by <see cref="Acontplus.Persistence.PostgreSQL.Interceptors.AuditSaveChangesInterceptor"/>.
/// </remarks>
public abstract class BaseContext : Common.Context.BaseContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    protected BaseContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class with a domain event dispatcher.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="eventDispatcher">The domain event dispatcher.</param>
    protected BaseContext(DbContextOptions options, IDomainEventDispatcher eventDispatcher)
        : base(options, eventDispatcher)
    {
    }
}

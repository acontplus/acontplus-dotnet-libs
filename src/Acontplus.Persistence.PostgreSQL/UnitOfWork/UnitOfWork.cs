namespace Acontplus.Persistence.PostgreSQL.UnitOfWork;

/// <summary>
/// Coordinates EF Core and ADO.NET operations within a single transactional unit of work for PostgreSQL.
/// </summary>
/// <typeparam name="TContext">The EF Core database context type.</typeparam>
public sealed class UnitOfWork<TContext>(
    TContext context,
    IAdoRepository adoRepository,
    ILogger<UnitOfWork<TContext>>? logger = null)
    : Common.UnitOfWork.UnitOfWork<TContext>(context, adoRepository, logger)
    where TContext : DbContext;

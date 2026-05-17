using System.Data.Common;

namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Defines a unit of work pattern for managing database operations and transactions.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a repository instance for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity for the repository.</typeparam>
    /// <returns>A repository instance for the specified entity type.</returns>
    IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class;

    /// <summary>
    /// Gets the ADO repository for executing raw SQL queries and commands.
    /// </summary>
    IAdoRepository AdoRepository { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction with the specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the transaction instance.</returns>
    Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active database transaction, if any.
    /// </summary>
    DbTransaction CurrentDbTransaction { get; }

    /// <summary>
    /// Gets the current database connection.
    /// </summary>
    DbConnection CurrentDbConnection { get; }

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    bool HasActiveTransaction { get; }
}

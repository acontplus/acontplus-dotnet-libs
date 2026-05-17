namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Represents a unit of work that supports distributed transactions across multiple databases or services.
/// </summary>
public interface IDistributedUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Begins a new distributed transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the distributed transaction.</returns>
    Task<ITransaction> BeginDistributedTransactionAsync(CancellationToken cancellationToken = default);
}
